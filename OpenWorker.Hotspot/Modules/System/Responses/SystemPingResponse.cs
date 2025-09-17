using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.System.Responses;

[HotspotMessage(Group, Command)]
public readonly struct SystemPingResponse(long keepAlive) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.Ping;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        // In the client: GetTickCount64() - keepAlive = ping in ms
        writer.Write(keepAlive);
    }
}