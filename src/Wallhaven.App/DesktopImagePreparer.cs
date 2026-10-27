using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Wallhaven.Desktop;

internal static class DesktopImagePreparer
{
    public static Task<string> PrepareAsync(string source, string dataRoot, CancellationToken ct = default) => Task.Run(async () =>
    {
        ct.ThrowIfCancellationRequested();
        string directory = Path.Combine(dataRoot, "desktop");
        Directory.CreateDirectory(directory);
        byte[] hash;
        await using (var input = File.OpenRead(source)) hash = await SHA256.HashDataAsync(input, ct);
        // Windows can hold the active BMP open. Never overwrite or rename onto it.
        string output = Path.Combine(directory, Convert.ToHexString(hash).ToLowerInvariant() + ".bmp");
        if (File.Exists(output)) return output;
        string temp = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".part");
        try
        {
            var decoder = BitmapDecoder.Create(new Uri(source), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var encoder = new BmpBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));
            using (var target = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) encoder.Save(target);
            ct.ThrowIfCancellationRequested();
            try { File.Move(temp, output, false); }
            catch (IOException) when (File.Exists(output)) { /* Another preparation completed first. */ }
            return output;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }, ct);
}
