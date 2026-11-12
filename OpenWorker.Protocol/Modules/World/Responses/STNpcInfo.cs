using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

/// <summary>
/// The part of the info of a monster or an NPC that every creature has. Of the place only the position and the rotation go
/// on the wire, not the map, so the writer is written by hand.
/// </summary>
public readonly struct STNpcInfo : IWireWritable
{
    public required ActorValue uxActorID { get; init; }
    public required WorldValue stPosInfo { get; init; }
    public required int nHP { get; init; }
    public required int nWayPointID { get; init; }
    public required int nSectorID { get; init; }
    public required byte byLevel { get; init; }
    public required int nTableID { get; init; }

    public void Write(ref PacketWriter writer)
    {
        uxActorID.Write(ref writer);
        writer.WriteVector3(stPosInfo.Position);
        writer.WriteSingle(stPosInfo.Rotation);
        writer.WriteInt32(nHP);
        writer.WriteInt32(nWayPointID);
        writer.WriteInt32(nSectorID);
        writer.WriteByte(byLevel);
        writer.WriteInt32(nTableID);
    }
}
