using System.Numerics;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// The requests of a client, one method for each. The layouts are the ones the request types of the server read,
/// and the tests check them against those types and against the packets a real client sent.
/// </summary>
internal static class Requests
{
    /// <summary>
    /// The longest request the bot sends: a chat line of the longest allowed length, and with it the headroom a
    /// character creation takes. A buffer this size holds any frame the bot builds, and two of them.
    /// </summary>
    public const int MaxFrameSize = 1024;

    public const int MaxChatLength = 255;

    /// <summary>
    /// What the character creation of a real client sent, found in the packet dump of that request: the hero,
    /// the looks that the tables of the client offer for it, and the outfit. The creation checks them against
    /// those tables, so values the bot made up would be turned away.
    /// </summary>
    public static class Template
    {
        public const Hero Hero = Domain.Enums.Hero.Stella;

        public const short HairStyle = 1501;
        public const short HairColor = 2501;
        public const short EyeColor = 5501;
        public const short SkinColor = 3501;

        public const int Outfit = 511;
    }

    public static void Login(ref FrameWriter writer, string username, string password, string macAddress)
    {
        writer.Begin(Opcodes.LoginRequest);
        writer.WriteString(username);
        writer.WriteString(password);
        writer.WriteString(macAddress);
        writer.End();
    }

    public static void GateList(ref FrameWriter writer, int account)
    {
        writer.Begin(Opcodes.GateListRequest);
        writer.WriteInt32(account);
        writer.End();
    }

    public static void GateConnect(ref FrameWriter writer, short gate)
    {
        writer.Begin(Opcodes.GateConnectRequest);
        writer.WriteInt16(gate);
        writer.End();
    }

    public static void EnterGate(ref FrameWriter writer, int account, short gate, long session)
    {
        writer.Begin(Opcodes.EnterGateRequest);
        writer.WriteInt32(account);
        writer.WriteInt16(gate);
        writer.WriteInt64(session);
        writer.End();
    }

    public static void PersonList(ref FrameWriter writer, long session)
    {
        writer.Begin(Opcodes.PersonListRequest);
        writer.WriteInt64(session);
        writer.End();
    }

    /// <summary>
    /// The request to create a character: the looks of a real client's creation, under the given name.
    /// </summary>
    public static void PersonCreate(ref FrameWriter writer, string name)
    {
        // The value is written by the server's own writer, so a change of its layout reaches the bot with it.
        using var stream = new MemoryStream();
        using (var binary = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            binary.Write(TemplatePerson(name));
            binary.Write(Template.Outfit);
        }

        writer.Begin(Opcodes.PersonCreateRequest);
        writer.WriteBytes(stream.GetBuffer().AsSpan(0, (int)stream.Length));
        writer.End();
    }

    /// <summary>
    /// The character a new player makes, under the given name.
    /// </summary>
    public static PersonValue TemplatePerson(string name)
    {
        var equipped = new EquipItemValueEntry[ItemModuleDefines.EquippedItemCount];

        Array.Fill(equipped, new EquipItemValueEntry(-1));

        return new PersonValue(
            -1,
            new PersonInfoValue(
                name,
                Template.Hero,
                new AppearanceValue(Template.HairStyle, Template.HairColor, Template.EyeColor, Template.SkinColor),
                new AppearanceValue(0, 0, 0, 0)),
            account: 0,
            level: 1,
            FactionGroup.None,
            StuffLevel.None,
            pvpKillCount: 0,
            new EquipItemValueEntry(-1),
            new EquipItemValueEntry(-1),
            equipped,
            new TitleValue(0, 0),
            new LeagueValue(0, string.Empty, new PersonLeagueCardValueEntry(0, 0)),
            new AbilityValue(default, default, default, default, default, new SpeedValueEntry(1f, 1f)),
            new PrivateShopValue(0, string.Empty),
            new FatiguePointsValue(0, 0),
            new RankValue(0, 0),
            battlePose: false,
            status: 0,
            statusEffects: []);
    }

    public static void PersonSelect(ref FrameWriter writer, int actor, int account, bool tutorialCleared)
    {
        writer.Begin(Opcodes.PersonSelectRequest);
        writer.WriteInt32(actor);
        writer.WriteInt32(account);
        writer.WriteBool(tutorialCleared);
        writer.End();
    }

    /// <summary>
    /// The first request on a district or a maze: who comes, from which map, with which session.
    /// </summary>
    public static void PersonEnter(ref FrameWriter writer, int account, int actor, ulong map, bool firstConnect, long session)
    {
        writer.Begin(Opcodes.PersonEnterRequest);
        writer.WriteInt32(account);
        writer.WriteInt32(actor);
        writer.WriteUInt64(map);
        writer.WriteBool(firstConnect);
        writer.WriteInt64(session);
        writer.End();
    }

