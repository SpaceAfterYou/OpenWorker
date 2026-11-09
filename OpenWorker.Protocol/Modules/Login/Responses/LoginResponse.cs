using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Hotspot.Modules.Login.Extensions;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LoginResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.Result;

    private bool IsClearTutorial { get; }
    private string MacAddress { get; } = "00-00-00-00-00-00";
    private string ErrorMessage { get; } = string.Empty;
    private LoginErrorMessageCode ErrorCode { get; }
    private LoginType LoginType { get; }
    private string Login { get; } = string.Empty;
    private SessionValue Session { get; }

    public LoginResponse(SessionValue session, string login, bool isClearTutorial, string macAddress, LoginType loginType)
    {
        IsClearTutorial = isClearTutorial;
        MacAddress = macAddress;
        LoginType = loginType;
        Login = login;
        Session = session;
    }

    public LoginResponse(LoginErrorMessageCode errorCode)
    {
        ErrorCode = errorCode;
    }

    public LoginResponse(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Session.Account);

        writer.Write(IsClearTutorial);
        writer.WriteAsciiString(MacAddress);

        writer.WriteUtf16UnicodeString(ErrorMessage, 1025);
        writer.Write(ErrorCode);

        writer.Write(LoginType);
        writer.WriteUtf16UnicodeString(Login, 21);

        writer.Write(Session);
    }
}