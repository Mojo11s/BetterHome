namespace BetterHome.Models;
public sealed class DockShortcut { public string Id { get; set; } = $"custom-{Guid.NewGuid():N}"; public string Name { get; set; } = "Shortcut"; public string Path { get; set; } = ""; }
