using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct LoginAuthRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.Req;

    [WireString(WireStringFormat.Utf8Unicode)]
    public string Username { get; }

    [WireString(WireStringFormat.Utf8Unicode)]
    public string Password { get; }

    // %02X-%02X-%02X-%02X-%02X-%02X
    [WireString(WireStringFormat.Utf8Unicode)]
    public string MacAddress { get; }

    public LoginAuthRequest(string username, string password, string macAddress) : this()
    {
        Username = username;
        Password = password;
        MacAddress = macAddress;
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
