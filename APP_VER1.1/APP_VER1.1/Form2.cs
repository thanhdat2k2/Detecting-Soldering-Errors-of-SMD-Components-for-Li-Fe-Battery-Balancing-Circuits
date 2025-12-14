using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace APP_VER1._1
{
    public partial class Form2 : Form
    {
        //private Form1 form1;

        public Form2()
        {
            InitializeComponent();
            //form1 = new Form1();
        }

        public void UpdateResults(
            string imagePath,
            string qTotal, string qMiss, string qBad, string qGood,
            string uTotal, string uMiss, string uBad, string uGood,
            string rTotal, string rMiss, string rBad, string rGood,
            string cTotal, string cMiss, string cBad, string cGood,
            string pcbStatus,
            string processTime)
        {
            try
            {
                //------Update result image------//
                // Log thông tin để debug
                Console.WriteLine("Đường dẫn ảnh: " + imagePath);
                Console.WriteLine("File tồn tại: " + File.Exists(imagePath));

                //------Update result image------//
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    try
                    {
                        // Giải phóng hình ảnh cũ
                        if (pictureBox1.Image != null)
                        {
                            Image oldImage = pictureBox1.Image;
                            pictureBox1.Image = null;
                            oldImage.Dispose();
                        }

                        // Load ảnh với phương pháp an toàn hơn
                        using (var bmpTemp = new Bitmap(imagePath))
                        {
                            // Tạo bản sao của ảnh để tránh lock file
                            pictureBox1.Image = new Bitmap(bmpTemp);
                        }

                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        Console.WriteLine("Đã load ảnh thành công");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi load ảnh: " + ex.Message);
                        Console.WriteLine("Lỗi khi load ảnh: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Đường dẫn ảnh không hợp lệ hoặc file không tồn tại");
                }

                //-----Update conponents information----//
                // Cập nhật thông tin linh kiện Q
                label10.Text = qTotal;
                label14.Text = qMiss;
                label18.Text = qBad;
                label22.Text = qGood;

                // Cập nhật thông tin linh kiện U
                label11.Text = uTotal;
                label15.Text = uMiss;
                label19.Text = uBad;
                label23.Text = uGood;

                // Cập nhật thông tin linh kiện R
                label12.Text = rTotal;
                label16.Text = rMiss;
                label20.Text = rBad;
                label24.Text = rGood;

                // Cập nhật thông tin linh kiện C
                label13.Text = cTotal;
                label17.Text = cMiss;
                label21.Text = cBad;
                label25.Text = cGood;

                //-----Update circuit status-----//
                switch (pcbStatus.ToUpper())
                {
                    case "GOOD":
                        panel1.BackColor = Color.Green;
                        label31.ResetText();
                        break;

                    case "GOOD ENOUGH":
                        panel1.BackColor = Color.Green;
                        label31.ResetText();
                        break;

                    case "BAD":
                        panel1.BackColor = Color.Yellow;
                        label31.ResetText();
                        break;

                    case "MISS":
                        panel1.BackColor = Color.Red;
                        label31.ResetText();
                        break;

                    case "UNKNOWN":
                        //panel1.BackColor = Color.Gray;
                        label31.Text = "X";
                        break;
                }

                //----Update process time----//
                label30.Text = processTime + "s";

                this.BringToFront();
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error update results: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
