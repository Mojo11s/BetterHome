using Microsoft.Win32;

namespace BetterHome.Services;

public sealed class StartupService
{
    private const string Name = "BetterHome";
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public bool IsEnabled { get { using var key = Registry.CurrentUser.OpenSubKey(KeyPath); return key?.GetValue(Name) is string; } }
    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        if (enabled) key.SetValue(Name, $"\"{Environment.ProcessPath}\""); else key.DeleteValue(Name, false);
    }
}
