using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Helpers.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct HelperSupportRewardRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.SupportReward;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage
}
