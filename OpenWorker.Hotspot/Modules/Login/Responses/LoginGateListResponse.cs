using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command)]
public readonly struct LoginGateListResponse(byte previous, GateDataInfo[] values) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.ServerList;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        Debug.Assert(values.Length < byte.MaxValue);

        // TODO: Previous gate? Error state?
        //       Unused in client
        writer.Write(previous);
        
        writer.Write((byte)values.Length);

        foreach (var value in values)
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
}