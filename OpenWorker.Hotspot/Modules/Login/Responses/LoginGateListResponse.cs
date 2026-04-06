using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LoginGateListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.ServerList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte Previous { get; init; }
    public required GateDataInfo[] Values { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Values.Length < byte.MaxValue);

        // TODO: Previous gate? Error state?
        //       Unused in client
        writer.Write(Previous);

        writer.Write((byte)Values.Length);

        foreach (var value in Values)
        {
            writer.Write(value.Gate.Id);
            writer.Write(value.Gate.Port);
            writer.WriteUtf8AsciiString(value.Gate.Name);
            writer.WriteUtf8AsciiString(value.Gate.Address);
            writer.Write(value.Gate.Workload);
            writer.Write(value.Gate.OnlineCount);
            writer.Write(value.Person.Count);
        }
    }

#endregion Interface: IWritableData
}
