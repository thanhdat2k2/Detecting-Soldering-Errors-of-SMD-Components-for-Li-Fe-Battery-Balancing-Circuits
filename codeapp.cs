using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.IO.Ports;
using System.Xml;

namespace APP
{
    public partial class Form1 : Form
    {
        // Camera variables
        private FilterInfoCollection cameras;  
        private VideoCaptureDevice cam;
        private Bitmap currentFrame = null;
        private bool isCameraRunning = false;


        // Conveyor control variables
        private bool isMotorRunning = false;
        private bool isServoActivated = false;
        private int conveyorSpeed = 0;  // Biến lưu tốc độ băng tải

        // Serial ports for Arduino control
        private string selectedPort = "";
        private string baudRate = "9600";
        private bool isOpenCOM = false;
        private bool isClosedCOM = false;

        // Serial port for AI model
        private string selectedAIPort = "";
        private string baudRateAI = "9600";
        private bool isOpenAICOM = false;
        private bool isClosedAICOM = false;

        private bool isSystemRunning = false;

        string InputData = String.Empty;
        delegate void SetTextCallBack(string text);

        public Form1()
        {
            InitializeComponent();
            InitializeSettings();

            serialPortProAPP.DataReceived += new SerialDataReceivedEventHandler(DataReceive);
            serialPortProAI.DataReceived += new SerialDataReceivedEventHandler(AIDataReceive);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeSettings();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCameraHandle();

            if (serialPortProAPP.IsOpen)
                serialPortProAPP.Close();

            if (serialPortProAI.IsOpen)
                serialPortProAI.Close();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Tính “scaleFactor” dựa trên chiều rộng hoặc chiều cao Form
            float scaleFactor = (float)this.ClientSize.Width / 768;

            // Tránh việc scale quá nhỏ hoặc quá lớn nếu muốn
            if (scaleFactor < 0.5f) scaleFactor = 0.5f;
            if (scaleFactor > 2.0f) scaleFactor = 2.0f;

            label1.Font = new Font(label1.Font.FontFamily, 9 * scaleFactor, label1.Font.Style);
            label2.Font = new Font(label2.Font.FontFamily, 9 * scaleFactor, label2.Font.Style);
            label3.Font = new Font(label3.Font.FontFamily, 9 * scaleFactor, label3.Font.Style);
            label4.Font = new Font(label4.Font.FontFamily, 9 * scaleFactor, label4.Font.Style);
            label5.Font = new Font(label5.Font.FontFamily, 7 * scaleFactor, label5.Font.Style);
            label6.Font = new Font(label6.Font.FontFamily, 7.5f * scaleFactor, label6.Font.Style);
            label7.Font = new Font(label7.Font.FontFamily, 7 * scaleFactor, label7.Font.Style);
            label8.Font = new Font(label8.Font.FontFamily, 7.5f * scaleFactor, label8.Font.Style);
            label9.Font = new Font(label9.Font.FontFamily, 7 * scaleFactor, label9.Font.Style);
            label10.Font = new Font(label10.Font.FontFamily, 7.5f * scaleFactor, label10.Font.Style);
            label11.Font = new Font(label11.Font.FontFamily, 7.5f * scaleFactor, label11.Font.Style);
            label12.Font = new Font(label12.Font.FontFamily, 7.5f * scaleFactor, label12.Font.Style);
            button7.Font = new Font(button7.Font.FontFamily, 10 * scaleFactor, button7.Font.Style);
            button8.Font = new Font(button8.Font.FontFamily, 10 * scaleFactor, button8.Font.Style);
            button9.Font = new Font(button9.Font.FontFamily, 10 * scaleFactor, button9.Font.Style);
            button10.Font = new Font(button10.Font.FontFamily, 6 * scaleFactor, button10.Font.Style);
            button11.Font = new Font(button11.Font.FontFamily, 6 * scaleFactor, button11.Font.Style);
            btnExecuteModel.Font = new Font(btnExecuteModel.Font.FontFamily, 7.8f * scaleFactor, btnExecuteModel.Font.Style);
            settingBox.Font = new Font(settingBox.Font.FontFamily, 7.8f * scaleFactor, settingBox.Font.Style);
            camListBox.Font = new Font(camListBox.Font.FontFamily, 8 * scaleFactor, camListBox.Font.Style);
            camControlBox.Font = new Font(camControlBox.Font.FontFamily, 8 * scaleFactor, camControlBox.Font.Style);
            lampBox.Font = new Font(lampBox.Font.FontFamily, 8 * scaleFactor, lampBox.Font.Style);
            button4.Font = new Font(button4.Font.FontFamily, 8 * scaleFactor, button4.Font.Style);
            button5.Font = new Font(button5.Font.FontFamily, 8 * scaleFactor, button5.Font.Style);
            button6.Font = new Font(button6.Font.FontFamily, 8 * scaleFactor, button6.Font.Style);
            tabControl1.Font = new Font(tabControl1.Font.FontFamily, 8 * scaleFactor, tabControl1.Font.Style);
            label18.Font = new Font(label18.Font.FontFamily, 6.5f * scaleFactor, label18.Font.Style);
            label22.Font = new Font(label22.Font.FontFamily, 7 * scaleFactor, label22.Font.Style);
            label21.Font = new Font(label21.Font.FontFamily, 7 * scaleFactor, label21.Font.Style);
        }

     
        // ---------SETTINGS---------//
        private void InitializeSettings()
        {
            // COM port for Arduino settings
            cboBaudrate.Items.AddRange(new string[] { "4800", "9600", "19200", "38400", "57600" });
            cboBaudrate.Text = baudRate;
            cboCOM.DataSource = SerialPort.GetPortNames();

            // COM port for AI model settings
            cboAIBaudrate.Items.AddRange(new string[] { "4800", "9600", "19200", "38400", "57600" });
            cboAIBaudrate.Text = baudRate;
            cboAICOM.DataSource = SerialPort.GetPortNames();

            // Conveyor settings
            trackBar1.Minimum = 0;
            trackBar1.Maximum = 255;
            trackBar1.TickFrequency = 5;  // Tốc độ thanh trượt tăng lên mỗi 5 đơn vị
            trackBar1.Value = conveyorSpeed; // Mặc định tốc độ là 0
        }

