using System.Runtime.InteropServices;
namespace BetterHome.Services;
public sealed class SystemMetricsService
{
    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low, High; public ulong Value => ((ulong)High << 32) | Low; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] private sealed class MemoryStatus { public uint Length = (uint)Marshal.SizeOf<MemoryStatus>(); public uint MemoryLoad; public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual; }
    [DllImport("kernel32.dll")] private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus status);
    private ulong _idle, _kernel, _user;
    public (double Cpu, double Ram, double Disk) Read()
    {
        double cpu = 0; if (GetSystemTimes(out var idle, out var kernel, out var user)) { var idleDelta = idle.Value - _idle; var totalDelta = kernel.Value - _kernel + user.Value - _user; if (_kernel != 0 && totalDelta > 0) cpu = 100d * (totalDelta - idleDelta) / totalDelta; _idle = idle.Value; _kernel = kernel.Value; _user = user.Value; }
        var memory = new MemoryStatus(); var ram = GlobalMemoryStatusEx(memory) ? memory.MemoryLoad : 0;
        double disk = 0; try { var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!); disk = 100d * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize; } catch { }
        return (Math.Clamp(cpu, 0, 100), ram, disk);
    }
}
