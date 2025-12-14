using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.IO.Ports;
using System.Threading;
//using System.Threading.Tasks;

namespace APP_VER1._1
{
    public partial class Form1 : Form
    {
        // Camera variables
        private FilterInfoCollection cameras;
        private VideoCaptureDevice cam;
        //private IntPtr dvpHandle = IntPtr.Zero;
        //private Timer cameraTimer;
        private Bitmap currentFrame = null;

        // Conveyor control variables
        private bool isMotorRunning = false;
        private int conveyorSpeed = 220;

        // Serial ports for Arduino control
        private string selectedARPort = "COM4";
        private string baudRateAR = "9600";

        // Serial port for AI model
        private string selectedAIPort = "COM10";
        private string baudRateAI = "9600";

        private bool isSystemRunning = false;

        string InputData = String.Empty;
        delegate void SetTextCallBack(string text);

        private Form2 form2;

        //-------------------------------------//
        //---------Form initialization---------//
        //-------------------------------------//
        public Form1()
        {
            InitializeComponent();

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(ARDataReceive);
            serialPort2.DataReceived += new SerialDataReceivedEventHandler(AIDataReceive);

            cboARBaudRate.Items.AddRange(new string[] { "4800", "9600", "19200", "38400", "57600" });
            cboARBaudRate.Text = baudRateAR;
            cboARCOM.DataSource = SerialPort.GetPortNames();

            cboAIBaudRate.Items.AddRange(new string[] { "4800", "9600", "19200", "38400", "57600" });
            cboAIBaudRate.Text = baudRateAI;
            cboAICOM.DataSource = SerialPort.GetPortNames();

            trackBar1.Minimum = 0;
            trackBar1.Maximum = 255;
            trackBar1.TickFrequency = 5;
            trackBar1.Value = conveyorSpeed;

            form2 = new Form2();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCameraHandle();

            if (serialPort1.IsOpen)
                serialPort1.Close();

            if (serialPort2.IsOpen)
                serialPort2.Close();
        }

