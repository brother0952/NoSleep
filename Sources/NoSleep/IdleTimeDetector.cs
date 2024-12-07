using System;
using System.Runtime.InteropServices;

namespace NoSleep
{
    /// <summary>
    /// 系统空闲时间检测器，用于获取用户最后输入时间
    /// </summary>
    public static class IdleTimeDetector
    {
        // Windows API 函数声明
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Windows API 结构体，用于获取最后输入信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;      // 结构体大小
            public uint dwTime;      // 最后输入的时间戳
        }

        /// <summary>
        /// 获取系统空闲时间（自上次用户输入后经过的时间）
        /// </summary>
        /// <returns>空闲时间（毫秒）</returns>
        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            return ((uint)Environment.TickCount - lastInputInfo.dwTime);
        }

        /// <summary>
        /// 获取最后一次用户输入的时间
        /// </summary>
        /// <returns>最后输入时间的DateTime对象</returns>
        public static DateTime GetLastInputTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            return DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastInputInfo.dwTime));
        }
    }
} 