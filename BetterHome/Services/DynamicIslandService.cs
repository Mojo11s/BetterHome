namespace BetterHome.Services;
public sealed class DynamicIslandService
{
    public event Action<string>? StatusChanged;
    public void Publish(string status) => StatusChanged?.Invoke(status);
}
