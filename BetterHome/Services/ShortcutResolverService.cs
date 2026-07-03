namespace BetterHome.Services;

public sealed class ShortcutResolverService
{
    public string? Resolve(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return path;
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(path);
            return shortcut.TargetPath as string;
        }
        catch { return null; }
    }
}
