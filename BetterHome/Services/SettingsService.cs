using System.Text.Json;
using BetterHome.Models;

namespace BetterHome.Services;

public sealed class SettingsService
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome", "settings.json");
    public AppSettings Load() { try { return File.Exists(_path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new() : new(); } catch { return new(); } }
    public void Save(AppSettings settings) { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })); }
    public void Reset() { if (File.Exists(_path)) File.Delete(_path); }
}
