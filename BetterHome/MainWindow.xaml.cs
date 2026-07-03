using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Rectangle = System.Windows.Shapes.Rectangle;
using Ellipse = System.Windows.Shapes.Ellipse;
using System.Windows.Threading;
using BetterHome.Models;
using BetterHome.Services;
using BetterHome.ViewModels;
using System.Runtime.InteropServices;

namespace BetterHome;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nint extraInfo);
    private readonly LayoutStorageService _storage = new();
    private readonly IconExtractionService _icons = new();
    private readonly ShortcutResolverService _shortcuts = new();
    private readonly GroupAssignmentService _groupAssignments = new();
    private readonly FileOperationService _fileOperations = new();
    private readonly DesktopVisibilityService _desktopVisibility = new();
    private readonly SettingsService _settingsService = new();
    private readonly TaskbarService _taskbarService = new();
    private readonly ObservableCollection<string> _searchResults = [];
    private readonly string[] _allItems = ["This PC", "Documents", "Project Files", "Recycle Bin", "Google Chrome", "Spotify", "VS Code", "Notion", "Photoshop", "Discord", "Figma", "Projects", "University", "Personal", "Notes", "Resources", "Archive", "Steam", "Epic Games", "Minecraft", "Genshin Impact", "Valorant", "EA App"];
    private readonly Dictionary<Border, (double Height, bool Collapsed)> _groups = [];
    private readonly Dictionary<Border, Panel> _groupContentHosts = [];
    private Canvas? _groupsOverlay;
    private DesktopItem? _selectedDesktopItem;
    private Border? _resizingGroup;
    private Point _resizeStart;
    private double _resizeStartWidth, _resizeStartHeight;
    private LayoutState _state = new();
    private AppSettings _settings = new();
    private DesktopItemService? _desktopItemsService;
    private List<DesktopItem> _desktopItems = [];
    private Views.FileHubView? _realFileHub;
    private readonly List<DockItemViewModel> _dockItems = [];
    private Views.SettingsWindow? _settingsWindow;
    private FrameworkElement? _dragging;
    private Point _dragStart;
    private double _startLeft, _startTop;
    private Border? _toast, _searchOverlay, _inputOverlay;
    private Canvas? _fileHubOverlay;
    private Border? _dock;
    private StackPanel? _widgets;
    private ToggleButton? _hideGroupsToggle;
    private bool _fileHubMinimized, _fileHubMaximized;
    private bool _loading;
    private double _fileHubWidth = 420, _fileHubHeight = 292, _fileHubLeft, _fileHubTop;
    private string[] _sortModes = ["Name", "Date", "Type"];
    private string[] _viewModes = ["List", "Grid"];
    private int _sortIndex, _viewIndex;
    private readonly TodoService _todoService = new();
    private readonly WeatherService _weatherService = new();
    private readonly SystemMetricsService _metricsService = new();
    private readonly PrayerTimesService _prayerTimesService = new();
    private readonly List<TodoItem> _todos = [];
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _metricsTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _prayerTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedDate;
    private TextBlock? _greetingText, _clockText, _dateText, _weatherTemp, _weatherDescription, _weatherStats;
    private StackPanel? _todoHost;
    private UniformGrid? _calendarGrid;
    private TextBlock? _calendarTitle;
    private readonly List<ProgressBar> _metricBars = [];
    private readonly List<TextBlock> _metricValues = [];
    private PrayerTimesData? _prayerTimes;
    private TextBlock? _nextPrayerText, _prayerLocationText;
    private UniformGrid? _prayerGrid;
    private Border? _prayerCard;
    private readonly HashSet<string> _prayerRemindersShown = [];
    private bool _desktopView;
    private bool _dockRefreshQueued;
    private MediaElement? _wallpaperVideo;
    private Image? _wallpaperImage;
    private Rectangle? _wallpaperEffect;
    private Button? _wallpaperPauseButton;
    private readonly WallpaperPerformanceService _wallpaperPerformance = new();
    private readonly SystemStatusService _systemStatus = new();
    private readonly InstalledAppsService _installedApps = new();
    private readonly RunningWindowsService _runningWindows = new();
    private readonly DynamicIslandService _dynamicIslandService = new();
    private readonly MediaSessionService _mediaSession = new();
    private readonly ThemeService _themeService = new();
    private Border? _smartTray;
    private Border? _featureOverlay, _dynamicIsland;
    private TextBlock? _dynamicIslandText;
    private Border? _miniPlayer, _edgeDock, _assistantPanel, _runningStrip;
    private Button? _assistantBubble;
    private readonly DispatcherTimer _runningAppsTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private bool _runningAppsTimerHooked;
    private string _runningDockSignature = "";
    private readonly DispatcherTimer _wallpaperGuardTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _wallpaperManuallyPaused, _wallpaperPolicyPaused;
    private double _lastCpuUsage;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += (_, _) => { SaveLayout(); _taskbarService.SetVisible(true); };
        SizeChanged += (_, _) => ApplyResponsiveScale();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load(); FontSize = _settings.UiFontSize;
        EnableGlobalCustomization();
        _taskbarService.SetVisible(false);
        Dispatcher.BeginInvoke(async () => { await Task.Delay(900); _taskbarService.SetVisible(false); });
        _desktopItemsService = new DesktopItemService(_icons, _shortcuts, _groupAssignments);
        _widgets = FindAncestorStack(WorkGroup, false);
        _dock = DockBar;
        BuildRealDock();
        ConfigureGroups();
        InstallRealFileHub();
        EnableFileHubCustomization();
        WireControls();
        InitializeRealWidgets();
        EnableWidgetCustomization();
        LoadLayout();
        ApplySettingsOnLaunch();
        ApplyResponsiveScale();
        InitializeDynamicIsland();
        InitializePremiumFeatures();
        _wallpaperGuardTimer.Tick += (_, _) => EvaluateWallpaperPlayback(); _wallpaperGuardTimer.Start(); EvaluateWallpaperPlayback();
    }

    private void InitializeRealWidgets()
    {
        _greetingText = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.StartsWith("Good "));
        _clockText = Descendants<TextBlock>(this).FirstOrDefault(t => t.FontSize >= 24 && t.Text.Contains("08:34"));
        _dateText = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.Contains("Monday, May 20"));
        _clockTimer.Tick += (_, _) => UpdateClock(); _clockTimer.Start(); UpdateClock();
        BuildCalendarWidget(); BuildTodoWidget(); BuildWeatherWidget(); BuildPrayerWidget(); BuildSystemWidget();
        _metricsTimer.Tick += (_, _) => UpdateSystemMetrics(); _metricsTimer.Start(); UpdateSystemMetrics();
        _prayerTimer.Tick += (_, _) => UpdatePrayerCountdown(); _prayerTimer.Start();
        _ = RefreshWeatherAsync(false);
        _ = RefreshPrayerTimesAsync(false);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TrayClockText.Text = now.ToString("hh:mm tt");
        TrayDateText.Text = now.ToString("M/d/yyyy");
        if (_greetingText != null) _greetingText.Text = now.Hour < 12 ? "Good morning" : now.Hour < 18 ? "Good afternoon" : "Good evening";
        if (_clockText != null) _clockText.Text = now.ToString("hh:mm:ss tt");
        if (_dateText != null) _dateText.Text = now.ToString("dddd, MMMM d, yyyy");
    }

    private void TrayArea_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (!_settings.ShowSmartTray) { ShowToast("Smart Tray is disabled in Settings"); return; }
        if (_smartTray is { Parent: not null }) { RootGrid.Children.Remove(_smartTray); _smartTray = null; return; }

        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = "BetterHome Smart Tray", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 2, 4, 13) });
        content.Children.Add(StatusRow("Network", _systemStatus.NetworkText, "ms-settings:network-status"));
        content.Children.Add(StatusRow("Volume", $"{_systemStatus.VolumePercent}%", "ms-settings:sound"));
        content.Children.Add(StatusRow("Battery", _systemStatus.BatteryText, "ms-settings:batterysaver"));
        content.Children.Add(StatusRow("Date & time", DateTime.Now.ToString("ddd, MMM d · hh:mm tt"), "ms-settings:dateandtime"));
        var refresh = new Button { Content = "Refresh status", Height = 34, Margin = new Thickness(4, 12, 4, 0), Style = (Style)FindResource("IconButton"), Background = new SolidColorBrush(Color.FromRgb(39, 75, 119)), ToolTip = "Read current Windows status again" };
        refresh.Click += (_, _) => { RootGrid.Children.Remove(_smartTray); _smartTray = null; TrayArea_Click(TrayArea, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)); };
        content.Children.Add(refresh);
        _smartTray = new Border { Width = 330, Padding = new Thickness(15), CornerRadius = new CornerRadius(16), Background = new SolidColorBrush(Color.FromArgb(245, 14, 27, 50)), BorderBrush = new SolidColorBrush(Color.FromRgb(55, 99, 148)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 22, 76), Child = content };
        Panel.SetZIndex(_smartTray, 500); RootGrid.Children.Add(_smartTray);
    }

    private Grid StatusRow(string label, string value, string settingsUri)
    {
        var row = new Grid { Height = 43, Margin = new Thickness(4, 2, 4, 2), Background = new SolidColorBrush(Color.FromRgb(27, 47, 76)), Cursor = Cursors.Hand, ToolTip = $"Open Windows {label} settings" };
        row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock { Text = label, Margin = new Thickness(11, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
        var status = new TextBlock { Text = value, Foreground = new SolidColorBrush(Color.FromRgb(151, 181, 219)), Margin = new Thickness(8, 0, 11, 0), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(status, 1); row.Children.Add(status);
        row.MouseLeftButtonDown += (_, e) => { e.Handled = true; SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(settingsUri) { UseShellExecute = true })); };
        return row;
    }

    private void BuildCalendarWidget()
    {
        var card = FindWidgetCard("Calendar"); if (card == null) return;
        var root = new StackPanel(); root.Children.Add(new TextBlock { Text = "Calendar", FontSize = 13, FontWeight = FontWeights.SemiBold });
        var nav = new Grid { Margin = new Thickness(0, 3, 0, 1) }; var previous = SmallWidgetButton("‹", "Previous month"); var next = SmallWidgetButton("›", "Next month");
        _calendarTitle = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        next.HorizontalAlignment = HorizontalAlignment.Right; previous.HorizontalAlignment = HorizontalAlignment.Left; previous.Click += (_, _) => { _calendarMonth = _calendarMonth.AddMonths(-1); RenderCalendar(); }; next.Click += (_, _) => { _calendarMonth = _calendarMonth.AddMonths(1); RenderCalendar(); };
        nav.Children.Add(previous); nav.Children.Add(_calendarTitle); nav.Children.Add(next); root.Children.Add(nav);
        _calendarGrid = new UniformGrid { Columns = 7, Rows = 7, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 210 }; root.Children.Add(_calendarGrid); card.Child = root; RenderCalendar();
    }

    private void RenderCalendar()
    {
        if (_calendarGrid == null || _calendarTitle == null) return; _calendarGrid.Children.Clear(); _calendarTitle.Text = _calendarMonth.ToString("MMMM yyyy");
        foreach (var day in new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }) _calendarGrid.Children.Add(new TextBlock { Text = day, FontSize = 8, FontFamily = new FontFamily("Segoe UI"), TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, Foreground = (Brush)FindResource("TextMuted") });
        var first = (int)_calendarMonth.DayOfWeek; var days = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
        for (var i = 0; i < 42; i++)
        {
            var number = i - first + 1; if (number < 1 || number > days) { _calendarGrid.Children.Add(new Border()); continue; }
            var date = new DateTime(_calendarMonth.Year, _calendarMonth.Month, number);
            var label = new TextBlock { Text = number.ToString(), FontFamily = new FontFamily("Segoe UI"), FontSize = 8, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
            var cell = new Border { Height = 16, Margin = new Thickness(1, 0, 1, 0), CornerRadius = new CornerRadius(7), Background = date.Date == DateTime.Today ? new SolidColorBrush(Color.FromRgb(38, 136, 255)) : Brushes.Transparent, BorderBrush = _selectedDate == date ? Brushes.White : Brushes.Transparent, BorderThickness = new Thickness(1), Child = label, Cursor = Cursors.Hand, ToolTip = date.ToString("D") };
            cell.MouseLeftButtonDown += (_, e) => { e.Handled = true; _selectedDate = date; RenderCalendar(); }; _calendarGrid.Children.Add(cell);
        }
    }

    private void BuildTodoWidget()
    {
        var card = FindWidgetCard("To Do"); if (card == null) return; _todos.Clear(); _todos.AddRange(_todoService.Load());
        var root = new StackPanel(); var header = new Grid(); header.Children.Add(new TextBlock { Text = "To Do", FontSize = 13, FontWeight = FontWeights.SemiBold }); var add = SmallWidgetButton("＋", "Add task"); add.HorizontalAlignment = HorizontalAlignment.Right; add.Click += (_, _) => AddRealTask(); header.Children.Add(add); root.Children.Add(header);
        _todoHost = new StackPanel { Margin = new Thickness(0, 6, 0, 0) }; root.Children.Add(_todoHost); card.Child = root; RenderTodos();
    }

    private void RenderTodos()
    {
        if (_todoHost == null) return; _todoHost.Children.Clear();
        foreach (var todo in _todos)
        {
            var check = new CheckBox { Content = todo.Text, IsChecked = todo.IsCompleted, Foreground = (Brush)FindResource("TextPrimary"), FontSize = 10, Margin = new Thickness(0, 3, 0, 2), Cursor = Cursors.Hand, ToolTip = "Check or uncheck task" };
            check.Checked += (_, _) => { todo.IsCompleted = true; SaveTodos(); }; check.Unchecked += (_, _) => { todo.IsCompleted = false; SaveTodos(); };
            var menu = new ContextMenu(); var delete = new MenuItem { Header = "Delete task" }; delete.Click += (_, _) => { _todos.Remove(todo); SaveTodos(); RenderTodos(); ShowToast("Task deleted"); }; menu.Items.Add(delete); check.ContextMenu = menu; _todoHost.Children.Add(check);
        }
        var addRow = new Button { Content = "＋  Add Task", Style = (Style)FindResource("IconButton"), HorizontalAlignment = HorizontalAlignment.Left, Foreground = (Brush)FindResource("TextMuted"), FontSize = 10, ToolTip = "Add a new task" }; addRow.Click += (_, _) => AddRealTask(); _todoHost.Children.Add(addRow);
    }

    private void AddRealTask() => ShowInput("Add Task", "What needs doing?", value => { _todos.Add(new TodoItem { Text = value }); SaveTodos(); RenderTodos(); ShowToast("Task added"); });
    private void SaveTodos() => _todoService.Save(_todos);

    private void BuildWeatherWidget()
    {
        var card = FindWidgetCard("Weather"); if (card == null) return; var root = new StackPanel(); var header = new Grid(); header.Children.Add(new TextBlock { Text = "Weather", FontSize = 13, FontWeight = FontWeights.SemiBold }); var refresh = SmallWidgetButton("↻", "Refresh Cairo weather"); refresh.HorizontalAlignment = HorizontalAlignment.Right; refresh.Click += async (_, _) => await RefreshWeatherAsync(true); header.Children.Add(refresh); root.Children.Add(header);
        _weatherTemp = new TextBlock { FontSize = 30, Margin = new Thickness(0, 5, 0, 0) }; _weatherDescription = new TextBlock { FontSize = 10, Foreground = (Brush)FindResource("TextMuted") }; _weatherStats = new TextBlock { FontSize = 9, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("TextMuted") };
        root.Children.Add(_weatherTemp); root.Children.Add(_weatherDescription); root.Children.Add(_weatherStats); card.Child = root; var cached = _weatherService.LoadCache(); if (cached != null) ApplyWeather(cached); else { _weatherTemp.Text = "--° C"; _weatherDescription.Text = "Cairo, Egypt"; }
    }

    private async Task RefreshWeatherAsync(bool notify)
    {
        try { var data = await _weatherService.GetCairoAsync(); ApplyWeather(data); if (notify) ShowToast("Weather updated"); }
        catch { var cached = _weatherService.LoadCache(); if (cached != null) ApplyWeather(cached); ShowToast("Weather update failed"); }
    }
    private void ApplyWeather(WeatherData data) { if (_weatherTemp == null) return; _weatherTemp.Text = $"{data.Temperature:0}° C"; _weatherDescription!.Text = $"{WeatherService.Describe(data.WeatherCode)} · Cairo"; _weatherStats!.Text = $"Humidity  {data.Humidity:0}%       Visibility  {data.Visibility:0.#} km\nWind  {data.WindSpeed:0.#} km/h"; }

    private void BuildSystemWidget()
    {
        var card = FindWidgetCard("System"); if (card == null) return; _metricBars.Clear(); _metricValues.Clear(); var root = new StackPanel(); var header = new Grid(); header.Children.Add(new TextBlock { Text = "System", FontSize = 13, FontWeight = FontWeights.SemiBold }); var refresh = SmallWidgetButton("↻", "Refresh system usage"); refresh.HorizontalAlignment = HorizontalAlignment.Right; refresh.Click += (_, _) => UpdateSystemMetrics(); header.Children.Add(refresh); root.Children.Add(header);
        AddMetric(root, "CPU Usage", Brushes.DodgerBlue); AddMetric(root, "RAM Usage", Brushes.DeepSkyBlue); AddMetric(root, "Disk (C:)", new SolidColorBrush(Color.FromRgb(142, 101, 255))); card.Child = root;
    }
    private void AddMetric(StackPanel root, string label, Brush color) { var line = new Grid { Margin = new Thickness(0, 5, 0, 2) }; line.Children.Add(new TextBlock { Text = label, FontSize = 9 }); var value = new TextBlock { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right }; line.Children.Add(value); root.Children.Add(line); var bar = new ProgressBar { Height = 3, Minimum = 0, Maximum = 100, Background = new SolidColorBrush(Color.FromRgb(36, 51, 79)), Foreground = color }; root.Children.Add(bar); _metricValues.Add(value); _metricBars.Add(bar); }
    private void UpdateSystemMetrics() { var values = _metricsService.Read(); _lastCpuUsage = values.Cpu; var data = new[] { values.Cpu, values.Ram, values.Disk }; for (var i = 0; i < _metricBars.Count; i++) { _metricBars[i].Value = data[i]; _metricValues[i].Text = $"{data[i]:0}%"; } }

    private void BuildPrayerWidget()
    {
        if (_widgets == null) return; var card = new Border { Style = (Style)FindResource("RailCard"), Height = 135, Visibility = _settings.EnablePrayerWidget ? Visibility.Visible : Visibility.Collapsed }; _prayerCard = card;
        var root = new StackPanel(); var header = new Grid(); header.Children.Add(new TextBlock { Text = "Prayer Times", FontSize = 13, FontWeight = FontWeights.SemiBold }); var refresh = SmallWidgetButton("↻", "Refresh local prayer times"); refresh.HorizontalAlignment = HorizontalAlignment.Right; refresh.Click += async (_, _) => await RefreshPrayerTimesAsync(true); header.Children.Add(refresh); root.Children.Add(header);
        _prayerLocationText = new TextBlock { FontSize = 9, Foreground = (Brush)FindResource("TextMuted"), Margin = new Thickness(0, 2, 0, 3), TextTrimming = TextTrimming.CharacterEllipsis };
        _nextPrayerText = new TextBlock { FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(97, 194, 255)), Margin = new Thickness(0, 0, 0, 4) };
        _prayerGrid = new UniformGrid { Columns = 2, Rows = 3 }; root.Children.Add(_prayerLocationText); root.Children.Add(_nextPrayerText); root.Children.Add(_prayerGrid); card.Child = root;
        var systemCard = _widgets.Children.OfType<Border>().LastOrDefault(); var index = systemCard == null ? _widgets.Children.Count : _widgets.Children.IndexOf(systemCard); _widgets.Children.Insert(index, card); _prayerTimes = _prayerTimesService.LoadCache(); RenderPrayerTimes();
    }
    private async Task RefreshPrayerTimesAsync(bool notify)
    {
        try { _prayerTimes = await _prayerTimesService.GetAsync(_settings.PrayerLocation, _settings.PrayerCalculationMethod); RenderPrayerTimes(); if (notify) ShowToast("Prayer times updated"); }
        catch { _prayerTimes ??= _prayerTimesService.LoadCache(); RenderPrayerTimes(); ShowToast("Prayer times update failed — using cache"); }
    }
    private void RenderPrayerTimes()
    {
        if (_prayerGrid == null || _prayerLocationText == null || _nextPrayerText == null) return; _prayerGrid.Children.Clear();
        if (_prayerTimes == null) { _prayerLocationText.Text = "Detecting local location…"; _nextPrayerText.Text = "Waiting for prayer times"; return; }
        _prayerLocationText.Text = _prayerTimes.Location;
        foreach (var item in new[] { ("Fajr", _prayerTimes.Fajr), ("Dhuhr", _prayerTimes.Dhuhr), ("Asr", _prayerTimes.Asr), ("Maghrib", _prayerTimes.Maghrib), ("Isha", _prayerTimes.Isha) }) _prayerGrid.Children.Add(new TextBlock { Text = $"{item.Item1}  {DateTime.Today.Add(item.Item2):hh:mm tt}", FontSize = 9, Margin = new Thickness(0, 2, 0, 1), ToolTip = $"{item.Item1} prayer" });
        UpdatePrayerCountdown();
    }
    private void UpdatePrayerCountdown()
    {
        if (_prayerTimes == null || _nextPrayerText == null) return; var now = DateTime.Now; var prayers = new[] { (Name: "Fajr", Time: _prayerTimes.Fajr), (Name: "Dhuhr", Time: _prayerTimes.Dhuhr), (Name: "Asr", Time: _prayerTimes.Asr), (Name: "Maghrib", Time: _prayerTimes.Maghrib), (Name: "Isha", Time: _prayerTimes.Isha) };
        var next = prayers.Select(p => (p.Name, Time: DateTime.Today.Add(p.Time))).FirstOrDefault(p => p.Time > now); if (next.Time == default) next = ("Fajr", DateTime.Today.AddDays(1).Add(_prayerTimes.Fajr)); var remaining = next.Time - now; _nextPrayerText.Text = $"Next: {next.Name} · {(int)remaining.TotalHours:00}:{remaining.Minutes:00}";
        if (_settings.PrayerReminderMinutes <= 0 || remaining.TotalMinutes > _settings.PrayerReminderMinutes || remaining.TotalMinutes < 0) return; var key = $"{next.Name}-{next.Time:yyyyMMddHHmm}"; if (!_prayerRemindersShown.Add(key)) return; ShowToast($"{next.Name} prayer in {_settings.PrayerReminderMinutes} minutes"); if (_settings.EnablePrayerSound) try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
    }

    private Border? FindWidgetCard(string title) { var text = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text == title || (title == "To Do" && t.Text == "To Do") || (title == "Calendar" && t.Text == "Calendar")); return text == null ? null : FindAncestor<Border>(text); }
    private Button SmallWidgetButton(string content, string tooltip) => new() { Content = content, Width = 26, Height = 22, Padding = new Thickness(0), Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, ToolTip = tooltip };

    private void EnableWidgetCustomization()
    {
        if (_widgets == null) return; string[] names = ["Greeting", "Calendar", "To Do", "Weather", "Prayer", "System"]; var cards = _widgets.Children.OfType<Border>().Take(names.Length).ToList();
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i]; var name = names[i]; card.Tag = name; card.Opacity = _settings.WidgetOpacity; card.Cursor = Cursors.Hand;
            card.MouseLeftButtonDown += (_, _) => { foreach (var other in cards) other.BorderBrush = (Brush)FindResource("Line"); card.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 151, 234)); };
            var menu = new ContextMenu(); AddMenu(menu, "Move up", () => MoveWidget(name, -1)); AddMenu(menu, "Move down", () => MoveWidget(name, 1)); AddMenu(menu, "Hide widget", () => HideWidget(name)); menu.Items.Add(new Separator()); AddMenu(menu, "Opacity 70%", () => SetWidgetOpacity(.7)); AddMenu(menu, "Opacity 85%", () => SetWidgetOpacity(.85)); AddMenu(menu, "Opacity 100%", () => SetWidgetOpacity(1)); AddMenu(menu, "Reset widgets", ResetWidgets); card.ContextMenu = menu;
        }
        ApplyWidgetPreferences();
    }

    private void ApplyWidgetPreferences()
    {
        if (_widgets == null) return; var cards = _widgets.Children.OfType<Border>().Where(b => b.Tag is string).ToDictionary(b => b.Tag!.ToString()!, StringComparer.OrdinalIgnoreCase);
        foreach (var card in cards.Values) _widgets.Children.Remove(card);
        var insert = 0; foreach (var name in _settings.WidgetOrder.Concat(cards.Keys).Distinct(StringComparer.OrdinalIgnoreCase)) if (cards.TryGetValue(name, out var card)) { card.Visibility = _settings.HiddenWidgets.Contains(name, StringComparer.OrdinalIgnoreCase) ? Visibility.Collapsed : Visibility.Visible; card.Opacity = _settings.WidgetOpacity; _widgets.Children.Insert(insert++, card); }
    }
    private void MoveWidget(string name, int delta) { var order = _settings.WidgetOrder; var index = order.IndexOf(name); if (index < 0) { order.Add(name); index = order.Count - 1; } var target = Math.Clamp(index + delta, 0, order.Count - 1); order.RemoveAt(index); order.Insert(target, name); _settingsService.Save(_settings); ApplyWidgetPreferences(); }
    private void HideWidget(string name) { if (!_settings.HiddenWidgets.Contains(name)) _settings.HiddenWidgets.Add(name); _settingsService.Save(_settings); ApplyWidgetPreferences(); ShowToast($"{name} widget hidden"); }
    private void SetWidgetOpacity(double opacity) { _settings.WidgetOpacity = opacity; _settingsService.Save(_settings); ApplyWidgetPreferences(); }
    private void ResetWidgets() { _settings.WidgetOrder = ["Greeting", "Calendar", "To Do", "Weather", "Prayer", "System"]; _settings.HiddenWidgets.Clear(); _settings.WidgetOpacity = 1; _settingsService.Save(_settings); ApplyWidgetPreferences(); ShowToast("Widgets reset"); }

    private void ConfigureGroups()
    {
        _groupsOverlay = new Canvas { Margin = new Thickness(290, 82, 28, 105), IsHitTestVisible = true }; Panel.SetZIndex(_groupsOverlay, 25); RootGrid.Children.Add(_groupsOverlay);
        foreach (var group in new[] { WorkGroup, AppsGroup, FoldersGroup, GamesGroup }) { if (group.Parent is Panel parent) parent.Children.Remove(group); _groupsOverlay.Children.Add(group); }
        var definitions = new[] { (GamesGroup, "Games", "Games", Color.FromRgb(43, 145, 255)), (AppsGroup, "Apps", "Apps", Color.FromRgb(36, 204, 92)), (FoldersGroup, "Projects", "Folders", Color.FromRgb(151, 79, 236)), (WorkGroup, "Recent Files", "Work", Color.FromRgb(255, 188, 22)) };
        foreach (var definition in definitions) BuildDesktopGroup(definition.Item1, definition.Item2, definition.Item3, definition.Item4);
        LayoutDesktopGroups(); LoadDesktopItems();
        foreach (var pair in definitions.Select(d => (d.Item1, d.Item2, d.Item3)))
        {
            pair.Item1.AllowDrop = true;
            pair.Item1.Drop += (_, e) => Group_Drop(pair.Item3, e);
            pair.Item1.MouseLeftButtonDown += (_, _) => { foreach (var group in _groups.Keys) group.BorderBrush = (Brush)FindResource("Line"); pair.Item1.BorderBrush = new SolidColorBrush(Color.FromRgb(122, 94, 230)); };
            var menu = new ContextMenu(); AddMenu(menu, "Refresh items", LoadDesktopItems); AddMenu(menu, "Hide group", () => { pair.Item1.Visibility = Visibility.Collapsed; ShowToast($"{pair.Item2} hidden"); }); menu.Items.Add(new Separator()); AddMenu(menu, "Reset group layout", () => { pair.Item1.Visibility = Visibility.Visible; LayoutDesktopGroups(); }); pair.Item1.ContextMenu = menu;
        }
    }

    private void BuildDesktopGroup(Border group, string title, string category, Color accent)
    {
        group.Tag = title; group.Width = _settings.DesktopGroupWidths.GetValueOrDefault(title, 285); group.Height = _settings.DesktopGroupHeights.GetValueOrDefault(title, 165); group.Padding = new Thickness(13, 9, 13, 7); group.Background = new SolidColorBrush(Color.FromArgb(225, 8, 28, 49)); group.BorderBrush = new SolidColorBrush(Color.FromArgb(190, 91, 168, 213)); group.BorderThickness = new Thickness(1); group.CornerRadius = new CornerRadius(13);
        var root = new Grid(); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(39) }); root.RowDefinitions.Add(new RowDefinition()); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
        var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new Ellipse { Width = 11, Height = 11, Fill = new SolidColorBrush(accent), Margin = new Thickness(0, 0, 10, 0) }); left.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center }); var count = new TextBlock { Tag = "Count", Foreground = new SolidColorBrush(Color.FromRgb(151, 169, 193)), FontSize = 10, Margin = new Thickness(12, 3, 0, 0) }; left.Children.Add(count); header.Children.Add(left);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; var hidden = new ToggleButton { Style = (Style)FindResource("Toggle"), IsChecked = _settings.ShowHiddenDesktopItems, Margin = new Thickness(8, 0, 10, 0), ToolTip = "Show hidden and system items" }; hidden.Click += (_, _) => { _settings.ShowHiddenDesktopItems = hidden.IsChecked == true; _settingsService.Save(_settings); LoadDesktopItems(); }; actions.Children.Add(new TextBlock { Text = "◉  Hidden", VerticalAlignment = VerticalAlignment.Center, FontSize = 11 }); actions.Children.Add(hidden); var add = HeaderAction("＋", "Add item"); add.Click += (_, _) => ShowGroupAddMenu(add, category); actions.Children.Add(add); var collapse = HeaderAction("⌃", "Collapse group"); collapse.Tag = "Collapse"; actions.Children.Add(collapse); Grid.SetColumn(actions, 1); header.Children.Add(actions); root.Children.Add(header);
        Panel content = title == "Recent Files" ? new StackPanel() : new WrapPanel(); content.Tag = "GroupContent"; var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 3), PanningMode = PanningMode.VerticalOnly }; scroll.Resources[typeof(ScrollBar)] = (Style)FindResource("GroupScrollBarStyle"); Grid.SetRow(scroll, 1); root.Children.Add(scroll); _groupContentHosts[group] = content;
        var hints = new TextBlock { Text = "Enter  Open   •   F2  Rename   •   Del  Delete   •   Space  Preview", Foreground = new SolidColorBrush(Color.FromRgb(103, 143, 191)), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Opacity = .4 }; Grid.SetRow(hints, 2); root.Children.Add(hints); group.MouseEnter += (_, _) => hints.Opacity = 1; group.MouseLeave += (_, _) => hints.Opacity = .4;
        var resizeGrip = new Border { Width = 22, Height = 22, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Background = Brushes.Transparent, Cursor = Cursors.SizeNWSE, ToolTip = "Drag to resize group", Child = new TextBlock { Text = "◢", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(83, 142, 184)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } }; Grid.SetRow(resizeGrip, 2); Panel.SetZIndex(resizeGrip, 20); resizeGrip.MouseLeftButtonDown += ResizeGrip_MouseDown; resizeGrip.MouseMove += ResizeGrip_MouseMove; resizeGrip.MouseLeftButtonUp += ResizeGrip_MouseUp; root.Children.Add(resizeGrip);
        _groups[group] = (group.Height, false); collapse.Click += (_, _) => ToggleGroup(group, collapse); group.Child = root;
    }

    private Button HeaderAction(string text, string tooltip) => new() { Content = text, Width = 34, Height = 31, Margin = new Thickness(4, 0, 0, 0), Style = (Style)FindResource("IconButton"), FontSize = 19, ToolTip = tooltip, Cursor = Cursors.Hand };

    private void LayoutDesktopGroups()
    {
        if (_groupsOverlay == null) return; var width = Math.Max(720, ActualWidth - 318); var availableHeight = Math.Max(520, ActualHeight - 187); var gap = 8d; var defaultWidth = Math.Min(300, width * .28); var defaultHeight = Math.Clamp((availableHeight - gap * 3) / 4, 135, 210);
        foreach (var group in new[] { WorkGroup, AppsGroup, FoldersGroup, GamesGroup }) { var title = group.Tag?.ToString() ?? "Group"; group.Width = Math.Clamp(_settings.DesktopGroupWidths.GetValueOrDefault(title, defaultWidth), 240, Math.Min(620, width)); if (!_groups.GetValueOrDefault(group).Collapsed) group.Height = Math.Clamp(_settings.DesktopGroupHeights.GetValueOrDefault(title, defaultHeight), 125, Math.Min(500, availableHeight)); }
        PositionDesktopGroups();
    }

    private void PositionDesktopGroups()
    {
        if (_groupsOverlay == null) return; var width = Math.Max(720, ActualWidth - 318); var gap = 8d; var availableHeight = _groupsOverlay.ActualHeight > 0 ? _groupsOverlay.ActualHeight : Math.Max(520, ActualHeight - 187); var visible = new[] { WorkGroup, AppsGroup, FoldersGroup, GamesGroup }.Where(g => g.Visibility == Visibility.Visible).ToList(); var total = visible.Sum(g => g.Height) + Math.Max(0, visible.Count - 1) * gap;
        if (total > availableHeight) { var usable = Math.Max(visible.Count * 105, availableHeight - Math.Max(0, visible.Count - 1) * gap); var currentTotal = visible.Sum(g => g.Height); foreach (var group in visible) group.Height = Math.Max(105, group.Height * usable / currentTotal); }
        var y = 0d;
        foreach (var group in new[] { WorkGroup, AppsGroup, FoldersGroup, GamesGroup }) { Canvas.SetLeft(group, Math.Max(0, width - group.Width)); Canvas.SetTop(group, y); y += group.Height + gap; }
    }

    private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement grip || FindAncestor<Border>(grip) is not { } group || !_groups.ContainsKey(group)) return; _resizingGroup = group; _resizeStart = e.GetPosition(_groupsOverlay); _resizeStartWidth = group.Width; _resizeStartHeight = group.Height; grip.CaptureMouse(); e.Handled = true;
    }
    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (_resizingGroup == null || e.LeftButton != MouseButtonState.Pressed || _groupsOverlay == null) return; var point = e.GetPosition(_groupsOverlay); var maxWidth = Math.Max(300, ActualWidth - 350); var maxHeight = Math.Max(180, ActualHeight - 210); _resizingGroup.Width = Math.Clamp(_resizeStartWidth + point.X - _resizeStart.X, 240, maxWidth); _resizingGroup.Height = Math.Clamp(_resizeStartHeight + point.Y - _resizeStart.Y, 125, maxHeight); var current = _groups[_resizingGroup]; _groups[_resizingGroup] = (_resizingGroup.Height, current.Collapsed); PositionDesktopGroups(); e.Handled = true;
    }
    private void ResizeGrip_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_resizingGroup == null) return; var title = _resizingGroup.Tag?.ToString() ?? "Group"; _settings.DesktopGroupWidths[title] = _resizingGroup.Width; _settings.DesktopGroupHeights[title] = _resizingGroup.Height; _settingsService.Save(_settings); (sender as UIElement)?.ReleaseMouseCapture(); _resizingGroup = null; SaveLayout(); e.Handled = true;
    }

    private void ShowGroupAddMenu(Button owner, string category)
    {
        var menu = new ContextMenu(); AddMenu(menu, "Add file or shortcut…", () => { var dialog = new Microsoft.Win32.OpenFileDialog { CheckFileExists = true, Filter = "All files|*.*" }; if (dialog.ShowDialog(this) == true && _desktopItemsService != null) { _desktopItemsService.Assign(dialog.FileName, category); ShowToast("Item assigned to group"); LoadDesktopItems(); } }); AddMenu(menu, "Add folder…", () => { var dialog = new Microsoft.Win32.OpenFolderDialog(); if (dialog.ShowDialog(this) == true && _desktopItemsService != null) { _desktopItemsService.Assign(dialog.FolderName, category); ShowToast("Folder assigned to group"); LoadDesktopItems(); } }); owner.ContextMenu = menu; menu.PlacementTarget = owner; menu.IsOpen = true;
    }

    private void LoadDesktopItems()
    {
        _desktopItems = _desktopItemsService?.GetDesktopItems().ToList() ?? [];
        PopulateGroup(WorkGroup, "Work"); PopulateGroup(AppsGroup, "Apps"); PopulateGroup(FoldersGroup, "Folders"); PopulateGroup(GamesGroup, "Games");
    }

    private void PopulateGroup(Border group, string category)
    {
        if (!_groupContentHosts.TryGetValue(group, out var host)) return; host.Children.Clear(); var items = _desktopItems.Where(i => i.Category == category && (_settings.ShowHiddenDesktopItems || !i.IsHidden)).OrderByDescending(i => category == "Work" ? SafeModified(i.Path) : DateTime.MinValue).ToList();
        var count = Descendants<TextBlock>(group).FirstOrDefault(t => Equals(t.Tag, "Count")); if (count != null) count.Text = $"{items.Count} items";
        foreach (var item in items) host.Children.Add(category == "Work" ? CreateRecentDesktopItem(item) : CreateDesktopItem(item)); if (items.Count == 0) host.Children.Add(new TextBlock { Text = "No desktop items", Foreground = (Brush)FindResource("TextMuted"), Margin = new Thickness(8, 20, 0, 0) });
    }

    private FrameworkElement CreateDesktopItem(DesktopItem item)
    {
        var image = new Image { Source = item.Icon, Width = 46, Height = 46, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center };
        var label = new TextBlock { Text = item.Name, FontSize = 10, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 94, MaxHeight = 30, Margin = new Thickness(0, 5, 0, 0) };
        var body = new StackPanel(); body.Children.Add(image); body.Children.Add(label); if (item.IsHidden) body.Children.Add(new TextBlock { Text = "Hidden", FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(125, 153, 190)), HorizontalAlignment = HorizontalAlignment.Center });
        var card = new Border { Width = 104, Height = 88, Padding = new Thickness(5), Margin = new Thickness(3, 3, 3, 5), CornerRadius = new CornerRadius(9), Cursor = Cursors.Hand, ToolTip = CreateDesktopInspector(item), Tag = item, Background = Brushes.Transparent, Child = body, Focusable = true, Opacity = item.IsHidden ? .62 : 1 };
        ToolTipService.SetInitialShowDelay(card, 500);
        card.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true; card.Focus(); _selectedDesktopItem = item;
            if (e.ClickCount == 2) OpenDesktopItem(item);
            else { ClearDesktopSelection(); card.Background = new SolidColorBrush(Color.FromArgb(95, 62, 105, 162)); }
        };
        card.MouseMove += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) { var data = new DataObject(); data.SetData("BetterHome.DesktopItem", item.Path); data.SetData(DataFormats.FileDrop, new[] { item.Path }); DragDrop.DoDragDrop(card, data, DragDropEffects.Copy | DragDropEffects.Move); } };
        card.ContextMenu = CreateDesktopContextMenu(item);
        card.KeyDown += (_, e) => HandleDesktopItemKey(item, e);
        return card;
    }

    private FrameworkElement CreateRecentDesktopItem(DesktopItem item)
    {
        var row = new Grid { Height = 40, Margin = new Thickness(2, 0, 2, 0), Cursor = Cursors.Hand, Tag = item, ToolTip = CreateDesktopInspector(item), Focusable = true }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        row.Children.Add(new Image { Source = item.Icon, Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center }); var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; texts.Children.Add(new TextBlock { Text = item.Name, FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis }); texts.Children.Add(new TextBlock { Text = Path.GetDirectoryName(item.Path), FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(130, 151, 179)), TextTrimming = TextTrimming.CharacterEllipsis }); Grid.SetColumn(texts, 1); row.Children.Add(texts); var date = new TextBlock { Text = SafeModified(item.Path).ToString("g"), FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(142, 160, 185)), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(date, 2); row.Children.Add(date); row.MouseEnter += (_, _) => row.Background = new SolidColorBrush(Color.FromArgb(70, 43, 83, 125)); row.MouseLeave += (_, _) => row.Background = Brushes.Transparent; row.MouseLeftButtonDown += (_, e) => { e.Handled = true; row.Focus(); _selectedDesktopItem = item; if (e.ClickCount == 2) OpenDesktopItem(item); }; row.ContextMenu = CreateDesktopContextMenu(item); row.KeyDown += (_, e) => HandleDesktopItemKey(item, e); ToolTipService.SetInitialShowDelay(row, 500); return row;
    }

    private ToolTip CreateDesktopInspector(DesktopItem item)
    {
        var panel = new StackPanel { Width = 245 }; var top = new StackPanel { Orientation = Orientation.Horizontal }; top.Children.Add(new Image { Source = item.Icon, Width = 38, Height = 38, Margin = new Thickness(0, 0, 11, 0) }); var names = new StackPanel(); names.Children.Add(new TextBlock { Text = item.Name, FontWeight = FontWeights.SemiBold, FontSize = 12 }); names.Children.Add(new TextBlock { Text = item.IsDirectory ? "File folder" : $"{Path.GetExtension(item.Path).TrimStart('.').ToUpperInvariant()} file", Foreground = new SolidColorBrush(Color.FromRgb(147, 170, 201)), FontSize = 9 }); top.Children.Add(names); panel.Children.Add(top);
        var info = GetDesktopInfo(item); panel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(49, 80, 111)), Margin = new Thickness(0, 10, 0, 8) }); foreach (var line in info) panel.Children.Add(new TextBlock { Text = line, FontSize = 9, Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap }); return new ToolTip { Content = panel, Background = new SolidColorBrush(Color.FromRgb(10, 27, 46)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(75, 137, 181)), BorderThickness = new Thickness(1), Padding = new Thickness(12) };
    }
    private IEnumerable<string> GetDesktopInfo(DesktopItem item) { FileSystemInfo info = item.IsDirectory ? new DirectoryInfo(item.Path) : new FileInfo(item.Path); yield return $"Size: {(item.IsDirectory ? "Calculating size…" : FormatFileSize(((FileInfo)info).Length))}"; yield return $"Date modified: {info.LastWriteTime:g}"; yield return $"Location: {Path.GetDirectoryName(item.Path)}"; if (!string.IsNullOrWhiteSpace(item.TargetPath)) yield return $"Target: {item.TargetPath}"; }
    private static string FormatFileSize(long size) => size < 1024 ? $"{size} B" : size < 1024 * 1024 ? $"{size / 1024d:0.#} KB" : $"{size / 1024d / 1024d:0.#} MB";
    private static DateTime SafeModified(string path) { try { return File.GetLastWriteTime(path); } catch { return DateTime.MinValue; } }
    private void HandleDesktopItemKey(DesktopItem item, KeyEventArgs e) { if (e.Key == Key.Enter) OpenDesktopItem(item); else if (e.Key == Key.F2) ShowInput("Rename", item.Name, value => RenameDesktopItem(item, value)); else if (e.Key == Key.Delete) DeleteDesktopItem(item); else if (e.Key == Key.Space) ShowToast($"Preview: {item.Name}"); else return; e.Handled = true; }

    private ContextMenu CreateDesktopContextMenu(DesktopItem item)
    {
        var menu = new ContextMenu();
        void Add(string title, Action action) { var entry = new MenuItem { Header = title }; entry.Click += (_, _) => action(); menu.Items.Add(entry); }
        Add("Open", () => OpenDesktopItem(item)); Add("Open file location", () => SafeAction(() => _fileOperations.OpenLocation(item.Path)));
        menu.Items.Add(new Separator()); Add("Rename", () => ShowInput("Rename", item.Name, value => RenameDesktopItem(item, value))); Add("Delete", () => DeleteDesktopItem(item));
        menu.Items.Add(new Separator()); Add("Properties", () => SafeAction(() => _fileOperations.Properties(item.Path))); return menu;
    }

    private void OpenDesktopItem(DesktopItem item) => SafeAction(() => _fileOperations.Open(item.Path), $"Opening {item.Name}");
    private void RenameDesktopItem(DesktopItem item, string name) { SafeAction(() => _fileOperations.Rename(item.Path, item.IsDirectory ? name : name + Path.GetExtension(item.Path)), "Item renamed"); LoadDesktopItems(); }
    private void DeleteDesktopItem(DesktopItem item) { if (MessageBox.Show($"Move '{item.Name}' to the Recycle Bin?", "BetterHome", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; SafeAction(() => _fileOperations.DeleteToRecycleBin(item.Path), "Moved to Recycle Bin"); LoadDesktopItems(); }
    private void ClearDesktopSelection() { foreach (var border in _groups.Keys.SelectMany(Descendants<Border>).Where(x => x.Tag is DesktopItem)) border.Background = Brushes.Transparent; }
    private void Group_Drop(string category, DragEventArgs e)
    {
        string? path = e.Data.GetData("BetterHome.DesktopItem") as string;
        if (path == null && e.Data.GetDataPresent(DataFormats.FileDrop)) path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        if (path == null || _desktopItemsService == null) return; _desktopItemsService.Assign(path, category); ShowToast($"Assigned {Path.GetFileName(path)} to {category}"); LoadDesktopItems(); e.Handled = true;
    }

    private void InstallRealFileHub()
    {
        if (FileHub.Child is not Grid grid) return;
        foreach (var child in grid.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) > 0).ToList()) grid.Children.Remove(child);
        if (FileHub.Parent is Panel oldParent) oldParent.Children.Remove(FileHub); _fileHubOverlay = new Canvas { Margin = new Thickness(34, 20, 34, 40) }; Panel.SetZIndex(_fileHubOverlay, 250); RootGrid.Children.Add(_fileHubOverlay); _fileHubOverlay.Children.Add(FileHub);
        FileHub.Width = Math.Max(980, RootGrid.ActualWidth - 68); FileHub.Height = Math.Max(620, RootGrid.ActualHeight - 60); Canvas.SetLeft(FileHub, 0); Canvas.SetTop(FileHub, 0); _fileHubWidth = FileHub.Width; _fileHubHeight = FileHub.Height;
        _realFileHub = new Views.FileHubView(_settings.FileHubFontSize); _realFileHub.Toast += ShowToast; Grid.SetRow(_realFileHub, 1); Grid.SetRowSpan(_realFileHub, 2); grid.Children.Add(_realFileHub);
    }

    private void SafeAction(Action action, string? success = null) { try { action(); if (success != null) ShowToast(success); } catch (Exception ex) { ShowToast(ex.Message); } }

    private void EnableGlobalCustomization()
    {
        ApplyWallpaper(); if (Content is not Grid root) return; var menu = new ContextMenu(); AddMenu(menu, "Change static wallpaper", ChangeWallpaper); AddMenu(menu, "Choose animated wallpaper…", ChangeAnimatedWallpaper); AddMenu(menu, _settings.WallpaperAnimationEnabled ? "Pause wallpaper motion" : "Play wallpaper motion", ToggleWallpaperAnimation); AddMenu(menu, "Reset wallpaper", () => { _settings.WallpaperPath = null; _settings.AnimatedWallpaperPath = null; _settings.WallpaperAnimationEnabled = true; _settingsService.Save(_settings); ApplyWallpaper(); }); menu.Items.Add(new Separator()); AddMenu(menu, "Show all widgets", ResetWidgets); AddMenu(menu, "Show all groups", Home); AddMenu(menu, "Reset dock", ResetDock); root.ContextMenu = menu; CreateWallpaperPauseButton(root);
    }
    private void ChangeWallpaper()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.webp", Title = "Choose BetterHome wallpaper" }; if (dialog.ShowDialog(this) != true) return; _settings.WallpaperPath = dialog.FileName; _settings.AnimatedWallpaperPath = null; _settings.WallpaperType = "Static"; _settingsService.Save(_settings); ApplyWallpaper(); ShowToast("Wallpaper changed");
    }
    private void ChangeAnimatedWallpaper()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Video wallpapers|*.mp4;*.wmv;*.avi", Title = "Choose a lightweight video wallpaper" }; if (dialog.ShowDialog(this) != true) return; _settings.AnimatedWallpaperPath = dialog.FileName; _settings.WallpaperType = "Video"; _settings.EnableLiveWallpaper = true; _settings.WallpaperAnimationEnabled = true; _settingsService.Save(_settings); ApplyWallpaper(); ShowToast("Animated wallpaper enabled");
    }
    private void ToggleWallpaperAnimation() { _settings.WallpaperAnimationEnabled = !_settings.WallpaperAnimationEnabled; _settingsService.Save(_settings); ApplyWallpaper(); ShowToast(_settings.WallpaperAnimationEnabled ? "Wallpaper motion enabled" : "Wallpaper motion paused"); }
    private void ApplyWallpaper()
    {
        if (Content is not Grid root) return; _wallpaperImage ??= root.Children.OfType<Image>().FirstOrDefault(); if (_wallpaperImage == null) return;
        if (_wallpaperVideo != null) { _wallpaperVideo.Stop(); root.Children.Remove(_wallpaperVideo); _wallpaperVideo = null; } if (_wallpaperEffect != null) { root.Children.Remove(_wallpaperEffect); _wallpaperEffect = null; }
        var play = _settings.EnableLiveWallpaper && _settings.WallpaperAnimationEnabled && !_wallpaperManuallyPaused && !_wallpaperPolicyPaused; var type = _settings.EnableLiveWallpaper ? _settings.WallpaperType : "Static";
        if (type == "Video" && !string.IsNullOrWhiteSpace(_settings.AnimatedWallpaperPath) && File.Exists(_settings.AnimatedWallpaperPath))
        {
            _wallpaperVideo = new MediaElement { Source = new Uri(_settings.AnimatedWallpaperPath), Stretch = Stretch.UniformToFill, LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Manual, IsMuted = true, ScrubbingEnabled = false, IsHitTestVisible = false }; _wallpaperVideo.MediaOpened += (_, _) => { if (_wallpaperVideo == null) return; _wallpaperVideo.Play(); if (!play) _wallpaperVideo.Pause(); }; _wallpaperVideo.MediaEnded += (_, _) => { if (_wallpaperVideo == null) return; _wallpaperVideo.Position = TimeSpan.Zero; _wallpaperVideo.Play(); }; root.Children.Insert(0, _wallpaperVideo); _wallpaperImage.Visibility = Visibility.Collapsed; _wallpaperVideo.Play(); UpdateWallpaperPauseButton(); return;
        }
        if (type is "Animated gradient" or "WebView animation")
        {
            _wallpaperImage.Visibility = Visibility.Collapsed; var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) }; var first = new GradientStop(Color.FromRgb(5, 20, 55), 0); var middle = new GradientStop(type == "WebView animation" ? Color.FromRgb(28, 29, 91) : Color.FromRgb(17, 54, 112), .48); var last = new GradientStop(Color.FromRgb(18, 8, 58), 1); gradient.GradientStops.Add(first); gradient.GradientStops.Add(middle); gradient.GradientStops.Add(last); _wallpaperEffect = new Rectangle { Fill = gradient, IsHitTestVisible = false }; root.Children.Insert(0, _wallpaperEffect);
            if (play) { var duration = TimeSpan.FromSeconds(_settings.WallpaperQuality == "Low" ? 36 : _settings.WallpaperQuality == "High" ? 16 : 25); var shift = new DoubleAnimation(.25, .72, duration) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() }; middle.BeginAnimation(GradientStop.OffsetProperty, shift); }
            UpdateWallpaperPauseButton(); return;
        }
        _wallpaperImage.Visibility = Visibility.Visible; _wallpaperImage.IsHitTestVisible = false;
        try { _wallpaperImage.Source = string.IsNullOrWhiteSpace(_settings.WallpaperPath) ? new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/wallpaper.png")) : new System.Windows.Media.Imaging.BitmapImage(new Uri(_settings.WallpaperPath)); } catch { _settings.WallpaperPath = null; }
        var scale = new ScaleTransform(1.02, 1.02); _wallpaperImage.RenderTransformOrigin = new Point(.5, .5); _wallpaperImage.RenderTransform = scale; if (play) { var seconds = _settings.WallpaperQuality == "Low" ? 38 : _settings.WallpaperQuality == "High" ? 18 : 26; var animation = new DoubleAnimation(1.02, 1.065, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } }; scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation); scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation.Clone()); } UpdateWallpaperPauseButton();
    }
    private void CreateWallpaperPauseButton(Grid root)
    {
        if (_wallpaperPauseButton != null) return; _wallpaperPauseButton = new Button { Content = "⏸ Wallpaper", Style = (Style)FindResource("IconButton"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 0, 0), Padding = new Thickness(10, 6, 10, 6), Background = new SolidColorBrush(Color.FromArgb(190, 15, 27, 49)), Foreground = Brushes.White, Cursor = Cursors.Hand, ToolTip = "Pause or resume live wallpaper" }; _wallpaperPauseButton.Click += (_, _) => { _wallpaperManuallyPaused = !_wallpaperManuallyPaused; ApplyWallpaper(); }; Panel.SetZIndex(_wallpaperPauseButton, 120); root.Children.Add(_wallpaperPauseButton); UpdateWallpaperPauseButton();
    }
    private void UpdateWallpaperPauseButton() { if (_wallpaperPauseButton != null) _wallpaperPauseButton.Content = _wallpaperManuallyPaused || _wallpaperPolicyPaused || !_settings.WallpaperAnimationEnabled ? "▶ Wallpaper" : "⏸ Wallpaper"; }
    private void EvaluateWallpaperPlayback()
    {
        var paused = (_settings.PauseWallpaperOnBattery && _wallpaperPerformance.IsOnBattery) || (_settings.PauseWallpaperOnFullscreen && _wallpaperPerformance.IsFullscreenForeground(new System.Windows.Interop.WindowInteropHelper(this).Handle)) || (_settings.PauseWallpaperOnHighCpu && _lastCpuUsage >= 75); if (paused == _wallpaperPolicyPaused) return; _wallpaperPolicyPaused = paused; ApplyWallpaper();
    }
    private void EnableFileHubCustomization()
    {
        var menu = new ContextMenu(); AddMenu(menu, "Center panel", () => { Canvas.SetLeft(FileHub, Math.Max(0, (CenterCanvas.ActualWidth - FileHub.ActualWidth) / 2)); Canvas.SetTop(FileHub, Math.Max(0, (CenterCanvas.ActualHeight - FileHub.ActualHeight) / 2)); SaveLayout(); }); AddMenu(menu, "Opacity 75%", () => FileHub.Opacity = .75); AddMenu(menu, "Opacity 90%", () => FileHub.Opacity = .9); AddMenu(menu, "Opacity 100%", () => FileHub.Opacity = 1); AddMenu(menu, "Hide File Hub", CloseFileHub); FileHub.ContextMenu = menu;
    }

    private void AddCollapse(Border group, double normalHeight)
    {
        _groups[group] = (normalHeight, false);
        if (group.Child is not StackPanel panel) return;
        var button = new Button { Content = "⌃", Width = 24, Height = 22, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, -22, 0, 0), Style = (Style)FindResource("IconButton"), ToolTip = "Collapse group" };
        panel.Children.Insert(1, button);
        button.Click += (_, _) => ToggleGroup(group, button);
    }

    private void ToggleGroup(Border group, Button button, bool? force = null)
    {
        var current = _groups[group];
        var collapsed = force ?? !current.Collapsed;
        _groups[group] = (current.Height, collapsed);
        if (group.Child is Grid grid) { foreach (UIElement child in grid.Children) if (Grid.GetRow(child) > 0) child.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible; }
        else if (group.Child is StackPanel panel && panel.Children.Count > 2) panel.Children[2].Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        group.Height = collapsed ? 64 : current.Height;
        button.Content = collapsed ? "⌄" : "⌃";
        button.ToolTip = collapsed ? "Expand group" : "Collapse group";
        if (_groupsOverlay != null) PositionDesktopGroups();
        if (!_loading) SaveLayout();
    }

    private void WireControls()
    {
        var toggles = Descendants<ToggleButton>(this).ToList();
        _hideGroupsToggle = toggles.FirstOrDefault(t => t != AutoToggle);
        if (_hideGroupsToggle != null) _hideGroupsToggle.Click += (_, _) =>
        {
            var hide = _hideGroupsToggle.IsChecked == true;
            if (_desktopVisibility.TrySetVisible(!hide, out var error)) ShowToast(hide ? "Windows desktop icons hidden" : "Windows desktop icons shown");
            else { _hideGroupsToggle.IsChecked = !hide; ShowToast(error ?? "Desktop icon visibility could not be changed"); }
        };

        foreach (var button in Descendants<Button>(this).ToList())
        {
            var content = button.Content?.ToString() ?? "";
            if (content.Contains("Desktop Mode")) button.Click += DesktopMode_Click;
            else if (IsInside(button, FileHub) && content is "—" or "□" or "×") WireFileHubButton(button, content);
        }

        WireWidgets();
        ApplyTooltips();
    }

    private void BuildRealDock()
    {
        if (_dock == null) return;
        _dock.IsHitTestVisible = true; Panel.SetZIndex(_dock, 80);
        var host = DockItemsHost; host.Children.Clear(); host.Margin = new Thickness(70, 0, 70, 0); host.Columns = 8;
        _dockItems.Clear();
        var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        var edge = FindExisting(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"));
        var code = FindExisting(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe"));
        var spotify = FindExisting(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "Spotify.exe"));
        Add("home", "Windows Start", WindowsLogoIcon(), OpenWindowsStartMenu);
        Add("search", "Windows Search", DockGlyph("&#xE721;", Color.FromRgb(221, 230, 246)), OpenWindowsSearch);
        Add("filehub", "File Hub", (object?)_icons.GetIcon(explorer) ?? DockGlyph("&#xE8B7;", Color.FromRgb(190, 205, 229)), ToggleFileHubFromDock);
        Add("browser", "Microsoft Edge", edge != null ? (object?)_icons.GetIcon(edge) ?? DockGlyph("E", Color.FromRgb(33, 179, 218)) : DockGlyph("E", Color.FromRgb(33, 179, 218)), () => SafeAction(() => OpenInEdge("https://www.google.com"), "Microsoft Edge opened"));
        Add("vscode", "Visual Studio Code", code != null ? (object?)_icons.GetIcon(code) ?? DockGlyph("⌁", Color.FromRgb(38, 159, 220)) : DockGlyph("⌁", Color.FromRgb(38, 159, 220)), () => OpenDockApp(code, "Visual Studio Code"));
        Add("spotify", "Spotify", spotify != null ? (object?)_icons.GetIcon(spotify) ?? DockGlyph("●", Color.FromRgb(29, 215, 96)) : DockGlyph("●", Color.FromRgb(29, 215, 96)), () => OpenDockApp(spotify, "Spotify"));
        Add("settings", "Settings", DockGlyph("&#xE713;", Color.FromRgb(180, 201, 225)), OpenSettingsSingleton);
        Add("recycle", "Recycle Bin", DockGlyph("&#xE74D;", Color.FromRgb(151, 180, 200)), () => SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true }), "Recycle Bin opened"));
        var pinnedProcesses = new HashSet<string>(["explorer", "msedge", "code", "spotify", "systemsettings"], StringComparer.OrdinalIgnoreCase);
        foreach (var running in _runningWindows.GetOpenWindows().GroupBy(window => window.ProcessName, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).Where(window => !pinnedProcesses.Contains(window.ProcessName)))
        {
            var icon = !string.IsNullOrWhiteSpace(running.ProcessPath) ? _icons.GetIcon(running.ProcessPath) : null; Add($"running:{running.ProcessName}", running.ProcessName, (object?)icon ?? DockGlyph(running.ProcessName[..1].ToUpperInvariant(), Color.FromRgb(133, 185, 232)), () => _runningWindows.Focus(running));
        }
        foreach (var shortcut in _settings.CustomDockItems.Where(s => File.Exists(s.Path) || Directory.Exists(s.Path))) Add(shortcut.Id, shortcut.Name, (object?)_icons.GetIcon(shortcut.Path, Directory.Exists(shortcut.Path)) ?? "◇", () => SafeAction(() => _fileOperations.Open(shortcut.Path), $"Opening {shortcut.Name}"), true);
        ApplyDockPreferences(host);
        if (_dock != null) _dock.Width = Math.Min(Math.Max(600, host.Children.Count * 61 + 130), Math.Max(600, ActualWidth - 80));

        void Add(string id, string name, object icon, Action action, bool custom = false)
        {
            DockItemViewModel? item = null;
            item = new DockItemViewModel { Id = id, Name = name, Icon = icon, Command = new RelayCommand(_ => { SetActiveDockItem(item!); action(); }) }; _dockItems.Add(item);
            var indicator = new Border { Height = 3, Width = 18, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.FromRgb(56, 166, 255)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 2), Visibility = Visibility.Collapsed };
            var iconContent = icon is ImageSource source ? new Image { Source = source, Width = 27, Height = 27, Stretch = Stretch.Uniform } : icon;
            var button = new Button { Content = iconContent, Style = (Style)FindResource("IconButton"), FontSize = 26, Cursor = Cursors.Hand, ToolTip = new ToolTip { Content = name, Placement = PlacementMode.Top, VerticalOffset = -5 }, Tag = id, Command = item.Command, RenderTransformOrigin = new Point(.5, .5), RenderTransform = new ScaleTransform(1, 1), IsHitTestVisible = true };
            button.MouseEnter += (_, _) => { AnimateScale(button, 1.12); button.Background = new SolidColorBrush(Color.FromArgb(75, 49, 116, 202)); };
            button.MouseLeave += (_, _) => { AnimateScale(button, 1); if (!item.IsActive) button.Background = Brushes.Transparent; };
            button.PreviewMouseLeftButtonDown += (_, _) => AnimateScale(button, .88); button.PreviewMouseLeftButtonUp += (_, _) => AnimateScale(button, 1.08);
            var menu = new ContextMenu();
            AddMenu(menu, "Open", action); AddMenu(menu, "Add application or file…", AddCustomDockItem); AddMenu(menu, "Move left", () => MoveDockItem(id, -1)); AddMenu(menu, "Move right", () => MoveDockItem(id, 1)); AddMenu(menu, custom ? "Remove from dock" : "Hide from dock", () => { if (custom) RemoveCustomDockItem(id); else HideDockItem(id); }); menu.Items.Add(new Separator());
            AddMenu(menu, "Small dock", () => SetDockScale(.85)); AddMenu(menu, "Normal dock", () => SetDockScale(1)); AddMenu(menu, "Large dock", () => SetDockScale(1.15)); AddMenu(menu, "Reset dock", ResetDock); button.ContextMenu = menu;
            item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(DockItemViewModel.IsActive)) { indicator.Visibility = item.IsActive ? Visibility.Visible : Visibility.Collapsed; button.Background = item.IsActive ? new SolidColorBrush(Color.FromArgb(85, 56, 108, 180)) : Brushes.Transparent; } };
            var cell = new Grid { IsHitTestVisible = true }; cell.Children.Add(button); cell.Children.Add(indicator); host.Children.Add(cell);
        }
        var dockMenu = new ContextMenu(); AddMenu(dockMenu, "Add application or file…", AddCustomDockItem); AddMenu(dockMenu, "Small dock", () => SetDockScale(.85)); AddMenu(dockMenu, "Normal dock", () => SetDockScale(1)); AddMenu(dockMenu, "Large dock", () => SetDockScale(1.15)); dockMenu.Items.Add(new Separator()); AddMenu(dockMenu, "Reset dock", ResetDock); if (_dock != null) _dock.ContextMenu = dockMenu;
    }

    private static string? FindExisting(params string[] paths) => paths.FirstOrDefault(File.Exists);
    private static Grid WindowsLogoIcon()
    {
        var grid = new Grid { Width = 27, Height = 27, Margin = new Thickness(2) }; grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var row = 0; row < 2; row++) for (var column = 0; column < 2; column++) { var tile = new Border { Background = new SolidColorBrush(Color.FromRgb(24, 169, 244)), Margin = new Thickness(1.4), CornerRadius = new CornerRadius(1) }; Grid.SetRow(tile, row); Grid.SetColumn(tile, column); grid.Children.Add(tile); } return grid;
    }
    private static void OpenWindowsStartMenu() { const byte windowsKey = 0x5B; keybd_event(windowsKey, 0, 0, 0); keybd_event(windowsKey, 0, 2, 0); }
    private static void OpenWindowsSearch() { const byte windowsKey = 0x5B, sKey = 0x53; keybd_event(windowsKey, 0, 0, 0); keybd_event(sKey, 0, 0, 0); keybd_event(sKey, 0, 2, 0); keybd_event(windowsKey, 0, 2, 0); }
    private static TextBlock DockGlyph(string glyph, Color color) => new() { Text = glyph.StartsWith("&#x") ? char.ConvertFromUtf32(Convert.ToInt32(glyph[3..^1], 16)) : glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), Foreground = new SolidColorBrush(color), FontSize = 27, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private void OpenDockApp(string? path, string name) { if (path == null) { ShowToast($"{name} is not installed"); return; } SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }), $"{name} opened"); }

    private void ApplyDockPreferences(UniformGrid host)
    {
        var cells = host.Children.Cast<Grid>().ToDictionary(c => Descendants<Button>(c).First().Tag!.ToString()!, StringComparer.OrdinalIgnoreCase); host.Children.Clear();
        foreach (var id in _settings.DockOrder.Concat(cells.Keys).Distinct(StringComparer.OrdinalIgnoreCase)) if (cells.TryGetValue(id, out var cell) && !_settings.HiddenDockItems.Contains(id, StringComparer.OrdinalIgnoreCase)) host.Children.Add(cell);
        host.Columns = Math.Max(1, host.Children.Count); if (_dock != null) { _dock.Opacity = _settings.DockOpacity; _dock.LayoutTransform = new ScaleTransform(_settings.DockScale, _settings.DockScale); }
    }
    private void MoveDockItem(string id, int delta) { var order = _settings.DockOrder; var index = order.IndexOf(id); if (index < 0) { order.Add(id); index = order.Count - 1; } var target = Math.Clamp(index + delta, 0, order.Count - 1); order.RemoveAt(index); order.Insert(target, id); SaveSettingsAndRefreshDock(); }
    private void HideDockItem(string id) { if (!_settings.HiddenDockItems.Contains(id)) _settings.HiddenDockItems.Add(id); SaveSettingsAndRefreshDock(); ShowToast($"{id} hidden from dock"); }
    private void SetDockScale(double scale) { _settings.DockScale = scale; SaveSettingsAndRefreshDock(); }
    private void ResetDock() { _settings.DockOrder = ["home", "search", "filehub", "browser", "vscode", "spotify", "settings", "recycle"]; _settings.HiddenDockItems.Clear(); _settings.CustomDockItems.Clear(); _settings.DockScale = 1; _settings.DockOpacity = 1; SaveSettingsAndRefreshDock(); ShowToast("Dock reset"); }
    private void AddCustomDockItem()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Add to BetterHome dock", Filter = "Applications and shortcuts|*.exe;*.lnk;*.url|All files|*.*", CheckFileExists = true }; if (dialog.ShowDialog(this) != true) return;
        var shortcut = new DockShortcut { Name = Path.GetFileNameWithoutExtension(dialog.FileName), Path = dialog.FileName }; _settings.CustomDockItems.Add(shortcut); _settings.DockOrder.Add(shortcut.Id); SaveSettingsAndRefreshDock(); ShowToast($"{shortcut.Name} added to dock");
    }
    private void RemoveCustomDockItem(string id) { _settings.CustomDockItems.RemoveAll(x => x.Id == id); _settings.DockOrder.Remove(id); _settings.HiddenDockItems.Remove(id); SaveSettingsAndRefreshDock(); ShowToast("Dock item removed"); }
    private void SaveSettingsAndRefreshDock()
    {
        _settingsService.Save(_settings); if (_dockRefreshQueued) return; _dockRefreshQueued = true;
        Dispatcher.BeginInvoke(async () => { await Task.Delay(180); _dockRefreshQueued = false; BuildRealDock(); }, DispatcherPriority.ContextIdle);
    }
    private static void AddMenu(ContextMenu menu, string title, Action action) { var item = new MenuItem { Header = title }; item.Click += (_, _) => action(); menu.Items.Add(item); }

    private Button? FindDockButton(string id) => _dock == null ? null : Descendants<Button>(_dock).FirstOrDefault(b => b.Tag?.ToString() == id);
    private void SetActiveDockItem(DockItemViewModel selected) { foreach (var item in _dockItems) item.IsActive = item == selected; }
    private void ToggleFileHubFromDock() { if (FileHub.Visibility == Visibility.Visible) CloseFileHub(); else { ShowFileHub(); Panel.SetZIndex(FileHub, 60); ShowToast("File Hub opened"); } }
    private void OpenBetterHomeFiles() { if (_realFileHub == null) return; _realFileHub.ViewModel.Navigate(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); FileHub.Visibility = Visibility.Visible; Panel.SetZIndex(FileHub, 60); ShowToast("File Hub opened"); }

    private static void OpenInEdge(string address)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };
        var edge = candidates.FirstOrDefault(File.Exists) ?? "msedge.exe";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = edge,
            Arguments = address,
            UseShellExecute = true
        });
    }

    private void InitializeDynamicIsland()
    {
        if (!_settings.EnableDynamicIsland || _dynamicIsland != null) return;
        _dynamicIslandText = new TextBlock { Text = "BetterHome ready", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
        _dynamicIsland = new Border { Width = 235, Height = 42, CornerRadius = new CornerRadius(22), Background = new SolidColorBrush(Color.FromArgb(245, 8, 17, 33)), BorderBrush = new SolidColorBrush(Color.FromRgb(37, 115, 172)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(18, 0, 18, 0), Child = _dynamicIslandText, Cursor = Cursors.Hand, ToolTip = "Click to view running apps" };
        _dynamicIsland.MouseLeftButtonDown += (_, e) => { e.Handled = true; ShowOpenAppsOverview(); };
        Panel.SetZIndex(_dynamicIsland, 450); RootGrid.Children.Add(_dynamicIsland);
        _dynamicIslandService.StatusChanged += status => Dispatcher.Invoke(() => { if (_dynamicIslandText != null) _dynamicIslandText.Text = status; });
        var count = _runningWindows.GetOpenWindows().Count; _dynamicIslandService.Publish($"{count} apps running");
    }

    private void InitializePremiumFeatures()
    {
        ApplySelectedTheme(false);
        if (_settings.ShowMiniPlayer) CreateMiniPlayer();
        if (_settings.EnableEdgeDock) CreateEdgeDock();
        if (_settings.ShowAssistantBubble) CreateAssistantBubble();
        if (!_runningAppsTimerHooked) { _runningAppsTimer.Tick += (_, _) => { RefreshRunningDock(); if (_settings.ShowRunningAppsStrip) RefreshRunningStrip(); }; _runningAppsTimerHooked = true; }
        _runningAppsTimer.Start(); RefreshRunningDock(); if (_settings.ShowRunningAppsStrip) RefreshRunningStrip();
    }

    private void RefreshRunningDock()
    {
        var signature = string.Join("|", _runningWindows.GetOpenWindows().Select(window => window.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name)); if (signature == _runningDockSignature) return; _runningDockSignature = signature; BuildRealDock();
    }

    private void CreateMiniPlayer()
    {
        var title = new TextBlock { Text = _mediaSession.CurrentTitle(_runningWindows), FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Width = 210, ToolTip = "Detected media window" };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        controls.Children.Add(MediaButton("⏮", "Previous", _mediaSession.Previous)); controls.Children.Add(MediaButton("▶", "Play / Pause", _mediaSession.PlayPause)); controls.Children.Add(MediaButton("⏭", "Next", _mediaSession.Next)); controls.Children.Add(MediaButton("−", "Volume down", _mediaSession.VolumeDown)); controls.Children.Add(MediaButton("+", "Volume up", _mediaSession.VolumeUp));
        var body = new StackPanel(); body.Children.Add(new TextBlock { Text = "Mini Player", FontWeight = FontWeights.SemiBold }); body.Children.Add(title); body.Children.Add(controls);
        _miniPlayer = new Border { Width = 250, Padding = new Thickness(12, 9, 12, 8), CornerRadius = new CornerRadius(14), Background = new SolidColorBrush(Color.FromArgb(235, 13, 27, 49)), BorderBrush = new SolidColorBrush(Color.FromRgb(51, 93, 139)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(285, 0, 0, 72), Child = body, ToolTip = "Right-click to hide" };
        var menu = new ContextMenu(); AddMenu(menu, "Refresh media", () => title.Text = _mediaSession.CurrentTitle(_runningWindows)); AddMenu(menu, "Hide Mini Player", () => { RootGrid.Children.Remove(_miniPlayer); _settings.ShowMiniPlayer = false; _settingsService.Save(_settings); }); _miniPlayer.ContextMenu = menu;
        Panel.SetZIndex(_miniPlayer, 300); RootGrid.Children.Add(_miniPlayer);
    }

    private Button MediaButton(string text, string tip, Action action)
    {
        var button = new Button { Content = text, Width = 35, Height = 30, Margin = new Thickness(2, 7, 2, 0), Style = (Style)FindResource("IconButton"), ToolTip = tip };
        button.Click += (_, _) => { action(); ShowToast(tip); }; return button;
    }

    private void CreateEdgeDock()
    {
        var items = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        items.Children.Add(EdgeButton("Files", ToggleFileHubFromDock)); items.Children.Add(EdgeButton("Apps", ShowAppDrawer)); items.Children.Add(EdgeButton("Open", ShowOpenAppsOverview)); items.Children.Add(EdgeButton("Tray", () => TrayArea_Click(TrayArea, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)))); items.Children.Add(EdgeButton("Themes", ShowThemeGallery)); items.Children.Add(EdgeButton("Settings", OpenSettingsSingleton));
        _edgeDock = new Border { Width = 72, Background = new SolidColorBrush(Color.FromArgb(245, 10, 22, 41)), BorderBrush = new SolidColorBrush(Color.FromRgb(51, 92, 137)), BorderThickness = new Thickness(1, 1, 0, 1), CornerRadius = new CornerRadius(15, 0, 0, 15), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(7, 12, 7, 12), Child = items, Margin = new Thickness(0, 0, -63, 0), Opacity = .25 };
        _edgeDock.MouseEnter += (_, _) => { _edgeDock.Margin = new Thickness(0); _edgeDock.Opacity = 1; }; _edgeDock.MouseLeave += (_, _) => { _edgeDock.Margin = new Thickness(0, 0, -63, 0); _edgeDock.Opacity = .25; };
        Panel.SetZIndex(_edgeDock, 350); RootGrid.Children.Add(_edgeDock);
    }

    private Button EdgeButton(string text, Action action) { var b = new Button { Content = text, Height = 39, Margin = new Thickness(0, 3, 0, 3), FontSize = 10, Style = (Style)FindResource("IconButton"), ToolTip = text }; b.Click += (_, _) => action(); return b; }

    private void CreateAssistantBubble()
    {
        var border = new FrameworkElementFactory(typeof(Border)); border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty)); border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty)); border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty)); border.SetValue(Border.CornerRadiusProperty, new CornerRadius(27));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center); presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center); border.AppendChild(presenter);
        _assistantBubble = new Button { Content = "B", Width = 54, Height = 54, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(35, 128, 216)), BorderBrush = new SolidColorBrush(Color.FromRgb(101, 180, 242)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, -27, 88), ToolTip = "BetterHome Assistant", Template = new ControlTemplate(typeof(Button)) { VisualTree = border }, Cursor = Cursors.Hand };
        _assistantBubble.Click += (_, _) => ToggleAssistant(); Panel.SetZIndex(_assistantBubble, 360); RootGrid.Children.Add(_assistantBubble);
    }

    private void ToggleAssistant()
    {
        if (_assistantPanel?.Parent is Panel old) { old.Children.Remove(_assistantPanel); _assistantPanel = null; return; }
        var input = new TextBox { Height = 39, Margin = new Thickness(14, 53, 14, 14), Padding = new Thickness(10), Background = new SolidColorBrush(Color.FromRgb(25, 44, 72)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(55, 102, 155)), ToolTip = "Try: open apps, file hub, themes, settings, running apps" };
        var grid = new Grid(); grid.Children.Add(new TextBlock { Text = "BetterHome Assistant", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(15, 14, 0, 0), VerticalAlignment = VerticalAlignment.Top }); grid.Children.Add(input);
        _assistantPanel = new Border { Width = 390, Height = 115, CornerRadius = new CornerRadius(15), Background = new SolidColorBrush(Color.FromArgb(250, 11, 24, 45)), BorderBrush = new SolidColorBrush(Color.FromRgb(49, 100, 155)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 28, 140), Child = grid };
        Panel.SetZIndex(_assistantPanel, 500); RootGrid.Children.Add(_assistantPanel); input.KeyDown += (_, e) => { if (e.Key == Key.Enter) ExecuteAssistantCommand(input.Text); else if (e.Key == Key.Escape) ToggleAssistant(); }; input.Focus();
    }

    private void ExecuteAssistantCommand(string raw)
    {
        var command = raw.Trim().ToLowerInvariant();
        if (command.Contains("open apps") || command == "apps") ShowAppDrawer(); else if (command.Contains("running") || command.Contains("open apps overview")) ShowOpenAppsOverview(); else if (command.Contains("file hub") || command.Contains("files")) ToggleFileHubFromDock(); else if (command.Contains("settings")) OpenSettingsSingleton(); else if (command.Contains("theme")) ShowThemeGallery(); else if (command.Contains("tray")) TrayArea_Click(TrayArea, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)); else if (command.Contains("pause wallpaper")) { _wallpaperManuallyPaused = true; ApplyWallpaper(); } else if (command.Contains("resume wallpaper")) { _wallpaperManuallyPaused = false; ApplyWallpaper(); } else if (command.Contains("downloads")) SafeAction(() => _fileOperations.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"))); else if (command.Contains("desktop")) SafeAction(() => _fileOperations.Open(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))); else if (command.StartsWith("search ")) { ShowSearch(); } else { ShowToast("Command not found. Try apps, files, running apps, tray, themes or settings"); return; }
        if (_assistantPanel?.Parent is Panel panel) panel.Children.Remove(_assistantPanel); _assistantPanel = null;
    }

    private void ShowAppDrawer()
    {
        if (!_settings.EnableAppDrawer) { ShowToast("App Drawer is disabled in Settings"); return; }
        var apps = _installedApps.Load();
        ShowFeatureOverlay("App Drawer", apps, app => app.Name, app => app.Path, app => SafeAction(() => _fileOperations.Open(app.Path), $"Opening {app.Name}"));
    }

    private void ShowThemeGallery()
    {
        ShowFeatureOverlay("BetterHome Themes", _themeService.Themes, theme => theme.Name, theme => theme.Name == _settings.SelectedTheme ? "Active theme" : "Click to apply", theme =>
        {
            _settings.SelectedTheme = theme.Name; _settingsService.Save(_settings); ApplySelectedTheme(true);
        }, theme =>
        {
            var menu = new ContextMenu(); var favorite = _settings.FavoriteThemes.Contains(theme.Name); AddMenu(menu, favorite ? "Remove favorite" : "Add favorite", () => { if (favorite) _settings.FavoriteThemes.Remove(theme.Name); else _settings.FavoriteThemes.Add(theme.Name); _settingsService.Save(_settings); ShowToast("Theme favorites updated"); }); return menu;
        });
    }

    private void ApplySelectedTheme(bool notify)
    {
        var theme = _themeService.Themes.FirstOrDefault(item => item.Name == _settings.SelectedTheme) ?? _themeService.Themes[0];
        if (ColorConverter.ConvertFromString(theme.Background) is Color background)
        {
            var shade = RootGrid.Children.OfType<Rectangle>().FirstOrDefault(); if (shade != null) shade.Fill = new SolidColorBrush(Color.FromArgb((byte)(theme.Opacity * 150), background.R, background.G, background.B));
        }
        if (_dynamicIsland != null && ColorConverter.ConvertFromString(theme.Accent) is Color accent) _dynamicIsland.BorderBrush = new SolidColorBrush(accent);
        if (notify) ShowToast($"Theme applied: {theme.Name}");
    }

    private void RefreshRunningStrip()
    {
        if (!_settings.ShowRunningAppsStrip) return;
        if (_runningStrip?.Parent is Panel parent) parent.Children.Remove(_runningStrip);
        var host = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var window in _runningWindows.GetOpenWindows().Take(12))
        {
            var button = new Button { Content = window.ProcessName.Length > 2 ? window.ProcessName[..2].ToUpperInvariant() : window.ProcessName.ToUpperInvariant(), Width = 34, Height = 30, Margin = new Thickness(3), Style = (Style)FindResource("IconButton"), Opacity = window.IsMinimized ? .55 : 1, ToolTip = window.Title };
            button.Click += (_, _) => _runningWindows.Focus(window); var menu = new ContextMenu(); AddMenu(menu, "Focus", () => _runningWindows.Focus(window)); AddMenu(menu, "Minimize", () => _runningWindows.Minimize(window)); button.ContextMenu = menu; host.Children.Add(button);
        }
        _runningStrip = new Border { Height = 38, Padding = new Thickness(7, 1, 7, 1), CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromArgb(225, 13, 25, 47)), BorderBrush = new SolidColorBrush(Color.FromRgb(48, 84, 126)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 67), Child = host };
        Panel.SetZIndex(_runningStrip, 280); RootGrid.Children.Add(_runningStrip);
    }

    private void ShowOpenAppsOverview()
    {
        if (!_settings.EnableOpenAppsOverview) { ShowToast("Open Apps Overview is disabled in Settings"); return; }
        var windows = _runningWindows.GetOpenWindows(); _dynamicIslandService.Publish($"{windows.Count} apps running");
        ShowFeatureOverlay("Open Apps", windows, window => window.ProcessName, window => window.Title, window => _runningWindows.Focus(window), window =>
        {
            var menu = new ContextMenu();
            AddMenu(menu, "Focus", () => _runningWindows.Focus(window)); AddMenu(menu, window.IsMinimized ? "Restore" : "Minimize", () => { if (window.IsMinimized) _runningWindows.Focus(window); else _runningWindows.Minimize(window); });
            AddMenu(menu, "Close", () => { if (!_settings.ConfirmBeforeClosingApps || MessageBox.Show($"Close {window.Title}?", "BetterHome", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) _runningWindows.Close(window); });
            if (!string.IsNullOrWhiteSpace(window.ProcessPath)) AddMenu(menu, "Open file location", () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{window.ProcessPath}\"") { UseShellExecute = true }));
            return menu;
        });
    }

    private void ShowFeatureOverlay<T>(string title, IReadOnlyList<T> source, Func<T, string> primary, Func<T, string> secondary, Action<T> open, Func<T, ContextMenu>? context = null)
    {
        CloseFeatureOverlay();
        var search = new TextBox { Height = 40, Margin = new Thickness(22, 58, 22, 0), VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(12, 6, 12, 6), Background = new SolidColorBrush(Color.FromRgb(26, 43, 70)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(59, 104, 157)), ToolTip = $"Search {title}" };
        var host = new WrapPanel { Margin = new Thickness(16) }; var scroll = new ScrollViewer { Margin = new Thickness(6, 105, 6, 12), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = host };
        var panel = new Grid(); panel.Children.Add(new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(22, 18, 0, 0), VerticalAlignment = VerticalAlignment.Top });
        var close = new Button { Content = "×", Width = 32, Height = 32, FontSize = 19, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(220, 72, 78)), BorderBrush = new SolidColorBrush(Color.FromRgb(255, 130, 135)), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 15, 18, 0), Cursor = Cursors.Hand, ToolTip = "Close" };
        var circle = new FrameworkElementFactory(typeof(Border)); circle.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty)); circle.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty)); circle.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty)); circle.SetValue(Border.CornerRadiusProperty, new CornerRadius(16)); var cp = new FrameworkElementFactory(typeof(ContentPresenter)); cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center); cp.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center); circle.AppendChild(cp); close.Template = new ControlTemplate(typeof(Button)) { VisualTree = circle }; close.Click += (_, _) => CloseFeatureOverlay();
        panel.Children.Add(close); panel.Children.Add(search); panel.Children.Add(scroll);
        _featureOverlay = new Border { Width = Math.Min(940, ActualWidth - 80), Height = Math.Min(650, ActualHeight - 90), Background = new SolidColorBrush(Color.FromArgb(249, 11, 23, 43)), BorderBrush = new SolidColorBrush(Color.FromRgb(56, 107, 158)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(20), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = panel, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 38, Opacity = .75 } };
        Panel.SetZIndex(_featureOverlay, 600); RootGrid.Children.Add(_featureOverlay);
        void Render()
        {
            host.Children.Clear(); foreach (var item in source.Where(item => primary(item).Contains(search.Text, StringComparison.OrdinalIgnoreCase) || secondary(item).Contains(search.Text, StringComparison.OrdinalIgnoreCase)))
            {
                var card = new Button { Width = 205, Height = 92, Margin = new Thickness(7), Padding = new Thickness(12), HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(25, 43, 70)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(45, 75, 111)), Cursor = Cursors.Hand, ToolTip = secondary(item) };
                var stack = new StackPanel(); stack.Children.Add(new TextBlock { Text = primary(item), FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis }); stack.Children.Add(new TextBlock { Text = secondary(item), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(153, 174, 205)), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, MaxHeight = 32 }); card.Content = stack;
                card.Click += (_, _) => { open(item); CloseFeatureOverlay(); }; if (context != null) card.ContextMenu = context(item); host.Children.Add(card);
            }
        }
        search.TextChanged += (_, _) => Render(); search.KeyDown += (_, e) => { if (e.Key == Key.Escape) CloseFeatureOverlay(); }; Render(); search.Focus();
    }

    private void CloseFeatureOverlay() { if (_featureOverlay?.Parent is Panel panel) panel.Children.Remove(_featureOverlay); _featureOverlay = null; }
    private void OpenSettingsSingleton()
    {
        if (_settingsWindow is { IsVisible: true }) { _settingsWindow.Activate(); return; }
        _settingsWindow = new Views.SettingsWindow(_settings) { Owner = this }; _settingsWindow.Closed += (_, _) => { if (_settingsWindow?.WasSaved == true) ApplySavedSettings(_settingsWindow); _settingsWindow = null; }; _settingsWindow.Show(); _settingsWindow.Activate();
    }

    private void ApplySavedSettings(Views.SettingsWindow window)
    {
        _settings = window.Settings; FontSize = _settings.UiFontSize; _realFileHub?.ApplyFontSize(_settings.FileHubFontSize); if (window.ResetLayoutRequested) ResetLayout(); AutoToggle.IsChecked = _settings.AutoArrangeOnLaunch;
        if (_widgets != null) _widgets.Visibility = _settings.ShowWidgets ? Visibility.Visible : Visibility.Collapsed; if (_dock != null) _dock.Visibility = _settings.ShowDock ? Visibility.Visible : Visibility.Collapsed; if (_prayerCard != null) _prayerCard.Visibility = _settings.EnablePrayerWidget ? Visibility.Visible : Visibility.Collapsed; FileHub.Visibility = _settings.ShowFileHubOnLaunch ? Visibility.Visible : Visibility.Collapsed; ApplyWallpaper(); RefreshPremiumFeatures(); _ = RefreshPrayerTimesAsync(false); SaveLayout(); ShowToast("Settings saved");
    }

    private void RefreshPremiumFeatures()
    {
        foreach (var element in new FrameworkElement?[] { _dynamicIsland, _miniPlayer, _edgeDock, _assistantPanel, _assistantBubble, _runningStrip }) if (element?.Parent is Panel panel) panel.Children.Remove(element);
        _dynamicIsland = null; _miniPlayer = null; _edgeDock = null; _assistantPanel = null; _assistantBubble = null; _runningStrip = null; _runningAppsTimer.Stop();
        InitializeDynamicIsland(); InitializePremiumFeatures();
    }

    private void WireFileHubButton(Button button, string content)
    {
        if (content == "—") button.Click += (_, _) => ToggleFileHubMinimize();
        else if (content == "□") button.Click += (_, _) => ToggleFileHubMaximize();
        else if (content.Contains("New Folder")) button.Click += (_, _) => ShowInput("New Folder", "Folder name", name => ShowToast($"Created folder {name}"));
        else if (content.Contains("Upload")) button.Click += (_, _) => ShowToast("Upload is ready for a future update");
        else if (content.Contains("Sort")) button.Click += (_, _) => { _sortIndex = (_sortIndex + 1) % _sortModes.Length; ShowToast($"Sort: {_sortModes[_sortIndex]}"); };
        else if (content.Contains("View")) button.Click += (_, _) => { _viewIndex = (_viewIndex + 1) % _viewModes.Length; ShowToast($"View: {_viewModes[_viewIndex]}"); };
    }

    private void WireFileHubRows()
    {
        string[] targets = ["Desktop", "Downloads", "Documents", "Pictures", "Music", "Videos", "Project Proposal.pdf", "Budget 2024.xlsx", "BetterHome Design.sketch", "Meeting Notes.txt", "Presentation.pptx"];
        foreach (var text in Descendants<TextBlock>(FileHub))
        {
            var clean = text.Text.TrimStart('▰', '↓', '▣', '▧', '●', '▶', ' ');
            var match = targets.FirstOrDefault(x => string.Equals(x, clean, StringComparison.OrdinalIgnoreCase));
            if (match == null) continue;
            text.Cursor = Cursors.Hand;
            text.MouseLeftButtonDown += (_, e) => { e.Handled = true; ShowToast($"Opening {match}"); };
        }
    }

    private void WireDockButton(Button button, string glyph)
    {
        var dockButtons = _dock == null ? [] : Descendants<Button>(_dock).ToList();
        var index = dockButtons.IndexOf(button); string[] names = ["Home", "Search", "File Hub", "Browser", "File Explorer", "Show Desktop", "Settings", "Tools"];
        if (index < 0 || index >= names.Length) return; button.ToolTip = names[index]; button.Cursor = Cursors.Hand;
        button.RenderTransformOrigin = new Point(.5, .5);
        button.RenderTransform = new ScaleTransform(1, 1);
        button.MouseEnter += (_, _) => { AnimateScale(button, 1.18); button.Background = new SolidColorBrush(Color.FromArgb(70, 55, 129, 220)); };
        button.MouseLeave += (_, _) => { AnimateScale(button, 1); if (button.Tag?.ToString() != "active") button.Background = Brushes.Transparent; };
        button.Click += async (_, _) =>
        {
            AnimateScale(button, .86); await Task.Delay(80); AnimateScale(button, 1.12);
            SetActiveDockButton(button);
            switch (index)
            {
                case 0: Home(); break;
                case 1: ShowSearch(); break;
                case 2: if (FileHub.Visibility == Visibility.Visible) { Panel.SetZIndex(FileHub, 20); ShowToast("File Hub opened"); } else ShowFileHub(); break;
                case 3: SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.google.com") { UseShellExecute = true }), "Browser opened"); break;
                case 4: SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\"") { UseShellExecute = true }), "File Explorer opened"); break;
                case 5: ToggleDesktopView(); break;
                case 6: Settings_Click(button, new RoutedEventArgs()); break;
                case 7: ShowToolsMenu(button); break;
            }
        };
    }

    private static void AnimateScale(Button button, double to)
    {
        if (button.RenderTransform is not ScaleTransform scale) return;
        var duration = TimeSpan.FromMilliseconds(130);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, duration));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, duration));
    }

    private void SetActiveDockButton(Button selected)
    {
        if (_dock == null) return;
        foreach (var b in Descendants<Button>(_dock)) { b.Background = Brushes.Transparent; b.Tag = null; }
        selected.Background = new SolidColorBrush(Color.FromArgb(90, 60, 108, 180));
        selected.Tag = "active";
    }

    private void ToggleDesktopView()
    {
        _desktopView = !_desktopView; if (_groupsOverlay != null) _groupsOverlay.Visibility = _desktopView ? Visibility.Collapsed : Visibility.Visible; else GroupsCanvas.Visibility = _desktopView ? Visibility.Collapsed : Visibility.Visible;
        if (_widgets != null) _widgets.Visibility = _desktopView ? Visibility.Collapsed : (_settings.ShowWidgets ? Visibility.Visible : Visibility.Collapsed);
        FileHub.Visibility = _desktopView ? Visibility.Collapsed : (_settings.ShowFileHubOnLaunch ? Visibility.Visible : Visibility.Collapsed);
        ShowToast(_desktopView ? "Desktop view" : "BetterHome view");
    }

    private void ShowToolsMenu(Button owner)
    {
        var menu = new ContextMenu { PlacementTarget = owner, Placement = PlacementMode.Top };
        void Add(string name, Action action) { var item = new MenuItem { Header = name }; item.Click += (_, _) => action(); menu.Items.Add(item); }
        Add("Refresh desktop items", () => { LoadDesktopItems(); ShowToast("Desktop items refreshed"); });
        Add("Auto arrange groups", () => { ResetLayout(); ShowToast("Groups arranged"); });
        Add("Reset layout", () => { ResetLayout(); SaveLayout(); ShowToast("Layout reset"); });
        Add("Reload widgets", () => { UpdateClock(); RenderCalendar(); RenderTodos(); UpdateSystemMetrics(); _ = RefreshWeatherAsync(true); });
        Add("Reset widgets", ResetWidgets);
        Add("Reset dock", ResetDock);
        Add("Open logs folder", () => SafeAction(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome")}\"") { UseShellExecute = true })));
        menu.IsOpen = true;
    }

    private void ApplyTooltips()
    {
        foreach (var toggle in Descendants<ToggleButton>(this)) { toggle.Cursor = Cursors.Hand; toggle.ToolTip ??= toggle == AutoToggle ? "Automatically restore panel positions" : "Hide or show Windows desktop icons"; }
        foreach (var button in Descendants<Button>(FileHub))
        {
            var text = button.Content?.ToString() ?? ""; button.Cursor = Cursors.Hand;
            button.ToolTip ??= text switch { "—" => "Minimize File Hub", "□" => "Maximize File Hub", "×" => "Close File Hub", "‹" => "Back", "›" => "Forward", "↑" => "Up one folder", "↻" => "Refresh", _ => string.IsNullOrWhiteSpace(text) ? "File Hub action" : text };
        }
        foreach (var button in Descendants<Button>(this).Where(b => b.ToolTip == null && b.Content?.ToString()?.Contains("Settings") == true)) button.ToolTip = "Open Settings";
    }

    private void WireWidgets()
    {
        foreach (var text in Descendants<TextBlock>(this).ToList())
        {
            if (text.Text is "○  Study for final exam" or "○  Finish project presentation" or "○  Workout" or "○  Read 30 pages") WireTask(text);
            else if (text.Text.Contains("Add Task")) { text.Cursor = Cursors.Hand; text.MouseLeftButtonDown += (_, e) => { e.Handled = true; AddTask(); }; }
            else if (text.Text == "‹") { text.Cursor = Cursors.Hand; text.MouseLeftButtonDown += (_, e) => { e.Handled = true; ChangeMonth(-1); }; }
            else if (text.Text == "›") { text.Cursor = Cursors.Hand; text.MouseLeftButtonDown += (_, e) => { e.Handled = true; ChangeMonth(1); }; }
            else if (text.Text == "Weather") { text.Cursor = Cursors.Hand; text.ToolTip = "Click to refresh"; text.MouseLeftButtonDown += (_, _) => RefreshWeather(); }
            else if (text.Text == "System") { text.Cursor = Cursors.Hand; text.ToolTip = "Click to refresh"; text.MouseLeftButtonDown += (_, _) => RefreshSystem(); }
        }
    }

    private void WireTask(TextBlock text)
    {
        text.Cursor = Cursors.Hand;
        text.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            var done = text.Text.StartsWith("●");
            text.Text = (done ? "○" : "●") + text.Text[1..];
            text.TextDecorations = done ? null : TextDecorations.Strikethrough;
            text.Opacity = done ? 1 : .62;
            SaveLayout();
        };
    }

    private void AddTask() => ShowInput("Add Task", "What needs doing?", task =>
    {
        var add = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.Contains("Add Task"));
        if (add?.Parent is not StackPanel panel) return;
        var item = new TextBlock { Text = $"○  {task}", Margin = new Thickness(0, 7, 0, 0), FontSize = 10, Foreground = (Brush)FindResource("TextPrimary") };
        panel.Children.Insert(panel.Children.IndexOf(add), item); WireTask(item); ShowToast("Task added"); SaveLayout();
    });

    private void ChangeMonth(int delta)
    {
        var month = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text == "May 2024" || t.Tag?.ToString() == "month");
        if (month == null) return;
        var value = month.Tag is DateTime d ? d : new DateTime(2024, 5, 1);
        value = value.AddMonths(delta); month.Tag = value; month.Text = value.ToString("MMMM yyyy"); ShowToast(month.Text);
    }

    private void RefreshWeather()
    {
        var temperature = Random.Shared.Next(18, 29);
        var temp = Descendants<TextBlock>(this).FirstOrDefault(t => t.FontSize == 31 && t.Text.StartsWith("24"));
        if (temp != null) temp.Text = $"{temperature}° C";
        ShowToast("Weather updated");
    }

    private void RefreshSystem()
    {
        var bars = Descendants<ProgressBar>(this).ToList();
        foreach (var bar in bars) bar.Value = Random.Shared.Next(12, 84);
        ShowToast("System values refreshed");
    }

    private void DesktopMode_Click(object sender, RoutedEventArgs e)
    {
        _state.DesktopModeEnabled = !_state.DesktopModeEnabled;
        ShowToast(_state.DesktopModeEnabled ? "Desktop Mode enabled" : "Desktop Mode disabled"); SaveLayout();
    }

    private void Home()
    {
        var fileHubWasVisible = FileHub.Visibility == Visibility.Visible; ResetLayout(); GroupsCanvas.Visibility = Visibility.Visible; if (_groupsOverlay != null) _groupsOverlay.Visibility = Visibility.Visible; FileHub.Visibility = fileHubWasVisible ? Visibility.Visible : Visibility.Collapsed; foreach (var group in _groups.Keys) { group.Visibility = Visibility.Visible; group.Opacity = 1; }
        if (_widgets != null) _widgets.Visibility = Visibility.Visible; if (_dock != null) _dock.Visibility = Visibility.Visible;
        if (_hideGroupsToggle != null) _hideGroupsToggle.IsChecked = false;
        foreach (var pair in _groups.ToList()) { var button = Descendants<Button>(pair.Key).FirstOrDefault(b => Equals(b.Tag, "Collapse")) ?? Descendants<Button>(pair.Key).First(); ToggleGroup(pair.Key, button, false); }
        _desktopView = false; ShowToast("Home"); SaveLayout();
    }

    private void ShowSearch()
    {
        if (_searchOverlay != null) return;
        var root = (Grid)Content;
        var box = new TextBox { FontSize = 18, Height = 42, Margin = new Thickness(18), Padding = new Thickness(12, 6, 12, 6), Background = new SolidColorBrush(Color.FromRgb(24, 37, 65)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(53, 112, 184)) };
        var list = new ListBox { Margin = new Thickness(18, 70, 18, 18), Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), ItemsSource = _searchResults };
        var grid = new Grid(); grid.Children.Add(box); grid.Children.Add(list);
        _searchOverlay = new Border { Width = 520, Height = 430, Background = new SolidColorBrush(Color.FromArgb(246, 12, 22, 43)), BorderBrush = new SolidColorBrush(Color.FromRgb(65, 108, 160)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Child = grid, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 34, Opacity = .7 } };
        Panel.SetZIndex(_searchOverlay, 100); root.Children.Add(_searchOverlay);
        void Filter() { _searchResults.Clear(); foreach (var item in _desktopItems.Select(i => i.Name).Where(i => i.Contains(box.Text, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase)) _searchResults.Add(item); }
        box.TextChanged += (_, _) => Filter(); Filter();
        void OpenSelected() { var name = list.SelectedItem?.ToString() ?? _searchResults.FirstOrDefault(); if (name == null) { ShowToast("No result"); return; } var item = _desktopItems.FirstOrDefault(i => i.Name == name); if (item == null) { ShowToast("No result"); return; } OpenDesktopItem(item); CloseSearch(); }
        list.MouseDoubleClick += (_, _) => OpenSelected(); list.MouseLeftButtonUp += (_, _) => { if (list.SelectedItem != null) OpenSelected(); };
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) OpenSelected(); else if (e.Key == Key.Escape) CloseSearch(); };
        box.Focus();
    }

    private void CloseSearch() { if (_searchOverlay?.Parent is Panel p) p.Children.Remove(_searchOverlay); _searchOverlay = null; }

    private void ShowInput(string title, string placeholder, Action<string> accepted)
    {
        if (_inputOverlay != null) return;
        var root = (Grid)Content;
        var titleText = new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(18, 15, 18, 0) };
        var input = new TextBox { Height = 38, Margin = new Thickness(18, 52, 18, 55), Padding = new Thickness(9), Background = new SolidColorBrush(Color.FromRgb(24, 37, 65)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(53, 112, 184)), ToolTip = placeholder };
        var cancel = new Button { Content = "Cancel", Width = 80, Height = 30, Margin = new Thickness(0, 0, 108, 14), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var add = new Button { Content = "Add", Width = 80, Height = 30, Margin = new Thickness(0, 0, 18, 14), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Background = new SolidColorBrush(Color.FromRgb(35, 127, 219)), Foreground = Brushes.White };
        var grid = new Grid(); grid.Children.Add(titleText); grid.Children.Add(input); grid.Children.Add(cancel); grid.Children.Add(add);
        _inputOverlay = new Border { Width = 390, Height = 155, Background = new SolidColorBrush(Color.FromArgb(250, 15, 25, 48)), CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.FromRgb(60, 105, 160)), BorderThickness = new Thickness(1), Child = grid, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Panel.SetZIndex(_inputOverlay, 110); root.Children.Add(_inputOverlay);
        void Close() { if (_inputOverlay?.Parent is Panel p) p.Children.Remove(_inputOverlay); _inputOverlay = null; }
        void Accept() { var value = input.Text.Trim(); if (value.Length == 0) return; accepted(value); Close(); }
        cancel.Click += (_, _) => Close(); add.Click += (_, _) => Accept(); input.KeyDown += (_, e) => { if (e.Key == Key.Enter) Accept(); else if (e.Key == Key.Escape) Close(); }; input.Focus();
    }

    private async void ShowToast(string message)
    {
        _dynamicIslandService.Publish(message);
        var root = (Grid)Content;
        if (_toast?.Parent is Panel oldParent) oldParent.Children.Remove(_toast);
        _toast = new Border { Background = new SolidColorBrush(Color.FromArgb(245, 18, 31, 56)), BorderBrush = new SolidColorBrush(Color.FromRgb(52, 129, 203)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(18, 11, 18, 11), Margin = new Thickness(0, 0, 28, 82), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Child = new TextBlock { Text = message, FontSize = 12 }, Opacity = 0 };
        Panel.SetZIndex(_toast, 200); root.Children.Add(_toast);
        _toast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)));
        await Task.Delay(2100);
        if (_toast == null) return;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(240)); fade.Completed += (_, _) => { if (_toast?.Parent is Panel p) p.Children.Remove(_toast); _toast = null; }; _toast.BeginAnimation(OpacityProperty, fade);
    }

    private void ToggleFileHubMinimize()
    {
        _fileHubMinimized = !_fileHubMinimized;
        if (FileHub.Child is Grid grid) for (var i = 1; i < grid.Children.Count; i++) grid.Children[i].Visibility = _fileHubMinimized ? Visibility.Collapsed : Visibility.Visible;
        FileHub.Height = _fileHubMinimized ? 38 : (_fileHubMaximized ? Math.Max(360, CenterCanvas.ActualHeight - 20) : _fileHubHeight); SaveLayout();
    }

    private void ToggleFileHubMaximize()
    {
        if (!_fileHubMaximized) { _fileHubLeft = Canvas.GetLeft(FileHub); _fileHubTop = Canvas.GetTop(FileHub); _fileHubWidth = FileHub.Width; _fileHubHeight = FileHub.Height; var host = _fileHubOverlay ?? CenterCanvas; FileHub.Width = Math.Max(700, host.ActualWidth); FileHub.Height = Math.Max(500, host.ActualHeight); Canvas.SetLeft(FileHub, 0); Canvas.SetTop(FileHub, 0); _fileHubMaximized = true; }
        else { FileHub.Width = _fileHubWidth; FileHub.Height = _fileHubHeight; Canvas.SetLeft(FileHub, _fileHubLeft); Canvas.SetTop(FileHub, _fileHubTop); _fileHubMaximized = false; }
        _fileHubMinimized = false; if (FileHub.Child is Grid grid) foreach (UIElement child in grid.Children) child.Visibility = Visibility.Visible; SaveLayout();
    }

    private void ApplyResponsiveScale()
    {
        if (ActualHeight <= 0) return;
        var scale = Math.Min(1.0, Math.Max(.72, ActualHeight / 900)); GroupsCanvas.LayoutTransform = new ScaleTransform(scale, scale); if (_widgets != null) { var widgetScale = Math.Min(1.0, Math.Max(.68, (ActualHeight - 105) / 920)); _widgets.LayoutTransform = new ScaleTransform(widgetScale, widgetScale); }
        LayoutDesktopGroups();
        if (_fileHubOverlay != null && !_fileHubMinimized) { FileHub.Width = Math.Max(980, ActualWidth - 68); FileHub.Height = Math.Max(620, ActualHeight - 60); Canvas.SetLeft(FileHub, 0); Canvas.SetTop(FileHub, 0); _fileHubWidth = FileHub.Width; _fileHubHeight = FileHub.Height; }
    }

    private void DragPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        _dragging = (FrameworkElement)sender;
        if (_dragging == FileHub && e.GetPosition(FileHub).Y > 40) { _dragging = null; return; }
        _dragStart = e.GetPosition(_dragging.Parent as IInputElement); _startLeft = Value(Canvas.GetLeft(_dragging)); _startTop = Value(Canvas.GetTop(_dragging)); _dragging.CaptureMouse(); e.Handled = true;
    }

    private void DragPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging is null || e.LeftButton != MouseButtonState.Pressed) return;
        var parent = (FrameworkElement)_dragging.Parent; var p = e.GetPosition(parent);
        Canvas.SetLeft(_dragging, Math.Clamp(_startLeft + p.X - _dragStart.X, 0, Math.Max(0, parent.ActualWidth - _dragging.ActualWidth)));
        Canvas.SetTop(_dragging, Math.Clamp(_startTop + p.Y - _dragStart.Y, 0, Math.Max(0, parent.ActualHeight - _dragging.ActualHeight)));
    }

    private void DragPanel_MouseUp(object sender, MouseButtonEventArgs e) { _dragging?.ReleaseMouseCapture(); _dragging = null; SaveLayout(); }
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsSingleton();
    }
    private void CloseFileHub_Click(object sender, RoutedEventArgs e) => CloseFileHub();
    private void MinimizeFileHub_Click(object sender, RoutedEventArgs e) { e.Handled = true; ToggleFileHubMinimize(); }
    private void MaximizeFileHub_Click(object sender, RoutedEventArgs e) { e.Handled = true; ToggleFileHubMaximize(); }
    private void CloseFileHub() { FileHub.Visibility = Visibility.Collapsed; SaveLayout(); }
    private void ShowFileHub_Click(object sender, RoutedEventArgs e) => ShowFileHub();
    private void ShowFileHub() { FileHub.Visibility = Visibility.Visible; ShowToast("File Hub opened"); SaveLayout(); }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { if (_featureOverlay != null) CloseFeatureOverlay(); else if (_smartTray != null) { RootGrid.Children.Remove(_smartTray); _smartTray = null; } else if (_searchOverlay != null) CloseSearch(); else if (_inputOverlay != null && _inputOverlay.Parent is Panel p) { p.Children.Remove(_inputOverlay); _inputOverlay = null; } }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.A) { ShowAppDrawer(); e.Handled = true; }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Tab) { ShowOpenAppsOverview(); e.Handled = true; }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F) { ToggleFileHubFromDock(); e.Handled = true; }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.T) { TrayArea_Click(TrayArea, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)); e.Handled = true; }
        else if (e.Key == Key.F11) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    private void AutoArrange_Click(object sender, RoutedEventArgs e) { _state.AutoArrangeEnabled = AutoToggle.IsChecked == true; if (_state.AutoArrangeEnabled) ResetLayout(); ShowToast(_state.AutoArrangeEnabled ? "Auto Arrange enabled" : "Auto Arrange disabled"); SaveLayout(); }
    private void ResetLayout() { if (_groupsOverlay != null) LayoutDesktopGroups(); else { Canvas.SetTop(WorkGroup, 0); Canvas.SetTop(AppsGroup, 163); Canvas.SetTop(FoldersGroup, 339); Canvas.SetTop(GamesGroup, 502); foreach (var p in _groups.Keys) Canvas.SetLeft(p, 0); } Canvas.SetLeft(FileHub, 0); Canvas.SetTop(FileHub, 0); }

    private void ApplySettingsOnLaunch()
    {
        if (_settings.AutoArrangeOnLaunch) ResetLayout();
        if (_widgets != null) _widgets.Visibility = _settings.ShowWidgets ? Visibility.Visible : Visibility.Collapsed;
        if (_dock != null) _dock.Visibility = _settings.ShowDock ? Visibility.Visible : Visibility.Collapsed;
        FileHub.Visibility = _settings.ShowFileHubOnLaunch ? Visibility.Visible : Visibility.Collapsed;
        AutoToggle.IsChecked = _settings.AutoArrangeOnLaunch;
        if (_hideGroupsToggle != null) _hideGroupsToggle.IsChecked = _settings.HideDesktopIconsOnLaunch;
        if (_settings.HideDesktopIconsOnLaunch && !_desktopVisibility.TrySetVisible(false, out var error)) ShowToast(error ?? "Desktop icons could not be hidden");
    }

    private void LoadLayout()
    {
        _loading = true;
        _state = _storage.Load() ?? new LayoutState(); Apply(FileHub, _state.FileHub); if (_groupsOverlay == null) { Apply(WorkGroup, _state.Work); Apply(AppsGroup, _state.Apps); Apply(FoldersGroup, _state.Folders); Apply(GamesGroup, _state.Games); } else LayoutDesktopGroups();
        AutoToggle.IsChecked = _state.AutoArrangeEnabled; if (_hideGroupsToggle != null) _hideGroupsToggle.IsChecked = !_state.GroupsVisible;
        ToggleGroup(WorkGroup, Descendants<Button>(WorkGroup).FirstOrDefault(b => Equals(b.Tag, "Collapse"))!, _state.WorkCollapsed); ToggleGroup(AppsGroup, Descendants<Button>(AppsGroup).FirstOrDefault(b => Equals(b.Tag, "Collapse"))!, _state.AppsCollapsed); ToggleGroup(FoldersGroup, Descendants<Button>(FoldersGroup).FirstOrDefault(b => Equals(b.Tag, "Collapse"))!, _state.FoldersCollapsed); ToggleGroup(GamesGroup, Descendants<Button>(GamesGroup).FirstOrDefault(b => Equals(b.Tag, "Collapse"))!, _state.GamesCollapsed);
        ApplyVisibilityState(); RestoreTasks(); _loading = false;
    }

    private void RestoreTasks()
    {
        foreach (var task in _state.TodoTasks.Where(t => !_allItems.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            var add = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.Contains("Add Task")); if (add?.Parent is not StackPanel panel) break;
            if (Descendants<TextBlock>(panel).Any(t => t.Text.EndsWith(task))) continue;
            var item = new TextBlock { Text = $"○  {task}", Margin = new Thickness(0, 7, 0, 0), FontSize = 10, Foreground = (Brush)FindResource("TextPrimary") }; panel.Children.Insert(panel.Children.IndexOf(add), item); WireTask(item);
        }
        foreach (var task in _state.CompletedTasks) { var text = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.EndsWith(task)); if (text != null) { text.Text = "●" + text.Text[1..]; text.TextDecorations = TextDecorations.Strikethrough; text.Opacity = .62; } }
    }

    private void ApplyVisibilityState() { if (_groupsOverlay != null) _groupsOverlay.Visibility = _state.GroupsVisible ? Visibility.Visible : Visibility.Collapsed; else GroupsCanvas.Visibility = _state.GroupsVisible ? Visibility.Visible : Visibility.Collapsed; FileHub.Visibility = _state.FileHubVisible ? Visibility.Visible : Visibility.Collapsed; if (_widgets != null) _widgets.Visibility = _state.WidgetsVisible ? Visibility.Visible : Visibility.Collapsed; if (_dock != null) _dock.Visibility = _state.DockVisible ? Visibility.Visible : Visibility.Collapsed; }

    private void SaveLayout()
    {
        _state.FileHub = Pos(FileHub); _state.Work = Pos(WorkGroup); _state.Apps = Pos(AppsGroup); _state.Folders = Pos(FoldersGroup); _state.Games = Pos(GamesGroup);
        _state.WorkCollapsed = _groups.GetValueOrDefault(WorkGroup).Collapsed; _state.AppsCollapsed = _groups.GetValueOrDefault(AppsGroup).Collapsed; _state.FoldersCollapsed = _groups.GetValueOrDefault(FoldersGroup).Collapsed; _state.GamesCollapsed = _groups.GetValueOrDefault(GamesGroup).Collapsed;
        _state.GroupsVisible = (_groupsOverlay?.Visibility ?? GroupsCanvas.Visibility) == Visibility.Visible; _state.FileHubVisible = FileHub.Visibility == Visibility.Visible; _state.WidgetsVisible = _widgets?.Visibility != Visibility.Collapsed; _state.DockVisible = _dock?.Visibility != Visibility.Collapsed; _state.AutoArrangeEnabled = AutoToggle.IsChecked == true;
        var todoPanel = Descendants<TextBlock>(this).FirstOrDefault(t => t.Text.Contains("Add Task"))?.Parent as StackPanel;
        var tasks = todoPanel == null ? [] : Descendants<TextBlock>(todoPanel).Where(t => t.Text.StartsWith("○  ") || t.Text.StartsWith("●  ")).ToList();
        _state.TodoTasks = tasks.Select(t => t.Text[3..]).Where(x => !new[] { "Study for final exam", "Finish project presentation", "Workout", "Read 30 pages" }.Contains(x)).ToList(); _state.CompletedTasks = tasks.Where(t => t.Text.StartsWith("●")).Select(t => t.Text[3..]).ToList(); _storage.Save(_state);
    }

    private Border? FindDock() => Descendants<Border>(this).FirstOrDefault(b => Descendants<Button>(b).Count(x => new[] { "⊞", "⌕", "▣", "◉", "❯", "●", "⚙", "♜" }.Contains(x.Content?.ToString())) >= 8);
    private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject { DependencyObject? current = element; while ((current = VisualTreeHelper.GetParent(current)) != null) if (current is T match) return match; return null; }
    private StackPanel? FindAncestorStack(DependencyObject element, bool unused) { DependencyObject? current = element; while ((current = VisualTreeHelper.GetParent(current)) != null) if (current is StackPanel s && Descendants<TextBlock>(s).Any(t => t.Text == "Weather")) return s; return Descendants<StackPanel>(this).FirstOrDefault(s => Descendants<TextBlock>(s).Any(t => t.Text == "Weather") && Descendants<TextBlock>(s).Any(t => t.Text == "Calendar")); }
    private static bool IsInside(DependencyObject child, DependencyObject ancestor) { DependencyObject? p = child; while ((p = VisualTreeHelper.GetParent(p)) != null) if (p == ancestor) return true; return false; }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static void Apply(FrameworkElement e, PanelPosition p) { Canvas.SetLeft(e, p.X); Canvas.SetTop(e, p.Y); }
    private static PanelPosition Pos(FrameworkElement e) => new() { X = Value(Canvas.GetLeft(e)), Y = Value(Canvas.GetTop(e)) };
    private static double Value(double value) => double.IsNaN(value) ? 0 : value;
}
