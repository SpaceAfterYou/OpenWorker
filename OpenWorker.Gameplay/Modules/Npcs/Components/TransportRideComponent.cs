using System.Numerics;

namespace OpenWorker.Gameplay.Modules.Npcs.Components;

/// <summary>
/// A player on a ride of an NPC of transports: added when it pays, removed when the ride is over (it is not one of the
/// components every player is made with, so having it means riding). The server alone ends a ride, as the client has no
/// request for it, so the component also says where the ride ends.
/// </summary>
public struct TransportRideComponent
{
    /// <summary>
    /// The row of the ride in <c>tb_Transport_Info</c>.
    /// </summary>
    public short Row;

    /// <summary>
    /// Where the player stands when the ride is over: the middle of the box the ride ends at.
    /// </summary>
    public Vector3 Destination;

    /// <summary>
    /// The way the player faces there.
    /// </summary>
    public float Rotation;
}
