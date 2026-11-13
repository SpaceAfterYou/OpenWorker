using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Hotspot;
using OpenWorker.Navigation;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// The ground of the district this server runs: the boxes of its map kept free of social objects, and the floor of its
/// navigation mesh. A district without a mesh checks no floor.
/// </summary>
public sealed partial class DistrictSocialItemGrounds(
    IOptions<InstanceOptions> instance,
    BatchManager batches,
    ReadOnlyCollection<DistrictRow> districts,
    NavMeshProvider meshes,
    ILogger<DistrictSocialItemGrounds> logger) : ISocialItemGrounds
{
    // How far above and below a point its floor may be.
    private const float FloorReach = 200f;

    private Task<SocialItemGround>? _ground;

    public async ValueTask<SocialItemGround> GetAsync()
    {
        return await (_ground ??= LoadAsync());
    }

    private async Task<SocialItemGround> LoadAsync()
    {
        var location = instance.Value.Location;

        var boxes = batches.TryGetAndCache(location, out var batch, BatchType.District)
            ? batch.EventBox.SocialItemExcludes
                .Select(e => ((e.PosTopLeft + e.PosBottomRight) / 2f, e.Size, e.Rotation))
                .ToArray()
            : [];

        if (!districts.IndexBy(e => e.Id).TryGet(location, out var row))
        {
            return new SocialItemGround(boxes);
        }

        var (mesh, error) = await meshes.GetAsync(row.Batch);

        if (mesh is null)
        {
            LogNoMesh(logger, location, error);

            return new SocialItemGround(boxes);
        }

        return new SocialItemGround(boxes, point => mesh.TryGetHeight(point, FloorReach, FloorReach, out _));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "District {Location} has no navigation mesh, social objects are put down without a floor check: {Error}")]
    private static partial void LogNoMesh(ILogger logger, short location, string? error);
}
