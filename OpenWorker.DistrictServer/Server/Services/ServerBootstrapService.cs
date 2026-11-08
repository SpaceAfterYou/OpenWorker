using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.DistrictServer.Server.Services;

/// <summary>
/// Tells the other servers that this district runs, where it is and where a player enters it.
/// A district that cannot say so cannot serve anybody, so the host does not start. The record has no lifetime:
/// when the server drains it is removed, and no player is sent to a district that is going away.
/// </summary>
public sealed partial class ServerBootstrapService(
    IRedisCollection<DistrictCache> cache,
    BatchManager manager,
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate,
    ILogger<ServerBootstrapService> logger
) :
    RegistrationService
{
    private DistrictCache? _record;

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
            throw Fail($"There is no batch of a district for the location {Location}.");
        }

        if (batch.EventBox.StartEvents.Count == 0)
        {
            throw Fail($"The batch of the location {Location} has no start event, so nobody could enter this district.");
        }

        var start = batch.EventBox.StartEvents[0];

        var position = start.GetPosition();

        var record = new DistrictCache
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
        };

        await cache.InsertAsync(record);

        _record = record;
    }

    protected override async Task RemoveAsync(CancellationToken cancellationToken)
    {
        if (_record is not { } record)
        {
            return;
        }

        try
        {
            await cache.DeleteAsync(record);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogRemoveFailed(e);
        }
    }

    private InvalidOperationException Fail(string reason)
    {
        LogCannotStart(Instance, Location, reason);

        return new InvalidOperationException(reason);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the record of the district from Redis.")]
    private partial void LogRemoveFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "District {Instance} of the location {Location} cannot start: {Reason}")]
    private partial void LogCannotStart(Guid instance, short location, string reason);
}
