namespace BetterHome.Models;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool HideDesktopIconsOnLaunch { get; set; }
    public bool AutoArrangeOnLaunch { get; set; } = true;
    public bool ShowWidgets { get; set; } = true;
    public bool ShowDock { get; set; } = true;
    public bool ShowFileHubOnLaunch { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public string WeatherLocation { get; set; } = "Cairo, Egypt";
    public List<string> DockOrder { get; set; } = ["home", "search", "filehub", "browser", "vscode", "spotify", "settings", "recycle"];
    public List<string> HiddenDockItems { get; set; } = [];
    public double DockScale { get; set; } = 1;
    public double DockOpacity { get; set; } = 1;
    public List<string> WidgetOrder { get; set; } = ["Greeting", "Calendar", "To Do", "Weather", "Prayer", "System"];
    public List<string> HiddenWidgets { get; set; } = [];
    public double WidgetOpacity { get; set; } = 1;
    public string? WallpaperPath { get; set; }
    public List<DockShortcut> CustomDockItems { get; set; } = [];
    public string? AnimatedWallpaperPath { get; set; }
    public bool WallpaperAnimationEnabled { get; set; } = true;
    public bool EnablePrayerWidget { get; set; } = true;
    public string PrayerLocation { get; set; } = "Cairo, Egypt";
    public string PrayerCalculationMethod { get; set; } = "Egyptian General Authority";
    public int PrayerReminderMinutes { get; set; }
    public bool EnablePrayerSound { get; set; }
    public bool EnableLiveWallpaper { get; set; } = true;
    public string WallpaperType { get; set; } = "Static";
    public string WallpaperQuality { get; set; } = "Medium";
    public bool PauseWallpaperOnBattery { get; set; } = true;
    public bool PauseWallpaperOnFullscreen { get; set; } = true;
    public bool PauseWallpaperOnHighCpu { get; set; } = true;
    public double UiFontSize { get; set; } = 12;
    public double FileHubFontSize { get; set; } = 12;
    public bool EnableDynamicIsland { get; set; } = true;
    public bool EnableAppDrawer { get; set; } = true;
    public bool EnableOpenAppsOverview { get; set; } = true;
    public bool ShowRunningAppsStrip { get; set; }
    public bool ConfirmBeforeClosingApps { get; set; } = true;
    public bool ShowMiniPlayer { get; set; } = true;
    public bool EnableEdgeDock { get; set; } = true;
    public bool ShowAssistantBubble { get; set; } = true;
    public bool ShowSmartTray { get; set; } = true;
    public string SelectedTheme { get; set; } = "BetterHome Glass Dark";
    public List<string> FavoriteThemes { get; set; } = [];
    public bool ShowHiddenDesktopItems { get; set; }
    public Dictionary<string, double> DesktopGroupWidths { get; set; } = [];
    public Dictionary<string, double> DesktopGroupHeights { get; set; } = [];
}