        private void btnApplySettings_Click(object sender, EventArgs e)
        {
            ApplyArduinoCOMSettings();
            //ApplyAIComSettings();
        }

        private void ApplyArduinoCOMSettings()
        {
            selectedPort = cboCOM.Text;
            baudRate = cboBaudrate.Text;

            if (isOpenCOM)
            {
                if (serialPortProAPP.IsOpen)
                    serialPortProAPP.Close();

                try
                {
                    serialPortProAPP.PortName = selectedPort;
                    serialPortProAPP.BaudRate = Convert.ToInt32(baudRate);
                    serialPortProAPP.DataBits = 8;
                    serialPortProAPP.Parity = Parity.None;
                    serialPortProAPP.StopBits = StopBits.One;
                    serialPortProAPP.Handshake = Handshake.None;
                    serialPortProAPP.Encoding = Encoding.ASCII;
                    serialPortProAPP.Open();

                    serialPortProAPP.DiscardInBuffer();
                    textBox4.Text = "Connected to Arduino on " + selectedPort + "\r\n";
                }
                catch (Exception ex)
                {
                    textBox4.Text = "Error: " + ex.Message + "\r\n";
                }
            }
            else if (isClosedCOM && serialPortProAPP.IsOpen)
            {
                serialPortProAPP.Close();
                textBox4.Text = "Disconnected Arduino COM.\r\n";
            }
        }

