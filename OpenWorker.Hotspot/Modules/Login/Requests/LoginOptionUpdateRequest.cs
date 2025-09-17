using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command)]
public readonly struct LoginOptionUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.OptionUpdate;

    public IReadOnlyCollection<byte> Values { get; } = reader.ReadBytes(LoginModuleDefines.PersonOptionCount);

    public MessageOpcode Opcode => new(Group, Command);
}