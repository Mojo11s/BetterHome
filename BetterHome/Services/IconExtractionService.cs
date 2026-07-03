using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BetterHome.Services;

public sealed class IconExtractionService
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO { public IntPtr hIcon; public int iIcon; public uint dwAttributes; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName; }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SHGetFileInfo(string path, uint attributes, out SHFILEINFO info, uint size, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    private const uint Icon = 0x100, LargeIcon = 0, UseAttributes = 0x10, DirectoryAttribute = 0x10;

    public ImageSource? GetIcon(string path, bool isDirectory = false)
    {
        try
        {
            var flags = Icon | LargeIcon;
            var attributes = isDirectory ? DirectoryAttribute : 0u;
            if (!File.Exists(path) && !Directory.Exists(path)) flags |= UseAttributes;
            if (SHGetFileInfo(path, attributes, out var info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags) == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;
            try
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
                source.Freeze(); return source;
            }
            finally { DestroyIcon(info.hIcon); }
        }
        catch { return null; }
    }
}
