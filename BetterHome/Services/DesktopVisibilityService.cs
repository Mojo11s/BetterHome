using System.Runtime.InteropServices;

namespace BetterHome.Services;

public sealed class DesktopVisibilityService
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string? windowName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    private const uint WM_COMMAND = 0x111, ToggleDesktopIcons = 0x7402, AbortIfHung = 0x2;

    public bool TrySetVisible(bool visible, out string? error)
    {
        error = null;
        try
        {
            var view = FindDesktopView(); if (view == IntPtr.Zero) { error = "Windows desktop view was not found."; return false; }
            var list = FindWindowEx(view, IntPtr.Zero, "SysListView32", "FolderView"); var current = list == IntPtr.Zero || IsWindowVisible(list);
            if (current != visible && SendMessageTimeout(view, WM_COMMAND, new IntPtr(ToggleDesktopIcons), IntPtr.Zero, AbortIfHung, 1500, out _) == IntPtr.Zero) { error = "Windows did not respond while changing desktop icon visibility."; return false; }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
    private static IntPtr FindDesktopView()
    {
        var progman = FindWindow("Progman", null); var view = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null); if (view != IntPtr.Zero) return view;
        var worker = IntPtr.Zero; while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero) { view = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null); if (view != IntPtr.Zero) return view; } return IntPtr.Zero;
    }
}
