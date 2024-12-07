using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

namespace NoSleep
{
    /// <summary>
    /// 配置管理器，负责处理程序的配置文件读写和屏保图片管理
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// 获取配置文件的完整路径（在程序目录下的 nosleep.ini）
        /// </summary>
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nosleep.ini");

        /// <summary>
        /// 支持的图片文件扩展名列表
        /// </summary>
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

        // Windows API 函数声明
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnedString, int size, string filePath);

        [DllImport("kernel32.dll")]
        private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

        /// <summary>
        /// 初始化配置文件
        /// </summary>
        public static void InitializeConfig()
        {
            bool keepScreenOn = true;  // 默认值
            bool autoStart = true;     // 默认值

            // 如果已有配置文件，先读取现有设置
            if (File.Exists(ConfigPath))
            {
                keepScreenOn = GetKeepScreenOn();
                autoStart = GetAutoStart();
                File.Delete(ConfigPath);  // 删除旧文件以创建新文件
            }

            // 创建或更新配置
            WritePrivateProfileString("ScreenSaver", "Enabled", "true", ConfigPath);
            WritePrivateProfileString("ScreenSaver", "Timeout", "120", ConfigPath);
            WritePrivateProfileString("ScreenSaver", "SlideShowInterval", "10", ConfigPath);
            WritePrivateProfileString("System", "AutoStart", autoStart.ToString().ToLower(), ConfigPath);
            WritePrivateProfileString("System", "KeepScreenOn", keepScreenOn.ToString().ToLower(), ConfigPath);
            
            // 根据保存的设置设置自动启动
            SetAutoStart(autoStart);
        }

        /// <summary>
        /// 更新配置文件结构但保留现有设置
        /// </summary>
        public static void UpdateConfig()
        {
            if (File.Exists(ConfigPath))
            {
                // 读取现有设置
                bool keepScreenOn = GetKeepScreenOn();
                bool autoStart = GetAutoStart();
                bool screenSaverEnabled = GetScreenSaverEnabled();
                int screenSaverTimeout = GetScreenSaverTimeout();
                int slideShowInterval = GetSlideShowInterval();

                // 删除旧文件并创建新文件
                File.Delete(ConfigPath);

                // 写入所有设置
                WritePrivateProfileString("ScreenSaver", "Enabled", screenSaverEnabled.ToString().ToLower(), ConfigPath);
                WritePrivateProfileString("ScreenSaver", "Timeout", screenSaverTimeout.ToString(), ConfigPath);
                WritePrivateProfileString("ScreenSaver", "SlideShowInterval", slideShowInterval.ToString(), ConfigPath);
                WritePrivateProfileString("System", "AutoStart", autoStart.ToString().ToLower(), ConfigPath);
                WritePrivateProfileString("System", "KeepScreenOn", keepScreenOn.ToString().ToLower(), ConfigPath);
            }
            else
            {
                InitializeConfig();
            }
        }

        /// <summary>
        /// 获取程序目录下第一个支持的图片文件路径
        /// </summary>
        /// <returns>找到的图片路径，如果没有找到则返回空字符串</returns>
        public static string GetFirstImagePath()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string extension in SupportedImageExtensions)
            {
                string[] files = Directory.GetFiles(directory, "*" + extension);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取屏保是否启用的设置
        /// </summary>
        /// <returns>true表示启用，false表示禁用</returns>
        public static bool GetScreenSaverEnabled()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("ScreenSaver", "Enabled", "true", result, 255, ConfigPath);
            return result.ToString().ToLower() == "true";
        }

        /// <summary>
        /// 获取屏保超时时间（秒）
        /// </summary>
        /// <returns>超时时间，默认为120秒（2分钟）</returns>
        public static int GetScreenSaverTimeout()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("ScreenSaver", "Timeout", "120", result, 255, ConfigPath);
            if (int.TryParse(result.ToString(), out int timeout))
            {
                return timeout;
            }
            return 120; // 默认2分钟
        }

        /// <summary>
        /// 获取系统是否自动启动的设置
        /// </summary>
        /// <returns>true表示自动启动，false表示不自动启动</returns>
        public static bool GetAutoStart()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("System", "AutoStart", "true", result, 255, ConfigPath);
            return result.ToString().ToLower() == "true";
        }

        /// <summary>
        /// 获取系统是否保持屏幕常亮的设置
        /// </summary>
        /// <returns>true表示启用，false表示禁用</returns>
        public static bool GetKeepScreenOn()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("System", "KeepScreenOn", "true", result, 255, ConfigPath);
            return result.ToString().ToLower() == "true";
        }

        /// <summary>
        /// 保存屏保启用状态
        /// </summary>
        /// <param name="enabled">是否启用屏保</param>
        public static void SaveScreenSaverEnabled(bool enabled)
        {
            WritePrivateProfileString("ScreenSaver", "Enabled", enabled.ToString(), ConfigPath);
        }

        /// <summary>
        /// 保存屏保超时时间
        /// </summary>
        /// <param name="seconds">超时时间（秒）</param>
        public static void SaveScreenSaverTimeout(int seconds)
        {
            WritePrivateProfileString("ScreenSaver", "Timeout", seconds.ToString(), ConfigPath);
        }

        /// <summary>
        /// 获取屏保图片轮播间隔（秒）
        /// </summary>
        public static int GetSlideShowInterval()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("ScreenSaver", "SlideShowInterval", "10", result, 255, ConfigPath);
            if (int.TryParse(result.ToString(), out int interval))
            {
                return interval;
            }
            return 10; // 默认10秒
        }

        private static void SetAutoStart(bool enabled)
        {
            string startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "NoSleep.lnk");

            if (enabled)
            {
                try
                {
                    var shell = new IWshRuntimeLibrary.WshShell();
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(startupPath);
                    shortcut.TargetPath = Application.ExecutablePath;
                    shortcut.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create autostart shortcut: {ex.Message}");
                }
            }
            else if (File.Exists(startupPath))
            {
                try
                {
                    File.Delete(startupPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to remove autostart shortcut: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取所有支持的图片文件路径
        /// </summary>
        public static string[] GetAllImagePaths()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            List<string> allImages = new List<string>();
            
            foreach (string extension in SupportedImageExtensions)
            {
                allImages.AddRange(Directory.GetFiles(directory, "*" + extension));
            }
            
            return allImages.ToArray();
        }
    }
} 