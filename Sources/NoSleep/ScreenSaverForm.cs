using System;
using System.Drawing;
using System.Windows.Forms;
using NoSleep.Properties;

namespace NoSleep
{
    public class ScreenSaverForm : Form
    {
        private PictureBox pictureBox;
        private Image screenSaverImage;
        private Point lastMousePosition;
        private const int MOUSE_SENSITIVITY = 10;
        private bool isFirstMove = true;
        private DateTime formOpenTime;
        private const int ACTIVATION_DELAY = 1000;
        private bool isClosing = false;

        public ScreenSaverForm(string imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                {
                    screenSaverImage = Image.FromFile(imagePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.formOpenTime = DateTime.Now;
            InitializeComponents();
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

            if (screenSaverImage != null)
            {
                pictureBox.Image = screenSaverImage;
            }

            this.Controls.Add(pictureBox);

            this.MouseMove += ScreenSaverForm_MouseMove;
            this.KeyDown += ScreenSaverForm_KeyDown;
            this.MouseDown += (s, e) => CloseScreenSaver();

            pictureBox.MouseMove += ScreenSaverForm_MouseMove;
            pictureBox.MouseDown += (s, e) => CloseScreenSaver();

            this.Bounds = SystemInformation.VirtualScreen;
            Cursor.Hide();
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

        private void ScreenSaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (isClosing) return;
            if ((DateTime.Now - formOpenTime).TotalMilliseconds < ACTIVATION_DELAY)
                return;

            CloseScreenSaver();
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
            if (screenSaverImage != null)
            {
                screenSaverImage.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (screenSaverImage != null)
                {
                    screenSaverImage.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
} 