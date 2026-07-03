using BetterHome.Models;

namespace BetterHome.Services;

public sealed class DesktopItemService(IconExtractionService icons, ShortcutResolverService shortcuts, GroupAssignmentService groups)
{
    private readonly Dictionary<string, string> _assignments = groups.Load();
    private static readonly string[] DocumentExtensions = [".txt", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".csv", ".md", ".rtf"];
    private static readonly string[] MediaExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".mp4", ".mkv", ".avi", ".mov", ".mp3", ".wav"];

    public IReadOnlyList<DesktopItem> GetDesktopItems()
    {
        var paths = new[] { Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) };
        return paths.Where(Directory.Exists).SelectMany(GetSafeEntries).Distinct(StringComparer.OrdinalIgnoreCase).Select(Create).Where(x => x != null).Cast<DesktopItem>().OrderBy(x => x.Name).ToList();
    }

    public void Assign(string path, string category) { _assignments[path] = category; groups.Save(_assignments); }

    private DesktopItem? Create(string path)
    {
        try
        {
            var directory = Directory.Exists(path); var extension = Path.GetExtension(path).ToLowerInvariant(); var target = shortcuts.Resolve(path);
            var name = directory ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);
            var category = _assignments.GetValueOrDefault(path) ?? Categorize(name, path, target, directory, extension);
            var attributes = File.GetAttributes(path); var hidden = attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System);
            return new DesktopItem { Name = name, Path = path, TargetPath = target, Category = category, IsDirectory = directory, IsHidden = hidden, Icon = icons.GetIcon(path, directory) };
        }
        catch { return null; }
    }

    private static string Categorize(string name, string path, string? target, bool directory, string extension)
    {
        var combined = $"{name} {path} {target}";
        if (combined.Contains("steam", StringComparison.OrdinalIgnoreCase) || combined.Contains("game", StringComparison.OrdinalIgnoreCase) || combined.Contains("minecraft", StringComparison.OrdinalIgnoreCase) || combined.Contains("valorant", StringComparison.OrdinalIgnoreCase)) return "Games";
        if (directory) return "Folders";
        if (extension is ".lnk" or ".url" or ".exe") return "Apps";
        if (DocumentExtensions.Contains(extension) || MediaExtensions.Contains(extension)) return "Work";
        return "Work";
    }

    private static IEnumerable<string> GetSafeEntries(string path) { try { return Directory.EnumerateFileSystemEntries(path).ToArray(); } catch { return []; } }
}
