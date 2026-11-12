using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

/// <summary>
/// The answer to a login: the account of the session first, the session itself last. The one value goes twice, so the
/// writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LoginResponse : IResponseHotspotMessage, IWireWritable
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

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Session.Account);

        writer.WriteBoolean(IsClearTutorial);
        writer.WriteAsciiString(MacAddress);

        writer.WriteUtf16UnicodeString(ErrorMessage);
        writer.WriteInt32((int)ErrorCode);

        writer.WriteByte((byte)LoginType);
        writer.WriteUtf16UnicodeString(Login);

        Session.Write(ref writer);
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
