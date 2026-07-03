using BetterHome.Models;

namespace BetterHome.Services;

public sealed class FileExplorerService(IconExtractionService icons)
{
    public IReadOnlyList<ExplorerItem> GetItems(string path)
    {
        var result = new List<ExplorerItem>();
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                try
                {
                    var directory = Directory.Exists(entry); var info = directory ? (FileSystemInfo)new DirectoryInfo(entry) : new FileInfo(entry); var size = directory ? 0 : ((FileInfo)info).Length;
                    result.Add(new ExplorerItem { Name = info.Name, FullPath = entry, IsDirectory = directory, Type = directory ? "File folder" : GetTypeName(info.Extension), Size = size, SizeText = directory ? "" : FormatSize(size), DateModified = info.LastWriteTime, Icon = icons.GetIcon(entry, directory) });
                }
                catch { }
            }
        }
        catch (Exception ex) { throw new IOException($"Could not open {path}", ex); }
        return result;
    }
    private static string GetTypeName(string extension) => string.IsNullOrWhiteSpace(extension) ? "File" : $"{extension.TrimStart('.').ToUpperInvariant()} file";
    private static string FormatSize(long value) { string[] units = ["B", "KB", "MB", "GB", "TB"]; double size = value; var unit = 0; while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; } return $"{size:0.#} {units[unit]}"; }
}
