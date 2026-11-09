using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Types;

namespace OpenWorker.Hotspot.Modules.Helpers.Responses;

/// <summary>
/// A helper is called or sent away; the actor is the one the helper became in the world.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct HelperSummonResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.Summon;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Person { get; init; }

    public int Actor { get; init; }

    public byte Summon { get; init; }

    public required HelperValue Helper { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Person);
        writer.Write(Actor);
        writer.Write(Summon);
        Helper.Write(writer);
    }

#endregion Interface: IWritableData
}
