using System.Runtime.InteropServices;
using BetterHome.Models;

namespace BetterHome.Services;

public sealed class MediaSessionService
{
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nint extra);
    private const uint Up = 2;
    public string CurrentTitle(RunningWindowsService windows) => windows.GetOpenWindows().FirstOrDefault(w =>
        w.ProcessName.Contains("spotify", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Contains("chrome", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Contains("msedge", StringComparison.OrdinalIgnoreCase))?.Title ?? "No media playing";
    public void PlayPause() => Press(0xB3);
    public void Next() => Press(0xB0);
    public void Previous() => Press(0xB1);
    public void VolumeUp() => Press(0xAF);
    public void VolumeDown() => Press(0xAE);
    private static void Press(byte key) { keybd_event(key, 0, 0, 0); keybd_event(key, 0, Up, 0); }
}
