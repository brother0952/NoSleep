using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using NoSleep.Properties;

namespace NoSleep
{
    public class ScreenSaverForm : Form
    {
        private PictureBox pictureBox;
        private List<Image> images = new List<Image>();
        private int currentImageIndex = 0;
        private Timer slideShowTimer;
        private Point lastMousePosition;
        private const int MOUSE_SENSITIVITY = 10;
        private bool isFirstMove = true;
        private DateTime formOpenTime;
        private const int ACTIVATION_DELAY = 1000;
        private bool isClosing = false;

        public ScreenSaverForm(string[] imagePaths)
        {
            try
            {
                // 加载所有图片
                foreach (string path in imagePaths)
                {
                    if (File.Exists(path))
                    {
                        images.Add(Image.FromFile(path));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.formOpenTime = DateTime.Now;
            InitializeComponents();
            InitializeSlideShow();
        }

        private void InitializeComponents()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            if (images.Count > 0)
            {
                pictureBox.Image = images[0];
            }

            this.Controls.Add(pictureBox);

            this.MouseMove += ScreenSaverForm_MouseMove;
            this.KeyPress += ScreenSaverForm_KeyPress;
            this.KeyDown += ScreenSaverForm_KeyDown;
            this.MouseDown += (s, e) => CloseScreenSaver();

            pictureBox.MouseMove += ScreenSaverForm_MouseMove;
            pictureBox.KeyPress += ScreenSaverForm_KeyPress;
            pictureBox.KeyDown += ScreenSaverForm_KeyDown;
            pictureBox.MouseDown += (s, e) => CloseScreenSaver();

            this.Bounds = SystemInformation.VirtualScreen;
            Cursor.Hide();

            this.KeyPreview = true;
        }

        private void InitializeSlideShow()
        {
            if (images.Count > 1)
            {
                slideShowTimer = new Timer();
                slideShowTimer.Interval = ConfigManager.GetSlideShowInterval() * 1000; // 转换为毫秒
                slideShowTimer.Tick += SlideShowTimer_Tick;
                slideShowTimer.Start();
            }
        }

        private void SlideShowTimer_Tick(object sender, EventArgs e)
        {
            if (images.Count > 1)
            {
                currentImageIndex = (currentImageIndex + 1) % images.Count;
                pictureBox.Image = images[currentImageIndex];
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.BringToFront();
            this.Activate();
        }

        private void ScreenSaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isClosing) return;
            
            Point screenPoint;
            if (sender == pictureBox)
            {
                screenPoint = pictureBox.PointToScreen(e.Location);
            }
            else
            {
                screenPoint = e.Location;
            }

            if ((DateTime.Now - formOpenTime).TotalMilliseconds < ACTIVATION_DELAY)
            {
                lastMousePosition = screenPoint;
                return;
            }

            if (isFirstMove)
            {
                lastMousePosition = screenPoint;
                isFirstMove = false;
                return;
            }

            int deltaX = Math.Abs(screenPoint.X - lastMousePosition.X);
            int deltaY = Math.Abs(screenPoint.Y - lastMousePosition.Y);

            if (deltaX > MOUSE_SENSITIVITY || deltaY > MOUSE_SENSITIVITY)
            {
                CloseScreenSaver();
                return;
            }

            lastMousePosition = screenPoint;
        }

        private void ScreenSaverForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (isClosing) return;
            if ((DateTime.Now - formOpenTime).TotalMilliseconds < ACTIVATION_DELAY)
                return;

            CloseScreenSaver();
        }

        private void ScreenSaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (isClosing) return;
            if ((DateTime.Now - formOpenTime).TotalMilliseconds < ACTIVATION_DELAY)
                return;

            CloseScreenSaver();
            e.Handled = true;
        }

        private void CloseScreenSaver()
        {
            if (!isClosing)
            {
                isClosing = true;
                BeginInvoke(new Action(() => 
                {
                    this.Close();
                }));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Cursor.Show();
            slideShowTimer?.Dispose();
            foreach (var image in images)
            {
                image?.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        image?.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
} 