using System.Text;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LoginNextHumanNetworkRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.FromNextHumanNetwork;

    public string PurpleTicket { get; } = ReadFixedString(reader, 2048);
    public string PurpleGameInfo { get; } = ReadFixedString(reader, 4096);
    public string PurpleUserId { get; } = ReadFixedString(reader, 21);
    public string MacAddress { get; } = ReadFixedString(reader, 18);

    public MessageOpcode Opcode => new(Group, Command);

    private static string ReadFixedString(BinaryReader reader, int size)
    {
        var bytes = reader.ReadBytes(size).AsSpan();
        var length = bytes.IndexOf((byte)0);

        return Encoding.ASCII.GetString(length < 0 ? bytes : bytes[..length]);
    }
}