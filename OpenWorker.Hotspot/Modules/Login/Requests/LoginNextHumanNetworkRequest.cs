using System.Text;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command)]
public readonly struct LoginNextHumanNetworkRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.FromNextHumanNetwork;

    public string PurpleTicket { get; } = Encoding.ASCII.GetString(reader.ReadBytes(2048));
    public string PurpleGameInfo { get; } = Encoding.ASCII.GetString(reader.ReadBytes(4096));
    public string PurpleUserId { get; } = Encoding.ASCII.GetString(reader.ReadBytes(21));
    public string MacAddress { get; } = Encoding.ASCII.GetString(reader.ReadBytes(18));

    public MessageOpcode Opcode => new(Group, Command);
}