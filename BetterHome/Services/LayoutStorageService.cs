using System.IO;
using System.Text.Json;
using BetterHome.Models;
namespace BetterHome.Services;
public sealed class LayoutStorageService
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome", "layout.json");
    public LayoutState? Load() { try { return File.Exists(_path) ? JsonSerializer.Deserialize<LayoutState>(File.ReadAllText(_path)) : null; } catch { return null; } }
    public void Save(LayoutState state) { try { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true })); } catch { } }
}
