using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Batch;

public sealed class MazeResourceProvider(ReadOnlyCollection<MazeInfoRow> resources)
{
    private readonly RowIndex<short, MazeInfoRow> _rows = resources.IndexBy(e => e.Id);

    public bool TryGetValue(short world, [NotNullWhen(true)] out VBatchFile? file)
    {
        if (!_rows.TryGet(world, out var resource) || resource.Id is 0)
        {
            file = null;
            return false;
        }

        return VBatchFile.TryCreate(resource.Batch, out file);
    }
}