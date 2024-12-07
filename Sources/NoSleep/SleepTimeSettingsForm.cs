using System;
using System.Windows.Forms;
using NoSleep.Properties;

namespace NoSleep
{
    public class SleepTimeSettingsForm : Form
    {
        private NumericUpDown numericUpDown;
        private Button btnOK;
        private Button btnCancel;

        public SleepTimeSettingsForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Sleep Time Settings";
            this.Size = new System.Drawing.Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var label = new Label
            {
                Text = "Minutes before sleep (0 = never sleep):",
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(280, 20)
            };

            numericUpDown = new NumericUpDown
            {
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(120, 20),
                Minimum = 0,
                Maximum = 999,
                Value = Settings.Default.SleepTime
            };

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(60, 80)
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(160, 80)
            };

            this.Controls.AddRange(new Control[] { label, numericUpDown, btnOK, btnCancel });
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Settings.Default.SleepTime = (int)numericUpDown.Value;
            Settings.Default.Save();
            this.Close();
        }
    }
} 