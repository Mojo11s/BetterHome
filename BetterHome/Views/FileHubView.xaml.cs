using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetterHome.Models;
using BetterHome.Services;
using BetterHome.ViewModels;

namespace BetterHome.Views;

public partial class FileHubView : UserControl
{
    private int _viewMode;
    private readonly IconExtractionService _iconService = new();
    private readonly List<string> _tabs = [];
    private int _activeTab;
    private bool _switchingTab;
    public FileHubViewModel ViewModel { get; }
    public event Action<string>? Toast;
    private ExplorerItem? Selected => DetailsList.Visibility == Visibility.Visible ? DetailsList.SelectedItem as ExplorerItem : AlternateList.SelectedItem as ExplorerItem;

    public FileHubView(double fontSize = 12)
    {
        InitializeComponent(); ApplyFontSize(fontSize); ViewModel = new(new FileExplorerService(_iconService), new FileOperationService()); ViewModel.Toast += value => Toast?.Invoke(value); ViewModel.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ViewModel.CurrentPath)) { if (!_switchingTab && _tabs.Count > _activeTab) { _tabs[_activeTab] = ViewModel.CurrentPath; BuildTabs(); } BuildBreadcrumbs(); UpdateDriveStatus(); } }; DataContext = ViewModel;
        Loaded += (_, _) => { _tabs.Add(ViewModel.CurrentPath); BuildNavigation(); BuildTabs(); BuildBreadcrumbs(); UpdateDriveStatus(); UpdateDetails(null); };
    }

    public void ApplyFontSize(double size) => FontSize = Math.Clamp(size, 10, 18);

    private void BuildNavigation()
    {
        NavigationHost.Children.Clear(); NavigationHost.Children.Add(new TextBlock { Text = "Quick Access", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(169, 187, 213)), Margin = new Thickness(8, 8, 0, 9) });
        AddNav("Home", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); AddNav("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); AddNav("Downloads", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); AddNav("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); AddNav("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)); AddNav("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)); AddNav("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        NavigationHost.Children.Add(new TextBlock { Text = "This PC", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(169, 187, 213)), Margin = new Thickness(8, 18, 0, 7) });
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady)) AddNav($"Drive {drive.Name}", drive.RootDirectory.FullName);
    }

    private void AddNav(string name, string path)
    {
        var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = new Image { Source = _iconService.GetIcon(path, true), Width = 19, Height = 19, VerticalAlignment = VerticalAlignment.Center }; var label = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 }; var marker = new TextBlock { Text = name.StartsWith("Drive ") ? "›" : "⌖", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(133, 158, 190)), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 1); Grid.SetColumn(marker, 2); row.Children.Add(icon); row.Children.Add(label); row.Children.Add(marker);
        var button = new Button { Content = row, Tag = path, Style = (Style)FindResource("FileHubSidebarItemStyle"), HorizontalContentAlignment = HorizontalAlignment.Stretch, ToolTip = path, Cursor = Cursors.Hand };
        button.Click += (_, _) => { ViewModel.Navigate(path); UpdateDriveStatus(); }; NavigationHost.Children.Add(button);
    }

    private void BuildBreadcrumbs()
    {
        if (BreadcrumbHost == null) return; BreadcrumbHost.Children.Clear(); var path = ViewModel.CurrentPath; var root = Path.GetPathRoot(path) ?? path; var segments = path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries); var current = root;
        AddCrumb(root.TrimEnd('\\'), root); foreach (var segment in segments) { current = Path.Combine(current, segment); AddSeparator(); AddCrumb(segment, current); }
        void AddSeparator() => BreadcrumbHost.Children.Add(new TextBlock { Text = "›", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(109, 132, 163)), FontSize = 17, Margin = new Thickness(5, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
        void AddCrumb(string text, string target) { var button = new Button { Content = text, Tag = target, Style = (Style)FindResource("FileHubCommandButtonStyle"), FontWeight = string.Equals(target, path, StringComparison.OrdinalIgnoreCase) ? FontWeights.SemiBold : FontWeights.Normal }; button.Click += (_, _) => ViewModel.Navigate((string)button.Tag); BreadcrumbHost.Children.Add(button); }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel.Back();
    private void Forward_Click(object sender, RoutedEventArgs e) => ViewModel.Forward();
    private void Up_Click(object sender, RoutedEventArgs e) => ViewModel.Up();
    private void Refresh_Click(object sender, RoutedEventArgs e) => ViewModel.Refresh();
    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Open folder in a new File Hub tab", InitialDirectory = ViewModel.CurrentPath, Multiselect = false };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return; _tabs.Add(dialog.FolderName); _activeTab = _tabs.Count - 1; SwitchToTab(_activeTab); Toast?.Invoke($"Opened {Path.GetFileName(dialog.FolderName)} in a new tab");
    }

    private void BuildTabs()
    {
        if (TabHost == null) return; var plus = TabHost.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Content, "＋")); TabHost.Children.Clear();
        for (var index = 0; index < _tabs.Count; index++)
        {
            var tabIndex = index; var path = _tabs[index]; var title = new Grid { Width = 175, Height = 27 }; title.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) }); title.ColumnDefinitions.Add(new ColumnDefinition()); title.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            var image = new Image { Source = _iconService.GetIcon(path, true), Width = 19, Height = 19, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            var name = new TextBlock { Text = string.IsNullOrWhiteSpace(Path.GetFileName(path)) ? path : Path.GetFileName(path), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 0, 8, 0), TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 12 };
            var closeText = new TextBlock { Text = "×", FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var close = new Border { Width = 21, Height = 21, CornerRadius = new CornerRadius(11), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(181, 57, 65)), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 105, 111)), BorderThickness = new Thickness(1), Child = closeText, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Close this tab", Cursor = Cursors.Hand };
            close.MouseEnter += (_, _) => close.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 67, 75)); close.MouseLeave += (_, _) => close.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(181, 57, 65)); close.MouseLeftButtonDown += (_, args) => { args.Handled = true; CloseTab(tabIndex); };
            Grid.SetColumn(name, 1); Grid.SetColumn(close, 2); title.Children.Add(image); title.Children.Add(name); title.Children.Add(close);
            var button = new Button { Content = title, Style = (Style)FindResource(_activeTab == index ? "FileHubTabStyle" : "FileHubCommandButtonStyle"), Tag = index, ToolTip = path, Cursor = Cursors.Hand, Margin = new Thickness(2, 0, 3, 0) }; button.Click += (_, _) => SwitchToTab(tabIndex); TabHost.Children.Add(button);
        }
        if (plus != null) TabHost.Children.Add(plus);
    }

    private void SwitchToTab(int index) { if (index < 0 || index >= _tabs.Count) return; _activeTab = index; _switchingTab = true; ViewModel.Navigate(_tabs[index], false); _switchingTab = false; BuildTabs(); }
    private void CloseTab(int index) { if (_tabs.Count == 1) { Toast?.Invoke("File Hub needs at least one open tab"); return; } _tabs.RemoveAt(index); _activeTab = Math.Clamp(_activeTab >= index ? _activeTab - 1 : _activeTab, 0, _tabs.Count - 1); SwitchToTab(_activeTab); }
    private void Address_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) ViewModel.Navigate(AddressBox.Text); }
    private void Items_DoubleClick(object sender, MouseButtonEventArgs e) => ViewModel.Open(Selected);
    private void Open_Click(object sender, RoutedEventArgs e) => ViewModel.Open(Selected);
    private void OpenLocation_Click(object sender, RoutedEventArgs e) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{ViewModel.CurrentPath}\"") { UseShellExecute = true }); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    private void Copy_Click(object sender, RoutedEventArgs e) { if (Selected is { } item) ViewModel.Copy(item); }
    private void Cut_Click(object sender, RoutedEventArgs e) { if (Selected is { } item) ViewModel.Cut(item); }
    private async void Paste_Click(object sender, RoutedEventArgs e) => await ViewModel.PasteAsync();
    private async void Delete_Click(object sender, RoutedEventArgs e) { if (Selected is { } item && MessageBox.Show($"Move '{item.Name}' to the Recycle Bin?", "BetterHome", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) await ViewModel.DeleteAsync(item); }
    private void Rename_Click(object sender, RoutedEventArgs e) { if (Selected is { } item) Prompt("Rename", item.Name, async value => await ViewModel.RenameAsync(item, value)); }
    private void NewFolder_Click(object sender, RoutedEventArgs e) => Prompt("New Folder", "New folder", async value => await ViewModel.NewFolderAsync(value));
    private void Properties_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } item) return;
        try { var info = item.IsDirectory ? (FileSystemInfo)new DirectoryInfo(item.FullPath) : new FileInfo(item.FullPath); var size = item.IsDirectory ? "—" : item.SizeText; MessageBox.Show($"Name: {item.Name}\n\nPath: {item.FullPath}\n\nType: {item.Type}\nSize: {size}\nCreated: {info.CreationTime:g}\nModified: {info.LastWriteTime:g}", "BetterHome Properties", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { Toast?.Invoke(ex.Message); }
    }

    private void View_Click(object sender, RoutedEventArgs e)
    {
        _viewMode = (_viewMode + 1) % 3;
        if (_viewMode == 0) { DetailsList.Visibility = Visibility.Visible; AlternateList.Visibility = Visibility.Collapsed; ViewButton.Content = "Details"; }
        else { DetailsList.Visibility = Visibility.Collapsed; AlternateList.Visibility = Visibility.Visible; AlternateList.ItemTemplate = (DataTemplate)FindResource(_viewMode == 1 ? "ListItem" : "GridItem"); AlternateList.ItemsPanel = _viewMode == 2 ? (ItemsPanelTemplate)FindResource("WrapItems") : new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))); ViewButton.Content = _viewMode == 1 ? "List" : "Grid"; }
        Toast?.Invoke($"View: {ViewButton.Content}");
    }

    private void GridView_Click(object sender, RoutedEventArgs e) => SetView(2);
    private void ListView_Click(object sender, RoutedEventArgs e) => SetView(1);
    private void DetailsView_Click(object sender, RoutedEventArgs e) => SetView(0);
    private void SetView(int mode)
    {
        _viewMode = mode; DetailsList.Visibility = mode == 0 ? Visibility.Visible : Visibility.Collapsed; AlternateList.Visibility = mode == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (mode != 0) { AlternateList.ItemTemplate = (DataTemplate)FindResource(mode == 1 ? "ListItem" : "GridItem"); AlternateList.ItemsPanel = mode == 2 ? (ItemsPanelTemplate)FindResource("WrapItems") : new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))); }
        ViewButton.Content = mode == 0 ? "☷  Details" : mode == 1 ? "☰  List" : "▦  Grid"; Toast?.Invoke($"View: {(mode == 0 ? "Details" : mode == 1 ? "List" : "Grid")}");
    }

    private void Share_Click(object sender, RoutedEventArgs e) { if (Selected is { } item) try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{item.FullPath}\"") { UseShellExecute = true }); Toast?.Invoke("Selected item opened in Windows Explorer for sharing"); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    private void Pin_Click(object sender, RoutedEventArgs e) { if (Selected is { } item) Toast?.Invoke($"{item.Name} pinned in File Hub"); }

    private void Selection_Changed(object sender, SelectionChangedEventArgs e) { var item = Selected; ViewModel.SelectedItem = item; StatusText.Text = $"{ViewModel.Items.Count} items"; SelectionStatus.Text = item == null ? "0 selected" : "1 item selected"; UpdateDetails(item); }

    private void UpdateDetails(ExplorerItem? item)
    {
        EmptyDetails.Visibility = item == null ? Visibility.Visible : Visibility.Collapsed; DetailsPane.Visibility = item == null ? Visibility.Collapsed : Visibility.Visible; if (item == null) return;
        PreviewIcon.Source = item.Icon; DetailName.Text = item.Name; DetailType.Text = item.Type; DetailLocation.Text = Path.GetDirectoryName(item.FullPath) ?? item.FullPath; DetailSize.Text = item.IsDirectory ? "—" : item.SizeText; DetailModified.Text = item.DateModified.ToString("g");
        try { DetailCreated.Text = (item.IsDirectory ? (FileSystemInfo)new DirectoryInfo(item.FullPath) : new FileInfo(item.FullPath)).CreationTime.ToString("g"); } catch { DetailCreated.Text = "—"; }
    }

    private void UpdateDriveStatus()
    {
        try { var root = Path.GetPathRoot(ViewModel.CurrentPath); var drive = DriveInfo.GetDrives().FirstOrDefault(d => string.Equals(d.Name, root, StringComparison.OrdinalIgnoreCase) && d.IsReady); if (drive == null) return; var used = drive.TotalSize - drive.AvailableFreeSpace; DriveText.Text = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})"; DriveBar.Value = drive.TotalSize == 0 ? 0 : used * 100d / drive.TotalSize; FreeSpaceText.Text = $"{FormatBytes(drive.AvailableFreeSpace)} free of {FormatBytes(drive.TotalSize)}"; } catch { }
    }
    private static string FormatBytes(long value) { string[] units = ["B", "KB", "MB", "GB", "TB"]; double size = value; var unit = 0; while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; } return $"{size:0.#} {units[unit]}"; }
    private void Items_MouseMove(object sender, MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed && Selected is { } item) DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DataFormats.FileDrop, new[] { item.FullPath }), DragDropEffects.Copy | DragDropEffects.Move); }
    private async void Items_Drop(object sender, DragEventArgs e) { if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return; foreach (var path in (string[])e.Data.GetData(DataFormats.FileDrop)) await ViewModel.CopyExternalAsync(path); }

    private void Prompt(string title, string initial, Func<string, Task> accepted)
    {
        var dialog = new Window { Title = title, Width = 380, Height = 155, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 24, 44)), Foreground = System.Windows.Media.Brushes.White, ResizeMode = ResizeMode.NoResize };
        var grid = new Grid { Margin = new Thickness(14) }; grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); var box = new TextBox { Text = initial, Height = 32, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 51, 79)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 94, 140)), Padding = new Thickness(7) }; var ok = new Button { Content = "OK", Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) }; Grid.SetRow(ok, 1); grid.Children.Add(box); grid.Children.Add(ok); dialog.Content = grid; ok.Click += async (_, _) => { if (!string.IsNullOrWhiteSpace(box.Text)) { await accepted(box.Text.Trim()); dialog.Close(); } }; dialog.ShowDialog();
    }
}