        //------------------------------//
        //----------Set up auto---------//
        //------------------------------//
        private bool ValidateSettings()
        {
            // Kiểm tra camera
            if (cboCam.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a camera first!", "Camera Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Kiểm tra COM ports
            if (string.IsNullOrEmpty(cboARCOM.Text) || string.IsNullOrEmpty(cboAICOM.Text))
            {
                MessageBox.Show("Please select COM ports for Arduino and AI!", "COM Ports Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Kiểm tra tốc độ băng tải
            if (trackBar1.Value == 0)
            {
                MessageBox.Show("Please set conveyor speed!", "Speed Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void StartAutomaticMode()
        {
            try
            {
                // 1. Open camera
                StartCamera(cboCam.SelectedIndex);
                Thread.Sleep(500);

                // 2. Open COM Arduino
                OpenArduinoCOM();
                Thread.Sleep(200);

                // 3. Open cổng COM AI
                OpenAICOM();
                Thread.Sleep(200);

                StartConveyorWithSpeed();
                Thread.Sleep(200);

                //}

                isSystemRunning = true;
                textBox1.AppendText("System started in automatic mode!" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting automatic mode: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopAutomaticMode();
            }
        }

        private void StopAutomaticMode()
        {
            try
            {
                // Dừng băng tải
                if (serialPort1.IsOpen)
                {
                    StopConveyor();
                }

                // Stop camera
                CloseCameraHandle();

                // Close COM
                if (serialPort1.IsOpen)
                    serialPort1.Close();

                if (serialPort2.IsOpen)
                    serialPort2.Close();

                if (form2 != null && !form2.IsDisposed && form2.Visible)
                {
                    form2.Hide();
                }

                isSystemRunning = false;
                textBox1.Text = "System stopped!" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping automatic mode: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //---------------------------------//
        //----------Button event-----------//
        //---------------------------------//
        private void btnExecuteModel_Click(object sender, EventArgs e)
        {
            if (!isSystemRunning)
            {
                if (ValidateSettings())
                {
                    StartAutomaticMode();
                }
                else
                {
                    MessageBox.Show("Please complete all settings before starting automatic mode!",
                                    "Settings Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnStopModel_Click(object sender, EventArgs e)
        {
            StopAutomaticMode();
        }

        private void btnRefesh_Click(object sender, EventArgs e)
        {
            cboCam.Items.Clear();
            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (cameras.Count == 0)
            {
                return;
            }

            foreach (FilterInfo info in cameras)
            {
                cboCam.Items.Add(info.Name);
            }
        }

        //private void btnRefesh_Click(object sender, EventArgs e)
        //{
        //    cboCam.Items.Clear();
        //    int count = 0;
        //    dvpRefresh(ref count);

        //    for (int i = 0; i < count; i++)
        //    {
        //        cboCam.Items.Add("Camera " + i);
        //    }

        //    if (count > 0)
        //    {
        //        cboCam.SelectedIndex = 0;
        //    }
        //}

        private void btnOpenCam_Click(object sender, EventArgs e)
        {
            if (cboCam.SelectedIndex != -1)
            {
                if (cam != null && cam.IsRunning)
                {
                    CloseCameraHandle();
                }
                StartCamera(cboCam.SelectedIndex);
            }
        }

        private void btnCloseCam_Click(object sender, EventArgs e)
        {
            if (cam != null && cam.IsRunning)
            {
                CloseCameraHandle();
                textBox2.ResetText();
            }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            HandleCapture();
        }

        private void btnOpenLED_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write("*den=1#" + "\r\n");
                textBox3.Text = "Turn light";
            }
        }

        private void btnCloseLED_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write("*den=0#" + "\r\n");
                textBox3.Text = "Turn off light";
            }
        }

        private void btnRunConveyor_Click(object sender, EventArgs e)
        {
            //StartConveyor();
        }

        private void btnStopConveyor_Click(object sender, EventArgs e)
        {
            StopConveyor();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBoxSpeed.Text = trackBar1.Value.ToString();
            label22.Text = trackBar1.Value.ToString();

            if (isMotorRunning && serialPort1.IsOpen)
            {
                string speed = "*speed=" + trackBar1.Value.ToString() + "#\r\n";
                serialPort1.Write(speed);
            }
        }

        //-----------------------------------//
        //---------Camera Management---------//
        //-----------------------------------//
        private void StartCamera(int camIndex)
        {
            if (cam != null && cam.IsRunning) return;

            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (cameras.Count > camIndex)
            {
                cam = new VideoCaptureDevice(cameras[camIndex].MonikerString);
                cam.NewFrame += cam_NewFrame;
                cam.Start();
                //isCameraRunning = true;
            }
        }


        private void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();

                // Cập nhật currentFrame an toàn
                Bitmap cloneForSave = (Bitmap)frame.Clone();

                lock (this)
                {
                    if (currentFrame != null)
                    {
                        currentFrame.Dispose();
                    }
                    currentFrame = cloneForSave;
                }

                // Cập nhật ảnh hiển thị
                pictureBox1.Invoke((MethodInvoker)(() =>
                {
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose();
                    }
                    pictureBox1.Image = (Bitmap)frame.Clone();
                }));

                frame.Dispose(); // Giải phóng frame gốc sau khi clone xong
            }
            catch (Exception ex)
            {
                // Ghi log nếu có lỗi bất thường
                this.Invoke((MethodInvoker)(() =>
                {
                    textBox2.AppendText("[ERROR cam_NewFrame] " + ex.Message + Environment.NewLine);
                }));
            }
        }

        private void CloseCameraHandle()
        {
            if (cam != null)
            {
                if (cam.IsRunning)
                {
                    cam.SignalToStop();
                    cam.WaitForStop();
                }

                cam = null;
            }

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }

        private String HandleCapture()
        {
            try
            {
                Bitmap imageToSave = null;

                lock (this)
                {
                    if (currentFrame == null)
                    {
                        BeginInvoke(new Action(() => textBox1.Text = "❌ currentFrame null!"));
                        return null;
                    }

                    imageToSave = (Bitmap)currentFrame.Clone();
                }

                //string folderPath = "D:\\AIP491_G4\\capIMG\\";
                string folderPath = "C:\\AIP491_G4\\capIMG\\";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Tạo tên file với timestamp
                string fileName = "PCB_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
                string fullPath = Path.Combine(folderPath, fileName);

                // Lưu ảnh và giải phóng bộ nhớ
                imageToSave.Save(fullPath, ImageFormat.Jpeg);
                imageToSave.Dispose();

                // Cập nhật UI
                BeginInvoke(new Action(() => textBox2.Text = fullPath));

                return fullPath;
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    textBox2.AppendText("[Error HandleCapture] " + ex.Message + Environment.NewLine);
                }));
                return null;
            }
        }

        
        //--------------------------------------//
        //--------Serial Port Management--------//
        //--------------------------------------//
        private void OpenArduinoCOM()
        {
            selectedARPort = cboARCOM.Text;
            baudRateAR = cboARBaudRate.Text;

            if (serialPort1.IsOpen)
                serialPort1.Close();

            try
            {
                serialPort1.PortName = selectedARPort;
                serialPort1.BaudRate = Convert.ToInt32(baudRateAR);
                serialPort1.DataBits = 8;
                serialPort1.Parity = Parity.None;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Handshake = Handshake.None;
                serialPort1.Encoding = Encoding.ASCII;
                serialPort1.Open();

                serialPort1.DiscardInBuffer();
                textBox1.Text = "✅ Connected to Arduino on " + selectedARPort + "\r\n";
            }
            catch (Exception ex)
            {
                textBox1.Text = "❌ Error opening Arduino COM: " + ex.Message + "\r\n";
            }
        }

        private void OpenAICOM()
        {
            selectedAIPort = cboAICOM.Text;
            baudRateAI = cboAIBaudRate.Text;


            if (serialPort2.IsOpen)
                serialPort2.Close();

            try
            {
                serialPort2.PortName = selectedAIPort;
                serialPort2.BaudRate = Convert.ToInt32(baudRateAI);
                serialPort2.DataBits = 8;
                serialPort2.Parity = Parity.None;
                serialPort2.StopBits = StopBits.One;
                serialPort2.Handshake = Handshake.None;
                serialPort2.Encoding = Encoding.ASCII;
                serialPort2.Open();

                serialPort2.DiscardInBuffer();
                textBox1.AppendText("Connected to AI on " + selectedAIPort + Environment.NewLine);
            }
            catch (Exception ex)
            {
                textBox1.AppendText("AI COM Error: " + ex.Message + Environment.NewLine);
            }
        }

        private void ARDataReceive(object obj, SerialDataReceivedEventArgs e)
        {
            // Đọc dữ liệu từ cổng Serial
            string inputData = serialPort1.ReadLine();
            if (string.IsNullOrEmpty(inputData))
                return;

            // Ghi log dữ liệu nhận được
            SetText(inputData);

            // Kiểm tra lệnh chụp
            if (inputData.Contains("*capture#"))
            {
                // Tạo một thread riêng để xử lý việc chụp ảnh
                Thread captureThread = new Thread(() =>
                {
                    // Đợi hệ thống ổn định (có thể điều chỉnh thời gian)
                    Thread.Sleep(500);

                    this.BeginInvoke(new Action(() =>
                    {
                        // Kiểm tra camera
                        if (cam == null || !cam.IsRunning )
                        {
                            textBox2.Text = "❌ Camera chưa được mở!";
                            return;
                        }

                        // Xử lý chụp ảnh
                        string imagepath = HandleCapture();
                        if (imagepath != null)
                        {
                            textBox2.Text = "✅ Saved image: " + imagepath;

                            if (serialPort2.IsOpen)
                            {
                                // Gửi đường dẫn đầy đủ tới Python
                                serialPort2.WriteLine(imagepath);
                            }
                            else
                            {
                                textBox1.BeginInvoke(new Action(() =>
                                {
                                    textBox1.Text = "❌ AI COM port is not open, cannot send imagepath!";
                                }));
                            }
                        }
                        else
                        {
                            textBox2.Text = "❌ No image to save!";
                        }

                    }));
                });

                // Đặt thread là background để không chặn ứng dụng khi đóng
                captureThread.IsBackground = true;
                captureThread.Start();

                // Xóa buffer để tránh xử lý lệnh trùng lặp
                serialPort1.DiscardInBuffer();
            }
        }

        private void AIDataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Đọc phản hồi từ Python qua cổng COM AI
                string response = serialPort2.ReadLine();

                if (!string.IsNullOrEmpty(response))
                {
                    // Hiển thị kết quả từ AI
                    this.BeginInvoke(new Action(() =>
                    {
                        textBox1.Text = "AI Response: " + response;

                        string imagePath = "";
                        string pcbStatus = "UNKNOWN";
                        string processTime = "0.00";

                        // Khởi tạo giá trị cho tất cả các thành phần
                        Dictionary<string, int> components = new Dictionary<string, int>()
                        {
                            {"QT", 0}, {"QM", 0}, {"QB", 0}, {"QG", 0},
                            {"UT", 0}, {"UM", 0}, {"UB", 0}, {"UG", 0},
                            {"RT", 0}, {"RM", 0}, {"RB", 0}, {"RG", 0},
                            {"CT", 0}, {"CM", 0}, {"CB", 0}, {"CG", 0}
                        };

                        string[] parts = response.Split(new string[] { " | " }, StringSplitOptions.None);

                        if (parts.Length >= 3)
                        {
                            imagePath = parts[0].Replace("Detected image path", "").Trim();

                            string statusComp = parts[1].Trim();
                            // Xử lý phần chi tiết thành phần
                            if (parts.Length > 1)
                            {
                                string[] componentGroups = parts[1].Split(';');
                                foreach (string group in componentGroups)
                                {
                                    string[] componentDetails = group.Trim().Split(',');
                                    foreach (string detail in componentDetails)
                                    {
                                        // Tìm kiểu dạng như "QT2" trong chuỗi
                                        if (detail.Length >= 3)
                                        {
                                            string compType = detail.Substring(0, 2); // Lấy 2 ký tự đầu (VD: QT)
                                            int value;
                                            if (int.TryParse(detail.Substring(2), out value) && components.ContainsKey(compType))
                                            {
                                                components[compType] = value;
                                            }
                                        }
                                    }
                                }
                            }

                            // Lấy trạng thái PCB
                            if (parts.Length > 2)
                            {
                                string statusPart = parts[2].Trim();
                                if (statusPart.StartsWith("Status"))
                                {
                                    pcbStatus = statusPart.Substring("Status".Length).Trim();
                                }
                            }

                            switch (pcbStatus.ToUpper())
                            {
                                case "MISS":
                                    serialPort1.Write("*mach=miss#");
                                    break;

                                case "BAD":
                                    serialPort1.Write("*mach=bad#");
                                    break;

                                case "GOOD ENOUGH":
                                    serialPort1.Write("*mach=good#");
                                    break;

                                case "GOOD":
                                    serialPort1.Write("*mach=good#");
                                    break;

                                case "UNKNOWN":
                                    break;
                            }


                            textBox1.AppendText("Status:" + pcbStatus);

                            // Lấy thời gian xử lý
                            if (parts.Length > 3)
                            {
                                string timePart = parts[3].Trim();
                                if (timePart.StartsWith("Time"))
                                {
                                    string timeText = timePart.Substring("Time".Length).Trim();
                                    timeText = timeText.Replace("s", "").Trim(); // Loại bỏ 's' ở cuối
                                    double tempTime;
                                    if (double.TryParse(timeText, out tempTime))
                                    {
                                        processTime = tempTime.ToString("0.00");

                                    }
                                }
                            }
                            textBox1.AppendText("Time:" + processTime);
                        }



                        if (form2 != null && !form2.IsDisposed)
                        {
                            form2.UpdateResults(
                                imagePath,
                                components["QT"].ToString(), components["QM"].ToString(),
                                components["QB"].ToString(), components["QG"].ToString(),
                                components["UT"].ToString(), components["UM"].ToString(),
                                components["UB"].ToString(), components["UG"].ToString(),
                                components["RT"].ToString(), components["RM"].ToString(),
                                components["RB"].ToString(), components["RG"].ToString(),
                                components["CT"].ToString(), components["CM"].ToString(),
                                components["CB"].ToString(), components["CG"].ToString(),
                                pcbStatus,
                                processTime
                            );

                            // Hiển thị Form2 nếu chưa hiển thị
                            if (!form2.Visible)
                            {
                                form2.Show();
                            }

                            // Đưa Form2 lên trước nếu nó đã bị che
                            form2.BringToFront();
                            form2.Refresh();
                        }
                        else
                        {
                            form2 = new Form2();
                            form2.UpdateResults(
                                imagePath,
                                components["QT"].ToString(), components["QM"].ToString(),
                                components["QB"].ToString(), components["QG"].ToString(),
                                components["UT"].ToString(), components["UM"].ToString(),
                                components["UB"].ToString(), components["UG"].ToString(),
                                components["RT"].ToString(), components["RM"].ToString(),
                                components["RB"].ToString(), components["RG"].ToString(),
                                components["CT"].ToString(), components["CM"].ToString(),
                                components["CB"].ToString(), components["CG"].ToString(),
                                pcbStatus,
                                processTime
                            );
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    textBox1.AppendText("❌ Error reading AI response: " + ex.Message + Environment.NewLine);
                }));
            }
        }

        //-----------------------------------//
        //----------Conveyor Control---------//
        //-----------------------------------//
        private void StartConveyorWithSpeed()
        {
            if (serialPort1.IsOpen)
            {
                int speed = trackBar1.Value;

                if (speed < 0 || speed > 255)
                {
                    textBox4.Text = "❌ Speed must be between 0 and 255";
                    return;
                }

                string command1 = "*bangtai=1#";
                string command2 = "*speed=" + speed.ToString() + "#";
                serialPort1.Write(command1);
                serialPort1.Write(command2);
                textBox4.Text = "✅ Conveyor started at speed " + speed;
                motor.BackColor = Color.Green;
                isMotorRunning = true;
            }
            else
            {
                textBox4.Text = "❌ COM port is not open!";
            }
        }


        private void StopConveyor()
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write("*bangtai=0#" + "\r\n");
                textBox4.Text = "Stop conveyor";
                isMotorRunning = false;
                motor.BackColor = Color.White;
            }
        }

        //------------------------------------//
        //------------------------------------//
        private void SetText(string text)
        {
            if (textBox1.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(SetText), text);
            }
            else
            {
                textBox1.AppendText(text + Environment.NewLine);

                // Tự động cuộn xuống để hiển thị thông báo mới nhất
                textBox1.SelectionStart = textBox4.Text.Length;
                textBox1.ScrollToCaret();
            }
        }
    }
}


