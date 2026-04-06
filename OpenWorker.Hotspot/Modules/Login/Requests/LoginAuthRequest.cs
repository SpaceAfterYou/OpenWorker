using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LoginAuthRequest() : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.Req;

    public string Username { get; }
    public string Password { get; }
    
    // %02X-%02X-%02X-%02X-%02X-%02X
    public string MacAddress { get; }
    
    public LoginAuthRequest(BinaryReader reader) : this()
    {
        Username = reader.ReadUtf8UnicodeString();
        Password = reader.ReadUtf8UnicodeString();
        MacAddress = reader.ReadUtf8UnicodeString();
    }
    
    public LoginAuthRequest(string username, string password, string macAddress) : this()
    {
        Username = username;
        Password = password;
        MacAddress = macAddress;
    }
    
    public MessageOpcode Opcode => new(Group, Command);

}