        private void ApplyAIComSettings()
        {
            selectedAIPort = cboAICOM.Text;
            baudRateAI = cboAIBaudrate.Text;

            if (isOpenAICOM)
            {
                if (serialPortProAI.IsOpen)
                    serialPortProAI.Close();

                try
                {
                    serialPortProAI.PortName = selectedAIPort;
                    serialPortProAI.BaudRate = Convert.ToInt32(baudRateAI);
                    serialPortProAI.DataBits = 8;
                    serialPortProAI.Parity = Parity.None;
                    serialPortProAI.StopBits = StopBits.One;
                    serialPortProAI.Handshake = Handshake.None;
                    serialPortProAI.Encoding = Encoding.ASCII;
                    serialPortProAI.Open();

                    serialPortProAI.DiscardInBuffer();
                    textBox4.AppendText("Connected to AI on " + selectedAIPort + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    textBox4.AppendText("AI COM Error: " + ex.Message + Environment.NewLine);
                }
            }
            else if (isClosedAICOM && serialPortProAI.IsOpen)
            {
                serialPortProAI.Close();
                textBox4.AppendText("Disconnected AI COM." + Environment.NewLine);
            }
        }

        // Mở, đóng COM
        private void button10_Click(object sender, EventArgs e)
        {
            isOpenCOM = true;
            isClosedCOM = false;
            isOpenAICOM = true;
            isClosedAICOM = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            isOpenCOM = false;
            isClosedCOM = true;
            isOpenAICOM = false;
            isClosedAICOM = true;
        }


        //-----------Camera Control-----------//
        private void StartCamera(int camIndex)
        {
            if (cam != null && cam.IsRunning) return;

            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (cameras.Count > camIndex)
            {
                cam = new VideoCaptureDevice(cameras[camIndex].MonikerString);
                cam.NewFrame += cam_NewFrame;
                cam.Start();
                isCameraRunning = true;
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
                    textBox4.AppendText("[ERROR cam_NewFrame] " + ex.Message + Environment.NewLine);
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

        //private String HandleCapture()
        //{
        //    if (currentFrame == null)
        //    {
        //        textBox1.Text = "❌ currentFrame bị null!";
        //        return null;
        //    }

        //    string folderPath = "D:\\AIP491_G4\\capIMG\\";
        //    string fileName = "PCB_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
        //    string fullPath = Path.Combine(folderPath, fileName);
        //    textBox1.Text = fullPath;

        //    Bitmap imageToSave = (Bitmap)currentFrame.Clone();
        //    imageToSave.Save(fullPath, ImageFormat.Jpeg);
        //    imageToSave.Dispose();

        //    return fullPath;
        //}

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

                string folderPath = "D:\\AIP491_G4\\capIMG\\";
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
                BeginInvoke(new Action(() => textBox1.Text = fullPath));

                return fullPath;
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    textBox4.AppendText("[Error HandleCapture] " + ex.Message + Environment.NewLine);
                }));
                return null;
            }
        }

        // Button to refresh camera list
        private void button1_Click(object sender, EventArgs e)
        {
            listCam.Items.Clear();
            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (cameras.Count == 0)
            {
                return;
            }

            foreach (FilterInfo info in cameras)
            {
                listCam.Items.Add(info.Name);
            }
        }

        //Button to start camera
        private void button2_Click(object sender, EventArgs e)
        {
            if (listCam.SelectedIndex != -1)
            {
                if (cam != null && cam.IsRunning)
                {
                    CloseCameraHandle();
                }
                StartCamera(listCam.SelectedIndex);
            }
        }

        // Button to stop camera
        private void button3_Click(object sender, EventArgs e)
        {
            if (cam != null && cam.IsRunning)
            {
                CloseCameraHandle();
                textBox1.ResetText();
            }
        }

        private void button4_click(object sender, EventArgs e)
        {
            //handleautocapture();
        }


        //--------LampControl--------//
        // Bật đèn
        private void button5_Click(object sender, EventArgs e)
        {
            if (serialPortProAPP.IsOpen)
            {
                serialPortProAPP.Write("*den=1#" + "\r\n");
                textBox2.Text = "Turn light";
            }
        }

        // Tắt đèn
        private void button6_Click(object sender, EventArgs e)
        {
            if (serialPortProAPP.IsOpen)
            {
                serialPortProAPP.Write("*den=0#" + "\r\n");
                textBox2.Text = "Turn off light";
            }
        }


        //--------BangTaiControl--------//
        // Chạy băng tải
        private void button7_Click(object sender, EventArgs e)
        {
            if (serialPortProAPP.IsOpen && !isMotorRunning)
            {
                int conveyorSpeed = int.Parse(textBoxSpeed.Text);
                if (conveyorSpeed >= 0 && conveyorSpeed <= 255)
                {
                    //serialPortProAPP.WriteLine(conveyorSpeed.ToString() + "\r\n");
                    serialPortProAPP.Write("*bangtai=1#" + conveyorSpeed.ToString() + "\r\n");
                    textBox3.Text = "Run conveyor";
                    isMotorRunning = true;
                    motor.BackColor = Color.Green;
                }
                else
                {
                    textBox3.Text = "Invalid speed value!";
                }
            }
        }

        // Dừng băng tải
        private void button8_Click(object sender, EventArgs e)
        {
            if (serialPortProAPP.IsOpen)
            {
                serialPortProAPP.Write("*bangtai=0#" + "\r\n");
                textBox3.Text = "Stop conveyor";
                isMotorRunning = false;
                motor.BackColor = Color.White;
            }
        }


        // Điều chỉnh tốc độ băng tải
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBoxSpeed.Text = trackBar1.Value.ToString();
            label18.Text = trackBar1.Value.ToString();
            string speed = "*speed=" + trackBar1.Value.ToString() + '#';
            serialPortProAPP.Write(speed);
        }

        // Gạt mạch xấu
        private void button9_Click(object sender, EventArgs e)
        {
            if (serialPortProAPP.IsOpen)
            {
                serialPortProAPP.Write("*mach=bad#" + "\r\n");
                isServoActivated = !isServoActivated;

                servo.BackColor = isServoActivated ? Color.Blue : Color.White;
                textBox3.Text = isServoActivated ? "Remove bad circuit" : "";
            }
        }

        private void DataReceive(object obj, SerialDataReceivedEventArgs e)
        {
            // Đọc dữ liệu từ cổng Serial
            string inputData = serialPortProAPP.ReadExisting();
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
                        if (cam == null || !cam.IsRunning)
                        {
                            textBox1.Text = "❌ Camera isn't opened!";
                            return;
                        }

                        // Xử lý chụp ảnh
                        string path = HandleCapture();
                        if (path != null)
                        {
                            textBox1.Text = "✅ Saved image: " + path;
                            // Gửi thông báo về Arduino
                            if (serialPortProAPP.IsOpen)
                            {
                                serialPortProAPP.WriteLine("*image=" + Path.GetFileName(path) + "#");
                            }
                        }
                        else
                        {
                            textBox1.Text = "❌ No image to save!";
                        }

                    }));
                });

                // Đặt thread là background để không chặn ứng dụng khi đóng
                captureThread.IsBackground = true;
                captureThread.Start();

                // Xóa buffer để tránh xử lý lệnh trùng lặp
                serialPortProAPP.DiscardInBuffer();
            }
        }

        private void AIDataReceive(object sender, SerialDataReceivedEventArgs e)
        { 
        }


        // Cải thiện SetText để tránh deadlock
        private void SetText(string text)
        {
            if (textBox4.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(SetText), text);
            }
            else
            {
                textBox4.AppendText(text + Environment.NewLine);

                // Tự động cuộn xuống để hiển thị thông báo mới nhất
                textBox4.SelectionStart = textBox4.Text.Length;
                textBox4.ScrollToCaret();
            }
        }

        private void popupResult_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
        }
    }
}
