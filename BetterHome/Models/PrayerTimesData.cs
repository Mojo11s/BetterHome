namespace BetterHome.Models;
public sealed class PrayerTimesData
{
    public string Location { get; set; } = "Local location";
    public DateTime Date { get; set; }
    public TimeSpan Fajr { get; set; }
    public TimeSpan Dhuhr { get; set; }
    public TimeSpan Asr { get; set; }
    public TimeSpan Maghrib { get; set; }
    public TimeSpan Isha { get; set; }
}
