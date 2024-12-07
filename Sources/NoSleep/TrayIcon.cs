using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NoSleep.Properties;

namespace NoSleep
{
    public class TrayIcon : ApplicationContext
    {
        internal const string AppName = "NoSleep";
        internal const string AppGuid = "8b2caf22-dc35-4e70-88df-35933ab63f69";
        const int RefreshInterval = 10000;
        private EXECUTION_STATE ExecutionMode = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED;

        private NotifyIcon _TrayIcon;
        private ToolStripMenuItem _EnabledItem;
        private ToolStripMenuItem _DisplayRequired;
        private ToolStripMenuItem _AutoStartItem;
        private readonly Timer _RefreshTimer;
        private Timer _screenSaverTimer;
        private ToolStripMenuItem _sleepTimeItem;
        private ToolStripMenuItem _screenSaverItem;
        private ScreenSaverForm _screenSaverForm;

        public TrayIcon()
        {
            _RefreshTimer = new Timer() { Interval = RefreshInterval };
            _RefreshTimer.Tick += RefreshTimer_Tick;
            ArmExecutionState();

            Application.ApplicationExit += this.OnApplicationExit;
            InitializeComponent();
            _TrayIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            _TrayIcon = new NotifyIcon
            {
                Text = AppName,
                Icon = Properties.Resources.TrayIcon
            };
            _TrayIcon.Click += TrayIcon_Click;

            var _CloseMenuItem = new ToolStripMenuItem("Close");
            _CloseMenuItem.Click += CloseMenuItem_Click;
            _AutoStartItem = new ToolStripMenuItem("Autostart at login") { Checked = LoadAutoStartPreference() };
            _AutoStartItem.Click += AutoStartItem_Click;
            _EnabledItem = new ToolStripMenuItem("Enabled") { Checked = true };
            _EnabledItem.Click += EnabledItem_Click;
            _DisplayRequired = new ToolStripMenuItem("Keep screen on") { Checked = !Settings.Default.DisplayRequired, ToolTipText="If display should be kept always on in addition to keeping the system on." };
            _DisplayRequired.Click += MonitorRequired_Click;
            MonitorRequired_Click(null, null);

            _sleepTimeItem = new ToolStripMenuItem("Sleep Time Settings");
            _sleepTimeItem.Click += SleepTimeItem_Click;

            _screenSaverItem = new ToolStripMenuItem("Screensaver Settings");
            _screenSaverItem.Click += ScreenSaverItem_Click;

            _TrayIcon.ContextMenuStrip = new ContextMenuStrip();
            _TrayIcon.ContextMenuStrip.Items.Add(_AutoStartItem);
            _TrayIcon.ContextMenuStrip.Items.Add(_DisplayRequired);
            _TrayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _TrayIcon.ContextMenuStrip.Items.Add(_EnabledItem);
            _TrayIcon.ContextMenuStrip.Items.Add(_sleepTimeItem);
            _TrayIcon.ContextMenuStrip.Items.Add(_screenSaverItem);
            _TrayIcon.ContextMenuStrip.Items.Add(_CloseMenuItem);

            InitializeScreenSaver();
        }

        private void MonitorRequired_Click(object sender, EventArgs e)
        {
            if (_DisplayRequired.Checked)
            {
                _DisplayRequired.Checked = false;
                ExecutionMode &= ~EXECUTION_STATE.ES_DISPLAY_REQUIRED;
                Settings.Default.DisplayRequired = false;
                Settings.Default.Save();
                ArmExecutionState();
            }
            else
            {
                _DisplayRequired.Checked = true;
                ExecutionMode |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
                Settings.Default.DisplayRequired = true;
                Settings.Default.Save();
                ArmExecutionState();
            }
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            var e2 = e as MouseEventArgs;
            if (e2.Button == MouseButtons.Left)
            {
                EnabledItem_Click(sender, e);
            }
        }

