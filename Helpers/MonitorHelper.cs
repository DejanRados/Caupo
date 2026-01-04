using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace Caupo.Helpers
{
    public class MonitorInfo
    {
        public int Index { get; set; }
        public string DeviceName { get; set; }
        public Rect Bounds { get; set; }
        public bool IsPrimary { get; set; }
    }

    public static class MonitorHelper
    {
        private delegate bool MonitorEnumDelegate(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

        [DllImport ("user32.dll")]
        private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumDelegate lpfnEnum, nint dwData);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

        [StructLayout (LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        private const uint MONITORINFOF_PRIMARY = 1;

        public static List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo> ();
            int index = 0;

            MonitorEnumDelegate callback = delegate (nint hMonitor, nint hdcMonitor, ref RECT rect, nint data)
            {
                var info = new MONITORINFOEX ();
                info.cbSize = Marshal.SizeOf (info);
                info.szDevice = string.Empty;

                GetMonitorInfo (hMonitor, ref info);

                monitors.Add (new MonitorInfo
                {
                    Index = index++,
                    DeviceName = info.szDevice,
                    Bounds = new Rect (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    IsPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0
                });

                return true;
            };

            EnumDisplayMonitors (nint.Zero, nint.Zero, callback, nint.Zero);

            return monitors;
        }
    }
}