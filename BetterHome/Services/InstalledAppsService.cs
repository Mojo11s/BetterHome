using BetterHome.Models;

namespace BetterHome.Services;

public sealed class InstalledAppsService
{
    public IReadOnlyList<InstalledApp> Load()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };
        return roots.Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(Path.Combine(root, "Programs"), "*.lnk", SearchOption.AllDirectories))
            .Select(path => new InstalledApp(Path.GetFileNameWithoutExtension(path), path))
            .GroupBy(app => app.Name, StringComparer.OrdinalIgnoreCase).Select(group => group.First())
            .OrderBy(app => app.Name).ToList();
    }
}
