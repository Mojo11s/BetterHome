using System.Windows.Media;

namespace BetterHome.Models;

public sealed class DesktopItem
{
    public required string Name { get; set; }
    public required string Path { get; init; }
    public string? TargetPath { get; init; }
    public required string Category { get; set; }
    public bool IsDirectory { get; init; }
    public bool IsHidden { get; init; }
    public ImageSource? Icon { get; init; }
}
