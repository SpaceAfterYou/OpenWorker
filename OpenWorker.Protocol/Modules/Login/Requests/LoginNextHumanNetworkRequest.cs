using System.Text;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

/// <summary>
/// A login through the launcher of the publisher: four ASCII texts, each in a block of a fixed size that ends at its first
/// zero byte. The generator reads a fixed text whole, zeros included, so the reader is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct LoginNextHumanNetworkRequest : IRequestHotspotMessage, IWireReadable<LoginNextHumanNetworkRequest>
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.FromNextHumanNetwork;

    private const int TicketSize = 2048;
    private const int GameInfoSize = 4096;
    private const int UserIdSize = 21;
    private const int MacAddressSize = 18;

    public LoginNextHumanNetworkRequest(BinaryReader reader)
    {
        this = WireBridge.Read<LoginNextHumanNetworkRequest>(reader);
    }

    public string PurpleTicket { get; init; }
    public string PurpleGameInfo { get; init; }
    public string PurpleUserId { get; init; }
    public string MacAddress { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public static LoginNextHumanNetworkRequest Read(ref PacketReader reader)
    {
        return new LoginNextHumanNetworkRequest
        {
            PurpleTicket = ReadFixedString(ref reader, TicketSize),
            PurpleGameInfo = ReadFixedString(ref reader, GameInfoSize),
            PurpleUserId = ReadFixedString(ref reader, UserIdSize),
            MacAddress = ReadFixedString(ref reader, MacAddressSize)
        };
    }

    private static string ReadFixedString(ref PacketReader reader, int size)
    {
        var bytes = reader.ReadBytes(size);
        var length = bytes.IndexOf((byte)0);

        return Encoding.ASCII.GetString(length < 0 ? bytes : bytes[..length]);
    }
}
