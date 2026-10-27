using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Wallhaven.Desktop;

/// <summary>Small LRU of frozen preview bitmaps, invalidated when the disk file changes.</summary>
internal sealed class BitmapMemoryCache
{
    private readonly long budget;
    public BitmapMemoryCache() : this(96L * 1024 * 1024) { }
    public BitmapMemoryCache(long budget) => this.budget = budget > 0 ? budget : throw new ArgumentOutOfRangeException(nameof(budget));
    private readonly object sync = new();
    private readonly Dictionary<(string Path, int Width), Entry> entries = new();
    private long usedBytes, access, decodeCount;
    public long DecodeCount => Interlocked.Read(ref decodeCount);
    public long CachedBytes { get { lock (sync) return usedBytes; } }
    private readonly record struct Stamp(long Length, long Modified, long Created)
    {
        public static Stamp Read(string path)
        {
            var file = new FileInfo(path);
            return new(file.Length, file.LastWriteTimeUtc.Ticks, file.CreationTimeUtc.Ticks);
        }
    }
    private sealed record Entry(Stamp Stamp, BitmapImage Bitmap, long Bytes, long Access);
    private static (string, int) Key(string path, int width) => (Path.GetFullPath(path).ToUpperInvariant(), width);

    public Task<BitmapImage> LoadAsync(string path, int width)
    {
        var stamp = Stamp.Read(path);
        var bitmap = Find(path, width, stamp);
        return bitmap != null ? Task.FromResult(bitmap) : Task.Run(() => Decode(path, width));
    }
    public BitmapImage? TryGet(string path, int width) => File.Exists(path) ? Find(path, width, Stamp.Read(path)) : null;
    public void Validate(string path)
    {
        try { Decode(path, Path.GetFileName(path).StartsWith("thumb-", StringComparison.OrdinalIgnoreCase) ? 400 : 2560); }
        catch (Exception ex) when (ex is IOException or FormatException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException)
        { throw new InvalidDataException("壁纸图片无法解码。", ex); }
    }
    public void FileCommitted(string source, string target)
    {
        var stamp = Stamp.Read(target);
        lock (sync)
        {
            foreach (int width in new[] { 400, 2560 })
            {
                if (!entries.Remove(Key(source, width), out var entry)) continue;
                if (entries.Remove(Key(target, width), out var previous)) usedBytes -= previous.Bytes;
                entries[Key(target, width)] = entry with { Stamp = stamp, Access = ++access };
            }
        }
    }
    private BitmapImage? Find(string path, int width, Stamp stamp)
    {
        lock (sync)
        {
            var key = Key(path, width);
            if (!entries.TryGetValue(key, out var entry)) return null;
            if (entry.Stamp != stamp) { entries.Remove(key); usedBytes -= entry.Bytes; return null; }
            entries[key] = entry with { Access = ++access };
            return entry.Bitmap;
        }
    }
    private BitmapImage Decode(string path, int width)
    {
        var stamp = Stamp.Read(path);
        if (Find(path, width, stamp) is { } cached) return cached;
        using var input = File.OpenRead(path);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = width;
        bitmap.StreamSource = input;
        bitmap.EndInit();
        bitmap.Freeze();
        if (Stamp.Read(path) != stamp) throw new IOException("图片在解码过程中发生变化，请重试。");
        Interlocked.Increment(ref decodeCount);
        long bytes = (long)bitmap.PixelWidth * bitmap.PixelHeight * Math.Max(4, (bitmap.Format.BitsPerPixel + 7) / 8);
        if (bytes > budget) return bitmap;
        lock (sync)
        {
            var key = Key(path, width);
            if (entries.Remove(key, out var old)) usedBytes -= old.Bytes;
            while (usedBytes + bytes > budget && entries.Count > 0)
            {
                (string Path, int Width) oldest = default;
                long oldestAccess = long.MaxValue;
                foreach (var pair in entries) if (pair.Value.Access < oldestAccess) { oldestAccess = pair.Value.Access; oldest = pair.Key; }
                usedBytes -= entries[oldest].Bytes;
                entries.Remove(oldest);
            }
            entries[key] = new(stamp, bitmap, bytes, ++access);
            usedBytes += bytes;
        }
        return bitmap;
    }
}
