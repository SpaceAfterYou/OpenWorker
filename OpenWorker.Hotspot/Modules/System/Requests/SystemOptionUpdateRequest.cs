using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Types;

namespace OpenWorker.Hotspot.Modules.System.Requests;

[HotspotMessage(Group, Command)]
public readonly struct SystemOptionUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.OptionUpdate;

    public IReadOnlyCollection<byte> OptionList { get; } = reader.ReadBytes(SystemModuleDefines.OptionCount);

    public MessageOpcode Opcode => new(Group, Command);
}