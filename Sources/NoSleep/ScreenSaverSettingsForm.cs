using System;
using System.Windows.Forms;

namespace NoSleep
{
    public class ScreenSaverSettingsForm : Form
    {
        private CheckBox enabledCheckBox;
        private NumericUpDown timeoutNumeric;
        private Button btnOK;
        private Button btnCancel;

        public ScreenSaverSettingsForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Screensaver Settings";
            this.Size = new System.Drawing.Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            enabledCheckBox = new CheckBox
            {
                Text = "Enable Screensaver",
                Location = new System.Drawing.Point(10, 20),
                Checked = ConfigManager.GetScreenSaverEnabled()
            };

            var timeoutLabel = new Label
            {
                Text = "Show screensaver after idle (seconds):",
                Location = new System.Drawing.Point(10, 50)
            };

            timeoutNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(10, 70),
                Minimum = 10,
                Maximum = 3600,
                Value = ConfigManager.GetScreenSaverTimeout()
            };

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(60, 120)
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(160, 120)
            };

            this.Controls.AddRange(new Control[] { 
                enabledCheckBox, timeoutLabel, timeoutNumeric,
                btnOK, btnCancel 
            });
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ConfigManager.SaveScreenSaverEnabled(enabledCheckBox.Checked);
            ConfigManager.SaveScreenSaverTimeout((int)timeoutNumeric.Value);
        }
    }
} 