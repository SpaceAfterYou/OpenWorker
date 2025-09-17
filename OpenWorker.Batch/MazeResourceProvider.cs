using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Batch;

public sealed class MazeResourceProvider(ReadOnlyCollection<MazeInfoRow> resources)
{
    public bool TryGetValue(short world, [NotNullWhen(true)] out VBatchFile? file)
    {
        var resource = resources.FirstOrDefault(e => e.Id == world);
        if (resource.Id is 0)
        {
            file = null;
            return false;
        }

        file = VBatchFile.Create(resource.Batch);
        return true;
    }
}