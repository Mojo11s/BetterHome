using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using BetterHome.Models;

namespace BetterHome.Services;

public sealed class PrayerTimesService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly string _cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterHome", "prayer-cache.json");
    public async Task<PrayerTimesData> GetAsync(string location, string calculationMethod)
    {
        var parts = location.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries); var city = parts.ElementAtOrDefault(0) ?? "Cairo"; var country = parts.ElementAtOrDefault(1) ?? "Egypt";
        var method = calculationMethod switch { "Muslim World League" => 3, "Umm Al-Qura" => 4, _ => 5 };
        var date = DateTime.Today; var url = $"https://api.aladhan.com/v1/timingsByCity/{date:dd-MM-yyyy}?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method={method}";
        using var document = JsonDocument.Parse(await Client.GetStringAsync(url)); var timings = document.RootElement.GetProperty("data").GetProperty("timings");
        var data = new PrayerTimesData { Location = $"{city}, {country}", Date = date, Fajr = Parse(timings, "Fajr"), Dhuhr = Parse(timings, "Dhuhr"), Asr = Parse(timings, "Asr"), Maghrib = Parse(timings, "Maghrib"), Isha = Parse(timings, "Isha") };
        Directory.CreateDirectory(Path.GetDirectoryName(_cache)!); await File.WriteAllTextAsync(_cache, JsonSerializer.Serialize(data)); return data;
    }
    public PrayerTimesData? LoadCache() { try { var data = File.Exists(_cache) ? JsonSerializer.Deserialize<PrayerTimesData>(File.ReadAllText(_cache)) : null; return data?.Date.Date == DateTime.Today ? data : null; } catch { return null; } }
    private static TimeSpan Parse(JsonElement timings, string name) { var value = timings.GetProperty(name).GetString() ?? "00:00"; return TimeSpan.TryParse(value[..Math.Min(5, value.Length)], out var time) ? time : TimeSpan.Zero; }
}
