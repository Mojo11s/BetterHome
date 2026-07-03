using System.Windows;
using System.Windows.Controls;
using BetterHome.Models;
using BetterHome.Services;

namespace BetterHome.Views;

public partial class SettingsWindow : Window
{
    private readonly StartupService _startup = new();
    private readonly SettingsService _settingsService = new();
    public AppSettings Settings { get; private set; }
    public bool ResetLayoutRequested { get; private set; }
    public bool WasSaved { get; private set; }
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent(); Settings = settings;
        StartToggle.IsChecked = _startup.IsEnabled; HideIconsToggle.IsChecked = settings.HideDesktopIconsOnLaunch; AutoToggle.IsChecked = settings.AutoArrangeOnLaunch;
        WidgetsToggle.IsChecked = settings.ShowWidgets; DockToggle.IsChecked = settings.ShowDock; FileHubToggle.IsChecked = settings.ShowFileHubOnLaunch;
        ThemeBox.SelectedIndex = settings.Theme switch { "Light" => 1, "System" => 2, _ => 0 };
        WeatherLocationBox.Text = settings.WeatherLocation;
        PrayerWidgetToggle.IsChecked = settings.EnablePrayerWidget; PrayerLocationBox.Text = settings.PrayerLocation; SelectContent(PrayerMethodBox, settings.PrayerCalculationMethod); SelectTag(PrayerReminderBox, settings.PrayerReminderMinutes.ToString()); PrayerSoundToggle.IsChecked = settings.EnablePrayerSound;
        LiveWallpaperToggle.IsChecked = settings.EnableLiveWallpaper; SelectContent(WallpaperTypeBox, settings.WallpaperType); SelectContent(WallpaperQualityBox, settings.WallpaperQuality); PauseBatteryToggle.IsChecked = settings.PauseWallpaperOnBattery; PauseFullscreenToggle.IsChecked = settings.PauseWallpaperOnFullscreen; PauseCpuToggle.IsChecked = settings.PauseWallpaperOnHighCpu;
        SelectContent(UiFontSizeBox, settings.UiFontSize.ToString("0")); SelectContent(FileHubFontSizeBox, settings.FileHubFontSize.ToString("0"));
        DynamicIslandToggle.IsChecked = settings.EnableDynamicIsland; AppDrawerToggle.IsChecked = settings.EnableAppDrawer; OpenAppsToggle.IsChecked = settings.EnableOpenAppsOverview; RunningStripToggle.IsChecked = settings.ShowRunningAppsStrip; MiniPlayerToggle.IsChecked = settings.ShowMiniPlayer; EdgeDockToggle.IsChecked = settings.EnableEdgeDock; AssistantToggle.IsChecked = settings.ShowAssistantBubble; SmartTrayToggle.IsChecked = settings.ShowSmartTray; ConfirmCloseToggle.IsChecked = settings.ConfirmBeforeClosingApps;
    }
    private void Done_Click(object sender, RoutedEventArgs e)
    {
        try { _startup.SetEnabled(StartToggle.IsChecked == true); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "BetterHome startup", MessageBoxButton.OK, MessageBoxImage.Error); return; }
        Settings.StartWithWindows = StartToggle.IsChecked == true; Settings.HideDesktopIconsOnLaunch = HideIconsToggle.IsChecked == true; Settings.AutoArrangeOnLaunch = AutoToggle.IsChecked == true;
        Settings.ShowWidgets = WidgetsToggle.IsChecked == true; Settings.ShowDock = DockToggle.IsChecked == true; Settings.ShowFileHubOnLaunch = FileHubToggle.IsChecked == true; Settings.Theme = ((ComboBoxItem)ThemeBox.SelectedItem).Content?.ToString() ?? "Dark"; Settings.WeatherLocation = string.IsNullOrWhiteSpace(WeatherLocationBox.Text) ? "Cairo, Egypt" : WeatherLocationBox.Text.Trim();
        Settings.EnablePrayerWidget = PrayerWidgetToggle.IsChecked == true; Settings.PrayerLocation = string.IsNullOrWhiteSpace(PrayerLocationBox.Text) ? "Cairo, Egypt" : PrayerLocationBox.Text.Trim(); Settings.PrayerCalculationMethod = ((ComboBoxItem)PrayerMethodBox.SelectedItem).Content?.ToString() ?? "Egyptian General Authority"; Settings.PrayerReminderMinutes = int.TryParse(((ComboBoxItem)PrayerReminderBox.SelectedItem).Tag?.ToString(), out var reminder) ? reminder : 0; Settings.EnablePrayerSound = PrayerSoundToggle.IsChecked == true;
        Settings.EnableLiveWallpaper = LiveWallpaperToggle.IsChecked == true; Settings.WallpaperType = ((ComboBoxItem)WallpaperTypeBox.SelectedItem).Content?.ToString() ?? "Static"; Settings.WallpaperQuality = ((ComboBoxItem)WallpaperQualityBox.SelectedItem).Content?.ToString() ?? "Medium"; Settings.PauseWallpaperOnBattery = PauseBatteryToggle.IsChecked == true; Settings.PauseWallpaperOnFullscreen = PauseFullscreenToggle.IsChecked == true; Settings.PauseWallpaperOnHighCpu = PauseCpuToggle.IsChecked == true;
        Settings.UiFontSize = double.TryParse(((ComboBoxItem)UiFontSizeBox.SelectedItem).Content?.ToString(), out var uiFont) ? uiFont : 12; Settings.FileHubFontSize = double.TryParse(((ComboBoxItem)FileHubFontSizeBox.SelectedItem).Content?.ToString(), out var hubFont) ? hubFont : 12;
        Settings.EnableDynamicIsland = DynamicIslandToggle.IsChecked == true; Settings.EnableAppDrawer = AppDrawerToggle.IsChecked == true; Settings.EnableOpenAppsOverview = OpenAppsToggle.IsChecked == true; Settings.ShowRunningAppsStrip = RunningStripToggle.IsChecked == true; Settings.ShowMiniPlayer = MiniPlayerToggle.IsChecked == true; Settings.EnableEdgeDock = EdgeDockToggle.IsChecked == true; Settings.ShowAssistantBubble = AssistantToggle.IsChecked == true; Settings.ShowSmartTray = SmartTrayToggle.IsChecked == true; Settings.ConfirmBeforeClosingApps = ConfirmCloseToggle.IsChecked == true;
        _settingsService.Save(Settings); WasSaved = true; Close();
    }
    private void ResetLayout_Click(object sender, RoutedEventArgs e) => ResetLayoutRequested = true;
    private void ResetAll_Click(object sender, RoutedEventArgs e) { if (MessageBox.Show("Reset all BetterHome settings?", "BetterHome", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; Settings = new AppSettings(); _settingsService.Reset(); StartToggle.IsChecked = false; HideIconsToggle.IsChecked = false; AutoToggle.IsChecked = true; WidgetsToggle.IsChecked = true; DockToggle.IsChecked = true; FileHubToggle.IsChecked = true; ThemeBox.SelectedIndex = 0; WeatherLocationBox.Text = "Cairo, Egypt"; PrayerWidgetToggle.IsChecked = true; PrayerLocationBox.Text = "Cairo, Egypt"; PrayerMethodBox.SelectedIndex = 0; PrayerReminderBox.SelectedIndex = 0; PrayerSoundToggle.IsChecked = false; LiveWallpaperToggle.IsChecked = true; WallpaperTypeBox.SelectedIndex = 0; WallpaperQualityBox.SelectedIndex = 1; PauseBatteryToggle.IsChecked = true; PauseFullscreenToggle.IsChecked = true; PauseCpuToggle.IsChecked = true; UiFontSizeBox.SelectedIndex = 1; FileHubFontSizeBox.SelectedIndex = 1; ResetLayoutRequested = true; }
    private void Cancel_Click(object sender, RoutedEventArgs e) { WasSaved = false; Close(); }
    private static void SelectContent(ComboBox box, string value) { box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0]; }
    private static void SelectTag(ComboBox box, string value) { box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == value) ?? box.Items[0]; }
}
