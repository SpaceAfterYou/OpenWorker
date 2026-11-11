using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// The opcodes the bot sends and listens for, as the 16-bit value of <c>MessageOpcode</c>: the group in the low
/// byte and the command in the high byte, which is how the two bytes lie on the wire. They are built from the
/// enumerations of the server, so a renumbered command moves them with it; the tests compare them to the opcodes
/// of the server's own message types.
/// </summary>
internal static class Opcodes
{
    public static ushort Group(ushort opcode) => (ushort)(opcode & 0xFF);

    public static ushort Command(ushort opcode) => (ushort)(opcode >> 8);

    /// <summary>
    /// The bit the server sets in the command of a failed request: the answer then carries a code instead of the result.
    /// </summary>
    public const byte ErrorFlag = 0x80;

    public static ushort ErrorOf(ushort opcode) => (ushort)(opcode | (ErrorFlag << 8));

    // System.
    public const ushort KeepAlive = (ushort)((byte)GroupOpcode.System | ((byte)SystemOpcode.KeepAlive << 8));
    public const ushort Ping = (ushort)((byte)GroupOpcode.System | ((byte)SystemOpcode.Ping << 8));

    // Login, on the auth server and at the start of the gate.
    public const ushort LoginRequest = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.Req << 8));
    public const ushort LoginResult = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.Result << 8));
    public const ushort GateListRequest = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.ServerListReq << 8));
    public const ushort GateList = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.ServerList << 8));
    public const ushort GateConnectRequest = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.ServerConnectReq << 8));
    public const ushort GateConnect = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.EnterServer << 8));
    public const ushort EnterGateRequest = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.EnterServerReq << 8));
    public const ushort EnterGate = (ushort)((byte)GroupOpcode.Login | ((byte)LoginOpcode.EnterServerRes << 8));

    // Characters.
    public const ushort PersonCreateRequest = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.CreateReq << 8));
    public const ushort PersonKickOut = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.KickOut << 8));
    public const ushort PersonListRequest = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.ListReq << 8));
    public const ushort PersonList = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.ListRes << 8));
    public const ushort PersonSelectRequest = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.SelectReq << 8));
    public const ushort PersonSelect = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.SelectRes << 8));
    public const ushort PersonEnterRequest = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.EnterGameServerReq << 8));
    public const ushort PersonInfo = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.InfoRes << 8));
    public const ushort PersonEnterAnswer = (ushort)((byte)GroupOpcode.Character | ((byte)CharacterOpcode.EnterGameServerRes << 8));

    // World.
    public const ushort WorldEnterRequest = (ushort)((byte)GroupOpcode.World | ((byte)WorldOpcode.EnterReq << 8));
    public const ushort WorldEnter = (ushort)((byte)GroupOpcode.World | ((byte)WorldOpcode.EnterRes << 8));
    public const ushort WorldObjectRequest = (ushort)((byte)GroupOpcode.World | ((byte)WorldOpcode.ObjectReq << 8));
    public const ushort MazeCreateRequest = (ushort)((byte)GroupOpcode.World | ((byte)WorldOpcode.MazeCreateReq << 8));

    // Movement.
    public const ushort MoveRequest = (ushort)((byte)GroupOpcode.Move | ((byte)MoveOpcode.Move << 8));
    public const ushort MoveBroadcast = (ushort)((byte)GroupOpcode.Move | ((byte)MoveOpcode.MoveBt << 8));

    // Skills.
    public const ushort SkillActiveRequest = (ushort)((byte)GroupOpcode.Skill | ((byte)SkillOpcode.ActiveSkillReq << 8));
    public const ushort SkillActive = (ushort)((byte)GroupOpcode.Skill | ((byte)SkillOpcode.ActiveSkillRes << 8));
    public const ushort SkillLoadInfo = (ushort)((byte)GroupOpcode.Skill | ((byte)SkillOpcode.SkillLoadInfo << 8));

    // Chat.
    public const ushort ChatNormal = (ushort)((byte)GroupOpcode.Chat | ((byte)ChatOpcode.Normal << 8));
    public const ushort ChatCommand = (ushort)((byte)GroupOpcode.Chat | ((byte)ChatOpcode.GmCommand << 8));

    // Maze.
    public const ushort MazeCompleteRequest = (ushort)((byte)GroupOpcode.Maze | ((byte)MazeOpcode.eSUB_CMD_COMPLETE_MAZE_REQ << 8));

    // Channels.
    public const ushort ChannelChangeRequest = (ushort)((byte)GroupOpcode.Channel | ((byte)ChannelOpcode.Change << 8));
}
