using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BetterHome.Models;
using BetterHome.Services;

namespace BetterHome.ViewModels;

public sealed class FileHubViewModel : INotifyPropertyChanged
{
    private readonly FileExplorerService _explorer;
    private readonly FileOperationService _operations;
    private readonly List<string> _back = [], _forward = [];
    private string _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), _searchText = "", _sortMode = "Name";
    private string? _clipboardPath; private bool _cut;
    public ObservableCollection<ExplorerItem> Items { get; } = [];
    public ExplorerItem? SelectedItem { get; set; }
    public event Action<string>? Toast;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string CurrentPath { get => _currentPath; set { _currentPath = value; Changed(); } }
    public string SearchText { get => _searchText; set { _searchText = value; Changed(); Refresh(); } }
    public string SortMode { get => _sortMode; set { _sortMode = value; Changed(); Refresh(); } }
    public FileHubViewModel(FileExplorerService explorer, FileOperationService operations) { _explorer = explorer; _operations = operations; Refresh(); }
    public void Navigate(string path, bool history = true) { try { if (!Directory.Exists(path)) return; if (history && !string.Equals(path, CurrentPath, StringComparison.OrdinalIgnoreCase)) { _back.Add(CurrentPath); _forward.Clear(); } CurrentPath = path; Refresh(); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void Back() { if (_back.Count == 0) return; var target = _back[^1]; _back.RemoveAt(_back.Count - 1); _forward.Add(CurrentPath); Navigate(target, false); }
    public void Forward() { if (_forward.Count == 0) return; var target = _forward[^1]; _forward.RemoveAt(_forward.Count - 1); _back.Add(CurrentPath); Navigate(target, false); }
    public void Up() { var parent = Directory.GetParent(CurrentPath); if (parent != null) Navigate(parent.FullName); }
    public void Open(ExplorerItem? item) { if (item == null) return; try { if (item.IsDirectory) Navigate(item.FullPath); else _operations.Open(item.FullPath); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void OpenLocation(ExplorerItem? item) { try { if (item != null) _operations.OpenLocation(item.FullPath); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void Properties(ExplorerItem? item) { try { if (item != null) _operations.Properties(item.FullPath); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void NewFolder(string name) { try { Directory.CreateDirectory(Path.Combine(CurrentPath, name)); Toast?.Invoke($"Created {name}"); Refresh(); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void Rename(ExplorerItem item, string name) { try { _operations.Rename(item.FullPath, name); Toast?.Invoke("Renamed successfully"); Refresh(); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void Delete(ExplorerItem item) { try { _operations.DeleteToRecycleBin(item.FullPath); Toast?.Invoke($"Moved {item.Name} to Recycle Bin"); Refresh(); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public void Copy(ExplorerItem item) { _clipboardPath = item.FullPath; _cut = false; Toast?.Invoke($"Copied {item.Name}"); }
    public void Cut(ExplorerItem item) { _clipboardPath = item.FullPath; _cut = true; Toast?.Invoke($"Cut {item.Name}"); }
    public void Paste() { if (_clipboardPath == null) return; try { if (_cut) _operations.Move(_clipboardPath, CurrentPath); else _operations.Copy(_clipboardPath, CurrentPath); Toast?.Invoke("Paste complete"); if (_cut) _clipboardPath = null; Refresh(); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    public Task NewFolderAsync(string name) => RunOperationAsync(() => Directory.CreateDirectory(Path.Combine(CurrentPath, name)), $"Created {name}");
    public Task RenameAsync(ExplorerItem item, string name) => RunOperationAsync(() => _operations.Rename(item.FullPath, name), "Renamed successfully");
    public Task DeleteAsync(ExplorerItem item) => RunOperationAsync(() => _operations.DeleteToRecycleBin(item.FullPath), $"Moved {item.Name} to Recycle Bin");
    public async Task PasteAsync()
    {
        if (_clipboardPath == null) { Toast?.Invoke("Nothing to paste"); return; }
        var source = _clipboardPath; var cut = _cut; await RunOperationAsync(() => { if (cut) _operations.Move(source, CurrentPath); else _operations.Copy(source, CurrentPath); }, "Paste complete"); if (cut) _clipboardPath = null;
    }
    public Task CopyExternalAsync(string source) => RunOperationAsync(() => _operations.Copy(source, CurrentPath), $"Copied {Path.GetFileName(source)}");
    private async Task RunOperationAsync(Action operation, string success)
    {
        try { await Task.Run(operation); await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { Refresh(); Toast?.Invoke(success); }); }
        catch (UnauthorizedAccessException) { await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Toast?.Invoke("Access denied for this operation")); }
        catch (IOException ex) { await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Toast?.Invoke(ex.Message)); }
        catch (Exception ex) { await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Toast?.Invoke(ex.Message)); }
    }
    public void Refresh() { try { IEnumerable<ExplorerItem> items = _explorer.GetItems(CurrentPath); if (!string.IsNullOrWhiteSpace(SearchText)) items = items.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)); items = SortMode switch { "Date" => items.OrderByDescending(i => i.DateModified), "Type" => items.OrderBy(i => i.Type), "Size" => items.OrderByDescending(i => i.Size), _ => items.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase) }; Items.Clear(); foreach (var item in items) Items.Add(item); } catch (Exception ex) { Toast?.Invoke(ex.Message); } }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