    /// <summary>
    /// What a client asks for when it has finished loading a district: the others around it.
    /// </summary>
    public static void WorldObject(ref FrameWriter writer)
    {
        writer.Begin(Opcodes.WorldObjectRequest);
        writer.End();
    }

    /// <summary>
    /// The request to enter a maze, as the client sends it from a district. The server takes the account and the
    /// person from the connection, so those fields stay empty as in the client's packets.
    /// </summary>
    public static void MazeCreate(ref FrameWriter writer, short location, int portal)
    {
        writer.Begin(Opcodes.MazeCreateRequest);
        writer.WriteInt32(0); // Account.
        writer.WriteInt32(0); // Actor.
        writer.WriteInt32(0); // Party.
        writer.WriteInt16(location);
        writer.WriteInt16(0); // Channel.
        writer.WriteInt32(0); // Jump.
        writer.WriteInt32(portal);
        writer.WriteInt16(0); // District world.
        writer.WriteUInt64(0); // Map.
        writer.WriteByte((byte)ChangeServerType.EnterMap);
        writer.End();
    }

    /// <summary>
    /// The request to enter another map, a district or a maze. As in the packets of the client the account and the
    /// person are empty: the server knows them from the connection.
    /// </summary>
    public static void WorldEnter(ref FrameWriter writer, short location, int jump, int portal)
    {
        writer.Begin(Opcodes.WorldEnterRequest);
        writer.WriteInt32(0); // Account.
        writer.WriteInt32(0); // Actor.
        writer.WriteInt32(0); // Party.
        writer.WriteInt16(location);
        writer.WriteInt16(0); // Channel.
        writer.WriteInt32(jump);
        writer.WriteInt32(portal);
        writer.WriteUInt64(0); // Map.
        writer.WriteInt16(0); // District world.
        writer.WriteByte((byte)ChangeServerType.EnterMap);
        writer.End();
    }

    public static void ChannelChange(ref FrameWriter writer, short channel)
    {
        writer.Begin(Opcodes.ChannelChangeRequest);
        writer.WriteInt16(channel);
        writer.End();
    }

    /// <summary>
    /// A move towards the target, sent from the place the character is at, the way a client reports its running.
    /// </summary>
    public static void Move(
        ref FrameWriter writer,
        int actor,
        ulong map,
        Vector3 position,
        float yaw,
        float targetX,
        float targetY,
        bool running,
        float pitch,
        float speed,
        byte changeMotion)
    {
        writer.Begin(Opcodes.MoveRequest);
        writer.WriteInt32(actor);
        writer.WriteUInt64(map);
        writer.WriteSingle(position.X);
        writer.WriteSingle(position.Y);
        writer.WriteSingle(position.Z);
        writer.WriteSingle(yaw);
        writer.WriteSingle(targetX);
        writer.WriteSingle(targetY);
        writer.WriteBool(running);
        writer.WriteSingle(pitch);
        writer.WriteSingle(speed);
        writer.WriteByte(changeMotion);
        writer.End();
    }

    public static void Ping(ref FrameWriter writer, long token)
    {
        writer.Begin(Opcodes.Ping);
        writer.WriteInt64(token);
        writer.End();
    }

    public static void KeepAlive(ref FrameWriter writer, long ticks)
    {
        writer.Begin(Opcodes.KeepAlive);
        writer.WriteInt64(ticks);
        writer.End();
    }

    /// <summary>
    /// A line said aloud to the ones around.
    /// </summary>
    public static void Chat(ref FrameWriter writer, string text)
    {
        if (text.Length > MaxChatLength)
        {
            throw new ArgumentException($"A chat line holds at most {MaxChatLength} characters.", nameof(text));
        }

        writer.Begin(Opcodes.ChatNormal);
        writer.WriteByte((byte)ChatMessageAppearance.Normal);
        writer.WriteString(text);
        writer.End();
    }

    /// <summary>
    /// The client has loaded the maze, so the script of it may start.
    /// </summary>
    public static void MazeComplete(ref FrameWriter writer)
    {
        writer.Begin(Opcodes.MazeCompleteRequest);
        writer.End();
    }

    /// <summary>
    /// The use of a skill where the character stands. The layout is the one the server reads: the skill, a field
    /// the server does not name, the place with the heading and the motion, the input flag, the divergence, the
    /// skill it follows, the slot of the deck and the index of a random trap.
    /// </summary>
    public static void SkillActive(ref FrameWriter writer, int skill, Vector3 position, float angle)
    {
        writer.Begin(Opcodes.SkillActiveRequest);
        writer.WriteInt32(skill);
        writer.WriteInt32(0);
        writer.WriteSingle(position.X);
        writer.WriteSingle(position.Y);
        writer.WriteSingle(position.Z);
        writer.WriteSingle(angle);
        writer.WriteInt16(-1); // Keeps the motion the character is in.
        writer.WriteBool(false);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteByte(0);
        writer.WriteInt32(0);
        writer.End();
    }
}
