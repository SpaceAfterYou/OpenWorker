using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct FriendFindResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Find;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<FriendFindEntry> Values { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Values.Count < short.MaxValue);
        writer.Write((short)Values.Count);

        foreach (var value in Values)
        {
            writer.Write(value.Person);

            Debug.Assert(value.Name.Length <= 21);
            writer.WriteUtf16UnicodeString(value.Name);

            writer.Write(value.Level);
            writer.Write(value.Channel);
            writer.Write(value.World);
            writer.Write(value.IsLoggedIn);
        }
    }

#endregion Interface: IWritableData
}
