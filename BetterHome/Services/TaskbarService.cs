using System.Runtime.InteropServices;

namespace BetterHome.Services;

public sealed class TaskbarService
{
    [StructLayout(LayoutKind.Sequential)] private struct AppBarData { public uint cbSize; public IntPtr hWnd; public uint uCallbackMessage; public uint uEdge; public Rect rc; public IntPtr lParam; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int left, top, right, bottom; }
    [DllImport("shell32.dll")] private static extern IntPtr SHAppBarMessage(uint message, ref AppBarData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string? windowName);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    private const uint GetState = 4, SetState = 10, AutoHide = 1, AlwaysOnTop = 2;
    private const int Hide = 0, Show = 5;
    private readonly uint _originalState;
    public TaskbarService() { var data = CreateData(); _originalState = (uint)SHAppBarMessage(GetState, ref data).ToInt64(); }
    public bool SetVisible(bool visible)
    {
        var taskbar = FindWindow("Shell_TrayWnd", null); if (taskbar == IntPtr.Zero) return false;
        var data = CreateData(); data.lParam = new IntPtr(visible ? (_originalState == 0 ? AlwaysOnTop : _originalState) : AutoHide | AlwaysOnTop); SHAppBarMessage(SetState, ref data);
        ShowWindow(taskbar, visible ? Show : Hide);
        var secondary = FindWindow("Shell_SecondaryTrayWnd", null); if (secondary != IntPtr.Zero) ShowWindow(secondary, visible ? Show : Hide);
        return true;
    }
    private static AppBarData CreateData() => new() { cbSize = (uint)Marshal.SizeOf<AppBarData>() };
}
