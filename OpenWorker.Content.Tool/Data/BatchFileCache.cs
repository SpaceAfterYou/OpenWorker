using OpenWorker.Batch;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Keeps parsed <c>*.vbatch</c> files so a second look at the same map does not read the XML again.
/// </summary>
internal sealed class BatchFileCache
{
    public static BatchFileCache Shared { get; } = new();

    private readonly object _gate = new();
    private readonly Dictionary<string, VBatchFile> _files = new(StringComparer.Ordinal);

    public VBatchFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var key = Path.GetFullPath(path);

        lock (_gate)
        {
            if (_files.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var file = VBatchFile.CreateFromPath(key);
            _files[key] = file;
            return file;
        }
    }
}
