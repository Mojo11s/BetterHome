using System.Text.Json;
using BetterHome.Models;
namespace BetterHome.Services;
public sealed class TodoService
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome", "todos.json");
    public List<TodoItem> Load() { try { return File.Exists(_path) ? JsonSerializer.Deserialize<List<TodoItem>>(File.ReadAllText(_path)) ?? [] : []; } catch { return []; } }
    public void Save(IEnumerable<TodoItem> items) { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true })); }
}
