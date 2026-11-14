using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Channel;

/// <summary>
/// The player was told to change servers. The default value is a player that was not.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct ServerChangeComponent
{
    public bool Told { get; internal set; }

    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp"/> of when it was told.
    /// </summary>
    public long At { get; internal set; }
}

/// <summary>
/// A player told to change servers is on its way out: until it has gone or its reservation has ended, a way out, a move to
/// another map or a maze entry it asks for is dropped without an answer, so that nothing is reserved for it twice. The client
/// sends such a request again by itself (the end of the tutorial does), so that costs it nothing.
/// </summary>
public sealed class ServerChanges(World ecs, TimeProvider time)
{
    /// <summary>
    /// The player has been told where to go.
    /// </summary>
    public void Begin(Entity player)
    {
        if (!ecs.IsAlive(player))
        {
            return;
        }

        var told = new ServerChangeComponent { Told = true, At = time.GetTimestamp() };

        if (ecs.Has<ServerChangeComponent>(player))
        {
            ecs.Set(player, told);
        }
        else
        {
            ecs.Add(player, told);
        }
    }

    /// <summary>
    /// Whether the player is on its way to another server and its reservation still holds.
    /// </summary>
    public bool IsLeaving(Entity player)
    {
        return ecs.IsAlive(player) &&
               ecs.TryGet(player, out ServerChangeComponent change) &&
               change.Told &&
               time.GetElapsedTime(change.At) < WorldManager.ReservationLifetime;
    }
}
