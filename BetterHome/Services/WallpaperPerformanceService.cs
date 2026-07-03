using System.Runtime.InteropServices;

namespace BetterHome.Services;

public sealed class WallpaperPerformanceService
{
    [StructLayout(LayoutKind.Sequential)] private struct SystemPowerStatus { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, Reserved; public uint BatteryLifeTime, BatteryFullLifeTime; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    public bool IsOnBattery => GetSystemPowerStatus(out var status) && status.ACLineStatus == 0;
    public bool IsFullscreenForeground(IntPtr betterHomeHandle)
    {
        var hwnd = GetForegroundWindow(); if (hwnd == IntPtr.Zero || hwnd == betterHomeHandle || !GetWindowRect(hwnd, out var window)) return false; var monitor = MonitorFromWindow(hwnd, 2); var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() }; if (!GetMonitorInfo(monitor, ref info)) return false; return window.Left <= info.Monitor.Left && window.Top <= info.Monitor.Top && window.Right >= info.Monitor.Right && window.Bottom >= info.Monitor.Bottom;
    }
}
