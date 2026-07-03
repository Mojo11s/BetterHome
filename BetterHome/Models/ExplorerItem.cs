using System.Windows.Media;

namespace BetterHome.Models;

public sealed class ExplorerItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string Type { get; init; }
    public string SizeText { get; init; } = "";
    public long Size { get; init; }
    public DateTime DateModified { get; init; }
    public bool IsDirectory { get; init; }
    public ImageSource? Icon { get; init; }
}
