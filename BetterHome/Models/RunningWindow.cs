namespace BetterHome.Models;
public sealed record RunningWindow(nint Handle, string Title, string ProcessName, string? ProcessPath, bool IsMinimized, bool IsActive);
