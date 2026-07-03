using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace BetterHome.Services;

public sealed class SystemStatusService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PowerStatus { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, Reserved; public int BatteryLifeTime, BatteryFullLifeTime; }

    [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out PowerStatus status);
    [DllImport("winmm.dll")] private static extern int waveOutGetVolume(IntPtr device, out uint volume);

    public string NetworkText => NetworkInterface.GetIsNetworkAvailable() ? "Connected" : "Disconnected";
    public string BatteryText
    {
        get
        {
            if (!GetSystemPowerStatus(out var value) || value.BatteryLifePercent == 255) return "Desktop power";
            return $"{value.BatteryLifePercent}%" + (value.ACLineStatus == 1 ? " · charging" : "");
        }
    }
    public int VolumePercent
    {
        get
        {
            if (waveOutGetVolume(IntPtr.Zero, out var value) != 0) return 0;
            return (int)Math.Round((value & 0xFFFF) * 100d / 65535d);
        }
    }
}
