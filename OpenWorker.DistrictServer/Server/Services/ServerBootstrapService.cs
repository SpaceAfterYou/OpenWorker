using System.Diagnostics;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.DistrictServer.Server.Services;

public sealed class ServerBootstrapService(
    IRedisCollection<DistrictCache> cache,
    BatchManager manager,
    IConfiguration configuration
) :
    BackgroundService
{
    private short Gate { get; } = configuration.GetGate();
    private short Location { get; } = configuration.GetLocation();
    private short Group { get; } = configuration.GetGroup();
    private Guid Instance { get; } = configuration.GetInstance();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!manager.TryGetAndCache(Location, out var batch, out var type) || type != BatchType.District)
        {
            return;
        }
        
        Debug.Assert(batch.EventBox.StartEvents.Count > 0);
        var start = batch.EventBox.StartEvents[0];

        var position = start.GetPosition();

        await cache.InsertAsync(new DistrictCache
        {
            Guid = Instance,

            Gate = Gate,
            Location = Location,
            Group = Group,

            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,

            Jump = start.Id,
            Portal = start.Id,

            Host = "127.0.0.1",
            Port = GetPort(),

            Rotation = start.Rotation
        }).ConfigureAwait(false);
    }
    
    private short GetPort() => Location switch
    {
        10003 => 10011,
        10021 => 10012,
        10031 => 10013,
        10041 => 10014,

        _ => throw new NotImplementedException()
    };
}