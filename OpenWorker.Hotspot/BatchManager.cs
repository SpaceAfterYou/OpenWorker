using System.Diagnostics.CodeAnalysis;
using OpenWorker.Batch;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot;

public enum BatchType
{
    None,
    Maze,
    District
}

internal readonly record struct BatchResult(VBatchFile File, BatchType Type);

public sealed class BatchManager(MazeResourceProvider mazes, DistrictResourceProvider districts)
{
    private Dictionary<short, BatchResult> Values { get; } = [];

    public bool TryGetAndCache(in WorldComponent world, [MaybeNullWhen(false)] out VBatchFile file, BatchType target)
    {
        return TryGetAndCache(world.Location, out file, out var type) && type == target;
    }
    
    public bool TryGetAndCache(in WorldComponent world, [MaybeNullWhen(false)] out VBatchFile file, out BatchType type)
    {
        return TryGetAndCache(world.Location, out file, out type);
    }
    
    public bool TryGetAndCache(in WorldComponent world, [MaybeNullWhen(false)] out VBatchFile file)
    {
        return TryGetAndCache(world.Location, out file, out _);
    }
    
    public bool TryGetAndCache(short world, [MaybeNullWhen(false)] out VBatchFile file)
    {
        return TryGetAndCache(world, out file, out _);
    }

    public bool TryGetAndCache(short world, [MaybeNullWhen(false)] out VBatchFile file, out BatchType type)
    {
        if (Values.TryGetValue(world, out var result))
        {
            file = result.File;
            type = result.Type;
            
            return true;
        }

        if (mazes.TryGetValue(world, out var value))
        {
            file = value;
            type = BatchType.Maze;

            Values.Add(world, new BatchResult(file, type));
            
            return true;
        }
        
        if (districts.TryGetValue(world, out value))
        {
            file = value;
            type = BatchType.District;

            Values.Add(world, new BatchResult(file, type));
            
            return true;
        }

        file = null;
        type = BatchType.None;
            
        return false;
    }
}
