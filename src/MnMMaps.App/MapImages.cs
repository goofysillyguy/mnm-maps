using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MnMMaps;

public sealed class MapImages : IDisposable
{
    private const int MaximumBytes = 40 * 1024 * 1024;
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(35) };

    public MapImages() => client.DefaultRequestHeaders.UserAgent.ParseAdd("MnMMaps/0.1 (standalone-community-map-viewer)");

    public async Task<BitmapSource> LoadAsync(MapEntry map, MapStore store, bool refresh, CancellationToken cancellation)
    {
        if (map.LocalFile is not null) return await Task.Run(() => Read(map.LocalFile), cancellation).ConfigureAwait(false);
        var path = store.CachedImagePath(map);
        if (!refresh && File.Exists(path))
        {
            try { return await Task.Run(() => Read(path), cancellation).ConfigureAwait(false); }
            catch (Exception exception) when (exception is IOException or NotSupportedException or FileFormatException or ArgumentException) { }
        }

        if (!Uri.TryCreate(map.ImageUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "static.wikitide.net")
            throw new InvalidOperationException("This catalog entry does not point to the wiki's image server.");

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumBytes) throw new IOException("The image is larger than 40 MB.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        using var data = new MemoryStream();
        var buffer = new byte[65536];
        int count;
        while ((count = await input.ReadAsync(buffer, cancellation).ConfigureAwait(false)) != 0)
        {
            if (data.Length + count > MaximumBytes) throw new IOException("The image is larger than 40 MB.");
            await data.WriteAsync(buffer.AsMemory(0, count), cancellation).ConfigureAwait(false);
        }
        var bytes = data.ToArray();
        var bitmap = await Task.Run(() => Decode(bytes), cancellation).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return bitmap;
    }

    public static BitmapSource Read(string path)
    {
        if (new FileInfo(path).Length > MaximumBytes) throw new IOException("The image is larger than 40 MB.");
        return Decode(File.ReadAllBytes(path));
    }

    private static BitmapSource Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        if ((long)frame.PixelWidth * frame.PixelHeight > 100_000_000)
            throw new IOException("This image is too large to display comfortably. Import a smaller version.");
        frame.Freeze();
        return frame;
    }

    public void Dispose() => client.Dispose();
}
