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
        private ToolStripMenuItem keepScreenOnItem;
        private ToolStripMenuItem showScreenSaverItem;

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

            // 尝试加载图标
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "valeo.ico");
            try
            {
                if (File.Exists(iconPath))
                {
                    _TrayIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    _TrayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch
            {
                _TrayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }

            // 创建菜单项
            var menu = new ContextMenuStrip();

            // 超时时间设置
            var timeoutMenu = new ToolStripMenuItem("超时时间");
            var timeouts = new[] { 10, 180, 600, 1800, 3600, 10800, -1 };
            var timeoutNames = new[] { "10秒", "3分钟", "10分钟", "30分钟", "60分钟", "3小时", "永不" };
            
            // 获取当前超时时间
            int currentTimeout = ConfigManager.GetScreenSaverTimeout();
            
            for (int i = 0; i < timeouts.Length; i++)
            {
                var item = new ToolStripMenuItem(timeoutNames[i]);
                int timeout = timeouts[i];
                // 如果当前值等于配置的值，或者是首次运行且当前项是3分钟（180秒），则勾选
                item.Checked = timeout == currentTimeout || (currentTimeout == 0 && timeout == 180);
                item.Click += (s, e) =>
                {
                    foreach (ToolStripMenuItem mi in timeoutMenu.DropDownItems)
                        mi.Checked = false;
                    ((ToolStripMenuItem)s).Checked = true;
                    ConfigManager.SaveScreenSaverTimeout(timeout);

                    // 如果选择了"永不"，自动切换到"保持屏幕常亮"模式
                    if (timeout == -1)
                    {
                        keepScreenOnItem.Checked = true;
                        showScreenSaverItem.Checked = false;
                        ConfigManager.SaveKeepScreenOn(true);
                        ConfigManager.SaveScreenSaverEnabled(false);
                        _screenSaverTimer?.Stop();
                        UpdateExecutionState();
                    }
                };
                timeoutMenu.DropDownItems.Add(item);
            }

            // 超时行为设置
            var timeoutActionMenu = new ToolStripMenuItem("超时后行为");

            // 初始化菜单项
            keepScreenOnItem = new ToolStripMenuItem("保持屏幕常亮");
            showScreenSaverItem = new ToolStripMenuItem("显示屏保图片");

            // 设置初始状态
            keepScreenOnItem.Checked = ConfigManager.GetKeepScreenOn();
            showScreenSaverItem.Checked = !ConfigManager.GetKeepScreenOn();

            // 设置点击事件
            keepScreenOnItem.Click += KeepScreenOnItem_Click;
            showScreenSaverItem.Click += ShowScreenSaverItem_Click;

            // 添加到菜单
            timeoutActionMenu.DropDownItems.Add(keepScreenOnItem);
            timeoutActionMenu.DropDownItems.Add(showScreenSaverItem);

            // 开机启动设置
            var autoStartItem = new ToolStripMenuItem("开机启动")
            {
                Checked = ConfigManager.GetAutoStart()
            };
            autoStartItem.Click += (s, e) =>
            {
                bool newState = !ConfigManager.GetAutoStart();
                ConfigManager.SaveAutoStart(newState);
                autoStartItem.Checked = newState;
            };

            // 添加所有菜单项
            menu.Items.Add(timeoutMenu);
            menu.Items.Add(timeoutActionMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(autoStartItem);
            menu.Items.Add(new ToolStripSeparator());
            
            var closeItem = new ToolStripMenuItem("退出程序");
            closeItem.Click += CloseMenuItem_Click;
            menu.Items.Add(closeItem);

            _TrayIcon.ContextMenuStrip = menu;
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
            
            // 根据配置决定是否启动计时器
            if (ConfigManager.GetScreenSaverEnabled())
            {
                _screenSaverTimer.Start();
            }
        }

        private void ScreenSaverTimer_Tick(object sender, EventArgs e)
        {
            // 添加日志以便调试
            Debug.WriteLine($"ScreenSaver Timer Tick - Enabled: {ConfigManager.GetScreenSaverEnabled()}, Form: {_screenSaverForm?.IsDisposed}");
            
            if (!ConfigManager.GetScreenSaverEnabled() || 
                (_screenSaverForm != null && !_screenSaverForm.IsDisposed) ||
                ConfigManager.GetScreenSaverTimeout() == -1)
                return;

            uint idleTime = IdleTimeDetector.GetIdleTime();
            double idleSeconds = idleTime / 1000.0;
            Debug.WriteLine($"Idle Time: {idleSeconds}s, Timeout: {ConfigManager.GetScreenSaverTimeout()}s");
            
            if (idleSeconds > ConfigManager.GetScreenSaverTimeout())
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
                    // 确保在主UI线程中创建和显示窗体
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            CreateAndShowScreenSaver();
                        });
                    }
                    else
                    {
                        System.Threading.Thread thread = new System.Threading.Thread(() =>
                        {
                            try
                            {
                                CreateAndShowScreenSaver();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error in screensaver thread: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        });
                        thread.SetApartmentState(System.Threading.ApartmentState.STA);
                        thread.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating screensaver: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _screenSaverForm = null;
                }
            }
        }

        private void CreateAndShowScreenSaver()
        {
            try
            {
                var imagePaths = ConfigManager.GetAllImagePaths();
                // 不管是否有图片，都创建屏保窗体
                _screenSaverForm = new ScreenSaverForm(imagePaths ?? new string[0]);
                _screenSaverForm.FormClosed += (s, e) =>
                {
                    _screenSaverForm?.Dispose();
                    _screenSaverForm = null;
                };

                Application.Run(_screenSaverForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing screensaver: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _screenSaverForm?.Dispose();
                _screenSaverForm = null;
            }
        }

        private void KeepScreenOnItem_Click(object sender, EventArgs e)
        {
            // 如果当前已经是激活状态，直接返回
            if (keepScreenOnItem.Checked)
                return;

            // 设置当前菜单项为选中状态，并取消另一个菜单项的选中状态
            keepScreenOnItem.Checked = true;
            showScreenSaverItem.Checked = false;
            
            // 更新配置
            ConfigManager.SaveKeepScreenOn(true);
            ConfigManager.SaveScreenSaverEnabled(false);

            // 停止屏保计时器
            _screenSaverTimer?.Stop();

            // 更新系统状态
            UpdateExecutionState();
        }

        private void ShowScreenSaverItem_Click(object sender, EventArgs e)
        {
            // 如果当前已经是激活状态，直接返回
            if (showScreenSaverItem.Checked)
                return;

            // 设置当前菜单项为选中状态，并取消另一个菜单项的选中状态
            showScreenSaverItem.Checked = true;
            keepScreenOnItem.Checked = false;

            // 更新配置
            ConfigManager.SaveScreenSaverEnabled(true);
            ConfigManager.SaveKeepScreenOn(false);

            // 启动屏保计时器
            _screenSaverTimer?.Start();

            // 更新系统状态
            UpdateExecutionState();
        }

        private void UpdateExecutionState()
        {
            ExecutionMode = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED;
            if (ConfigManager.GetKeepScreenOn())
            {
                ExecutionMode |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
            }
            WinU.SetThreadExecutionState(ExecutionMode);
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