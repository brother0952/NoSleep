using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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
        /// <returns>超时时间，默认为300秒（5分钟）</returns>
        public static int GetScreenSaverTimeout()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("ScreenSaver", "Timeout", "300", result, 255, ConfigPath);
            if (int.TryParse(result.ToString(), out int timeout))
            {
                return timeout;
            }
            return 300; // 默认5分钟
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
    }
} 