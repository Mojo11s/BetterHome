using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BetterHome.ViewModels;

public sealed class DockItemViewModel : INotifyPropertyChanged
{
    private bool _isActive;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required object Icon { get; init; }
    public string Tooltip => Name;
    public required ICommand Command { get; init; }
    public bool IsActive { get => _isActive; set { if (_isActive == value) return; _isActive = value; PropertyChanged?.Invoke(this, new(nameof(IsActive))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}
