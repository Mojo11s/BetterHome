using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BetterHome.Models;

namespace BetterHome.Services;

public sealed class RunningWindowsService
{
    private delegate bool EnumProc(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, nint parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint handle);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(nint handle);
    [DllImport("user32.dll")] private static extern int GetWindowText(nint handle, StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint processId);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(nint handle, int command);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint handle, uint message, nint wParam, nint lParam);

    public IReadOnlyList<RunningWindow> GetOpenWindows()
    {
        var result = new List<RunningWindow>(); var active = GetForegroundWindow();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || GetWindowTextLength(handle) <= 0) return true;
            var title = new StringBuilder(GetWindowTextLength(handle) + 1); GetWindowText(handle, title, title.Capacity);
            GetWindowThreadProcessId(handle, out var id);
            try
            {
                using var process = Process.GetProcessById((int)id); if (process.ProcessName.Equals("BetterHome", StringComparison.OrdinalIgnoreCase)) return true;
                string? path = null; try { path = process.MainModule?.FileName; } catch { }
                result.Add(new RunningWindow(handle, title.ToString(), process.ProcessName, path, IsIconic(handle), handle == active));
            }
            catch { }
            return true;
        }, 0);
        return result.OrderByDescending(window => window.IsActive).ThenBy(window => window.ProcessName).ToList();
    }

    public void Focus(RunningWindow window) { ShowWindowAsync(window.Handle, 9); SetForegroundWindow(window.Handle); }
    public void Minimize(RunningWindow window) => ShowWindowAsync(window.Handle, 6);
    public void Close(RunningWindow window) => PostMessage(window.Handle, 0x0010, 0, 0);
}
