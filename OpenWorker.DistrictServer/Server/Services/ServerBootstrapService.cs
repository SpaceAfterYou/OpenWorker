using System.Diagnostics;
using Microsoft.Extensions.Options;
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
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate
) :
    BackgroundService
{
    private short Gate { get; } = gate.Value.Id;
    private short Location { get; } = instance.Value.Location;
    private short Group { get; } = instance.Value.Group;
    private Guid Instance { get; } = instance.Value.Identifier;
    private string Host { get; } = instance.Value.PublicHost;

    /// <summary>
    /// The port clients reach this district at: every district listens behind its own, so there is no default.
    /// </summary>
    private short Port { get; } = checked((short)(instance.Value.PublicPort
        ?? throw new InvalidOperationException("Instance:PublicPort must be set to the port clients reach this district at.")));

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

            Host = Host,
            Port = Port,

            Rotation = start.Rotation
        });
    }
}
