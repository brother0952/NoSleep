using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using NoSleep.Properties;

namespace NoSleep
{
    public class TrayIcon : ApplicationContext
    {
        internal const string AppName = "NoSleep";
        internal const string AppGuid = "8b2caf22-dc35-4e70-88df-35933ab63f69";
        const int RefreshInterval = 10000;
        private EXECUTION_STATE ExecutionMode;

        private NotifyIcon _TrayIcon;
        private readonly Timer _RefreshTimer;
        private Timer _screenSaverTimer;
        private ScreenSaverForm _screenSaverForm;

        public TrayIcon()
        {
            // 根据配置设置执行模式
            ExecutionMode = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED;
            if (ConfigManager.GetKeepScreenOn())
            {
                ExecutionMode |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
            }

            _RefreshTimer = new Timer() { Interval = RefreshInterval };
            _RefreshTimer.Tick += RefreshTimer_Tick;
            
            Application.ApplicationExit += this.OnApplicationExit;
            InitializeComponent();
            
            _TrayIcon.Visible = true;
            ArmExecutionState();
        }

        private void InitializeComponent()
        {
            _TrayIcon = new NotifyIcon
            {
                Text = AppName
            };

            // 尝试加载图标，如果失败则使用默认图标
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "valeo.ico");
            try
            {
                if (File.Exists(iconPath))
                {
                    _TrayIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // 使用应用程序默认图标
                    _TrayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch
            {
                // 如果加载失败，使用应用程序默认图标
                _TrayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }

            var _CloseMenuItem = new ToolStripMenuItem("Close");
            _CloseMenuItem.Click += CloseMenuItem_Click;

            _TrayIcon.ContextMenuStrip = new ContextMenuStrip();
            _TrayIcon.ContextMenuStrip.Items.Add(_CloseMenuItem);

            InitializeScreenSaver();
        }

        private void ArmExecutionState()
        {
            _RefreshTimer.Start();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            _TrayIcon.Visible = false;
            _RefreshTimer.Stop();
            if (ExecutionMode.HasFlag(EXECUTION_STATE.ES_CONTINUOUS))
            {
                WinU.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
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
                            _screenSaverForm = new ScreenSaverForm(ConfigManager.GetAllImagePaths());
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
                    MessageBox.Show($"Error creating screensaver thread: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _screenSaverForm = null;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _RefreshTimer?.Dispose();
                _screenSaverTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}