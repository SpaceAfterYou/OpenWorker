using System.Numerics;
using Arch.Core;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Server.Common;

/// <summary>
/// Who hears what a player does: the one thing the handlers of this assembly leave to the server. A district tells the
/// players of the channel (the chat) or those who see the player (everything else); a maze, where every player has a maze
/// of their own for now, tells the player alone. A server registers its audience with <c>AddServerCommon</c>.
/// </summary>
public interface IPlayerAudience
{
    /// <summary>
    /// What the player says aloud in the chat, to everybody who hears it, the player too.
    /// </summary>
    void Chat(Entity player, IResponseHotspotMessage message);

    /// <summary>
    /// To everybody who sees the player, the player too.
    /// </summary>
    void Nearby<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable;

    /// <summary>
    /// To everybody who sees the player, but not the player.
    /// </summary>
    void NearbyToOthers<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable;

    /// <inheritdoc cref="NearbyToOthers{T}(Entity, in T)" />
    void NearbyToOthers(Entity player, IResponseHotspotMessage message);

    /// <summary>
    /// A move of the player, which a later move may replace before it goes out.
    /// </summary>
    void Move(Entity player, in MovementMoveBtResponse message);

    /// <summary>
    /// The place of the player has changed: where the others see it from may have too.
    /// </summary>
    void Relocate(Entity player, Vector3 position);
}
