using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using BetterHome.Models;
namespace BetterHome.Services;
public sealed class WeatherService
{
    private readonly string _cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome", "weather-cache.json");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    public async Task<WeatherData> GetCairoAsync()
    {
        var url = "https://api.open-meteo.com/v1/forecast?latitude=30.0444&longitude=31.2357&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,visibility";
        var json = await Client.GetStringAsync(url); using var doc = JsonDocument.Parse(json); var current = doc.RootElement.GetProperty("current");
        var data = new WeatherData { Temperature = current.GetProperty("temperature_2m").GetDouble(), Humidity = current.GetProperty("relative_humidity_2m").GetDouble(), WeatherCode = current.GetProperty("weather_code").GetInt32(), WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(), Visibility = current.TryGetProperty("visibility", out var visibility) ? visibility.GetDouble() / 1000 : 0, UpdatedAt = DateTime.Now };
        Directory.CreateDirectory(Path.GetDirectoryName(_cache)!); await File.WriteAllTextAsync(_cache, JsonSerializer.Serialize(data)); return data;
    }
    public WeatherData? LoadCache() { try { return File.Exists(_cache) ? JsonSerializer.Deserialize<WeatherData>(File.ReadAllText(_cache)) : null; } catch { return null; } }
    public static string Describe(int code) => code switch { 0 => "Clear Sky", 1 or 2 => "Partly Cloudy", 3 => "Overcast", 45 or 48 => "Fog", >= 51 and <= 67 => "Rain", >= 71 and <= 77 => "Snow", >= 80 and <= 82 => "Rain Showers", >= 95 => "Thunderstorm", _ => "Cloudy" };
}
