using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.Modules.Persons.Requests;

[HotspotMessage(Group, Command)]
public readonly struct PersonSecondPasswordRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.SecondPassword;

    public PasswordCheckType CheckType { get; } = reader.ReadPasswordCheckType();
    public string Password { get; } = reader.ReadUtf8AsciiStringWithoutTerminator();
    
    public MessageOpcode Opcode => new(Group, Command);
}