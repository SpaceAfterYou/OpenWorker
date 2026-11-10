using Avalonia.Media.Imaging;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Loads GUI icons once by their table path (no <c>.png</c>), under the unpacked GUI root.
/// </summary>
internal sealed class GuiIconCache(string? guiFolder)
{
    private readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.Ordinal);

    public int Missing { get; private set; }

    public Bitmap? Get(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        if (_cache.TryGetValue(iconPath, out var cached))
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(guiFolder))
        {
            Missing++;
            _cache[iconPath] = null;
            return null;
        }

        var file = Path.Combine(
            guiFolder,
            iconPath.Replace('/', Path.DirectorySeparatorChar) + ".png");

        if (!File.Exists(file))
        {
            Missing++;
            _cache[iconPath] = null;
            return null;
        }

        var bitmap = new Bitmap(file);
        _cache[iconPath] = bitmap;
        return bitmap;
    }
}