        private void EnabledItem_Click(object sender, EventArgs e)
        {
            var item = _EnabledItem;
            if (item.Checked)
            {
                item.Checked = false;
                _TrayIcon.Icon = Resources.TrayIconInactive;
                DisarmExecutionState();
            }
            else
            {
                item.Checked = true;
                _TrayIcon.Icon = Resources.TrayIcon;
                ArmExecutionState();
            }
        }

        private void AutoStartItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem item))
                return;

            item.Checked = item.Checked ? !RemoveFromStartup() : AddToStartup();
        }

        private void ArmExecutionState()
        {
            _RefreshTimer.Start();
        }

        private void DisarmExecutionState()
        {
            _RefreshTimer.Enabled = false;
            if (ExecutionMode.HasFlag(EXECUTION_STATE.ES_CONTINUOUS)) WinU.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            _TrayIcon.Visible = false;
            DisarmExecutionState();
            _RefreshTimer.Dispose();
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            WinU.SetThreadExecutionState(ExecutionMode);
        }

        private bool AddToStartup()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string appExecutablePath = Application.ExecutablePath;
            string appShortcutPath = Path.Combine(startupFolderPath, $"{AppName}.lnk");
            try { CreateShortcut(appExecutablePath, appShortcutPath); }
            catch (Exception e)
            {
                MessageBox.Show($"Wasn't able to create autostart shortcut at '{appShortcutPath}'. Error: {e.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool RemoveFromStartup()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string appShortcutPath = Path.Combine(startupFolderPath, $"{AppName}.lnk");
            if (File.Exists(appShortcutPath))
            {
                try
                {
                    File.Delete(appShortcutPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Wasn't able to remove autostart shortcut from '{appShortcutPath}'. Error: {e.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }

        private void CreateShortcut(string targetPath, string shortcutPath)
        {
            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.Save();
        }

        private bool LoadAutoStartPreference()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string appShortcutPath = Path.Combine(startupFolderPath, $"{AppName}.lnk");
            return File.Exists(appShortcutPath);
        }

        private void InitializeScreenSaver()
        {
            _screenSaverTimer = new Timer();
            _screenSaverTimer.Interval = 1000;
            _screenSaverTimer.Tick += ScreenSaverTimer_Tick;
            
            if (ConfigManager.GetScreenSaverEnabled())
            {
                _screenSaverTimer.Start();
            }
        }

        private void ScreenSaverTimer_Tick(object sender, EventArgs e)
        {
            if (!ConfigManager.GetScreenSaverEnabled() || 
                (_screenSaverForm != null && !_screenSaverForm.IsDisposed))
                return;

            uint idleTime = IdleTimeDetector.GetIdleTime();
            if (idleTime / 1000.0 > ConfigManager.GetScreenSaverTimeout())
            {
                ShowScreenSaver();
            }
        }

        private void ShowScreenSaver()
        {
            if (_screenSaverForm == null || _screenSaverForm.IsDisposed)
            {
                try
                {
                    System.Threading.Thread thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            _screenSaverForm = new ScreenSaverForm(ConfigManager.GetFirstImagePath());
                            _screenSaverForm.FormClosed += (s, e) => 
                            {
                                _screenSaverForm = null;
                            };

                            Application.Run(_screenSaverForm);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error showing screensaver: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            _screenSaverForm = null;
                        }
                    });

                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error showing screensaver: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _screenSaverForm = null;
                }
            }
        }

        private void SleepTimeItem_Click(object sender, EventArgs e)
        {
            using (var form = new SleepTimeSettingsForm())
            {
                form.ShowDialog();
            }
        }

        private void ScreenSaverItem_Click(object sender, EventArgs e)
        {
            using (var form = new ScreenSaverSettingsForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (ConfigManager.GetScreenSaverEnabled())
                    {
                        _screenSaverTimer.Start();
                    }
                    else
                    {
                        _screenSaverTimer.Stop();
                    }
                }
            }
        }

        public void UpdateLastActivityTime()
        {
            DateTime lastInput = IdleTimeDetector.GetLastInputTime();
            if (_screenSaverForm != null && !_screenSaverForm.IsDisposed)
            {
                _screenSaverForm.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}