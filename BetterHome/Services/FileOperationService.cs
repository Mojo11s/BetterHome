using Microsoft.VisualBasic.FileIO;

namespace BetterHome.Services;

public sealed class FileOperationService
{
    public void Open(string path) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    public void OpenLocation(string path) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", File.Exists(path) ? $"/select,\"{path}\"" : $"\"{path}\"") { UseShellExecute = true });
    public void Properties(string path) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"(New-Object -ComObject Shell.Application).NameSpace((Split-Path -LiteralPath '{path.Replace("'", "''")}')).ParseName((Split-Path -Leaf -LiteralPath '{path.Replace("'", "''")}')).InvokeVerb('properties')\"") { UseShellExecute = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden });
    public string Rename(string path, string newName)
    {
        var parent = Path.GetDirectoryName(path) ?? throw new IOException("Parent folder not found."); var destination = Path.Combine(parent, newName);
        if (Directory.Exists(path)) Directory.Move(path, destination); else File.Move(path, destination); return destination;
    }
    public void DeleteToRecycleBin(string path)
    {
        if (Directory.Exists(path)) FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        else FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }
    public void Copy(string source, string destination)
    {
        if (Directory.Exists(source)) CopyDirectory(source, Path.Combine(destination, Path.GetFileName(source)));
        else File.Copy(source, Path.Combine(destination, Path.GetFileName(source)), true);
    }
    public void Move(string source, string destination)
    {
        var target = Path.Combine(destination, Path.GetFileName(source)); if (Directory.Exists(source)) Directory.Move(source, target); else File.Move(source, target, true);
    }
    private static void CopyDirectory(string source, string destination) { Directory.CreateDirectory(destination); foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true); foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir))); }
}
