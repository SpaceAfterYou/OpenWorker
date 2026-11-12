using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LoginOptionUpdateRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.OptionUpdate;

    [WireArray(Count = LoginModuleDefines.PersonOptionCount)]
    public IReadOnlyCollection<byte> Values { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
