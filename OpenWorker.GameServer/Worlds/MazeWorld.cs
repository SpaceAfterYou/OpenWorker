using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.GameServer.Worlds.Abstractions;

namespace OpenWorker.GameServer.Worlds;

internal interface IMazeRoomEvent
{
    ValueTask DoAsync(CancellationToken cancellationToken);
}

internal sealed class MazeRoomComponent(MazeRoom room)
{
    public readonly List<IMazeRoomEvent> Events = room.Events;
}

internal sealed class MazeRoom
{
    private List<Entity> Players { get; } = [];
    public List<IMazeRoomEvent> Events { get; } = [];

    public async Task TickAsync(double deltaTime, CancellationToken cancellationToken)
    {
        foreach (var @event in Events)
        {
            await @event.DoAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed class MazeWorld(ILogger<MazeWorld> logger) : IMazeWorld
{
    private Dictionary<Guid, MazeRoom> Rooms { get; } = [];

    public int TickRate => 1;

    internal MazeRoom GetOrCreateRoom(Guid id) => Rooms.GetValueOrDefault(id, new MazeRoom());

    public async Task TickAsync(double deltaTime, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ticking maze world.");

        await Task.WhenAll(Rooms.Values.Select(room => room.TickAsync(deltaTime, cancellationToken))).ConfigureAwait(false);
    }
}