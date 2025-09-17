using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[HotspotMessage(Group, Command)]
public readonly struct FriendFindResponse(IReadOnlyCollection<FriendFindEntry> values) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Find;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        Debug.Assert(values.Count < short.MaxValue);
        writer.Write((short)values.Count);

        foreach (var value in values)
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
}