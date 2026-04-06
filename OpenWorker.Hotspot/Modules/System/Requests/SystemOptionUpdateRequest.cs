using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Types;

namespace OpenWorker.Hotspot.Modules.System.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SystemOptionUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.OptionUpdate;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyCollection<byte> OptionList { get; } = reader.ReadBytes(SystemModuleDefines.OptionCount);

#endregion Message: Body
}
