namespace BetterHome.Models;

public sealed class PanelPosition { public double X { get; set; } public double Y { get; set; } }

public sealed class LayoutState
{
    public PanelPosition FileHub { get; set; } = new();
    public PanelPosition Work { get; set; } = new();
    public PanelPosition Apps { get; set; } = new();
    public PanelPosition Folders { get; set; } = new();
    public PanelPosition Games { get; set; } = new();
    public bool WorkCollapsed { get; set; }
    public bool AppsCollapsed { get; set; }
    public bool FoldersCollapsed { get; set; }
    public bool GamesCollapsed { get; set; }
    public bool GroupsVisible { get; set; } = true;
    public bool WidgetsVisible { get; set; } = true;
    public bool DockVisible { get; set; } = true;
    public bool FileHubVisible { get; set; } = true;
    public bool AutoArrangeEnabled { get; set; } = true;
    public bool DesktopModeEnabled { get; set; }
    public bool StartWithWindows { get; set; }
    public bool DarkTheme { get; set; } = true;
    public List<string> TodoTasks { get; set; } = [];
    public List<string> CompletedTasks { get; set; } = [];
}
