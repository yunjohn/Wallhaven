using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wallhaven.Desktop;

internal static class DesktopIdentityResolver
{
    public static Task<string?> FindCachedSourceAsync(string desktopPath, string directory, CancellationToken ct) => Task.Run(async () =>
    {
        var candidates = Directory.EnumerateFiles(directory, "wallhaven-*")
            .Where(path => !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc).Take(150).ToArray();
        string stem = Path.GetFileNameWithoutExtension(desktopPath);
        bool contentNamed = stem.Length == 64 && stem.All(Uri.IsHexDigit);
        if (contentNamed)
        {
            foreach (string file in candidates)
            {
                ct.ThrowIfCancellationRequested();
                await using var input = File.OpenRead(file);
                string hash = Convert.ToHexString(await SHA256.HashDataAsync(input, ct));
                if (hash.Equals(stem, StringComparison.OrdinalIgnoreCase)) return file;
            }
            return null;
        }
        // Migration for older versions' shared desktop.bmp or Windows' transcoded file.
        byte[] signature;
        try { signature = Fingerprint(desktopPath); } catch { return null; }
        foreach (string file in candidates)
        {
            ct.ThrowIfCancellationRequested();
            if (string.Equals(file, desktopPath, StringComparison.OrdinalIgnoreCase)) return file;
            try { if (Fingerprint(file).SequenceEqual(signature)) return file; } catch { }
        }
        return null;
    }, ct);
    private static byte[] Fingerprint(string path)
    {
        var frame = new BitmapImage();
        frame.BeginInit(); frame.CacheOption = BitmapCacheOption.OnLoad; frame.DecodePixelWidth = 256;
        frame.UriSource = new Uri(path); frame.EndInit();
        var scaled = new TransformedBitmap(frame, new ScaleTransform(64d / frame.PixelWidth, 64d / frame.PixelHeight));
        var rgba = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[rgba.PixelWidth * rgba.PixelHeight * 4];
        rgba.CopyPixels(pixels, rgba.PixelWidth * 4, 0);
        return SHA256.HashData(pixels);
    }
}
