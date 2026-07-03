using BetterHome.Models;

namespace BetterHome.Services;

public sealed class ThemeService
{
    public IReadOnlyList<BetterHomeTheme> Themes { get; } =
    [
        new("BetterHome Glass Dark", "#0B172B", "#2D91E8", .94, 16), new("Clean White", "#E8EEF7", "#2678D8", .92, 14),
        new("Midnight Blue", "#07152F", "#438BFF", .95, 18), new("Gaming Neon", "#130B27", "#A344FF", .92, 12),
        new("Productivity Calm", "#122925", "#42B89B", .94, 14), new("Ramadan Night", "#101735", "#CDAA57", .95, 18),
        new("Eid Celebration", "#10302A", "#58D6A8", .92, 18), new("Minimal Black", "#070707", "#B7C5D8", .97, 8),
        new("Ocean Glass", "#082838", "#22B8D5", .91, 20), new("Anime Soft", "#30243B", "#F18BBF", .90, 20)
    ];
}
