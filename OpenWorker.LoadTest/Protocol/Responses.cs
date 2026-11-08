using System.Buffers.Binary;
using System.Numerics;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Skill.Responses;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// The answer to a login: the account and the session key on success, a code on a refusal.
/// </summary>
internal sealed record LoginResult(int Account, string Login, int ErrorCode, string ErrorMessage, long Session);

internal sealed record GateEntry(short Id, int Port, string Name, string Address, int Workload, int Online, byte Persons);

/// <summary>
/// Where a connection is sent next. The address is empty or all zeros when the server refused. The port is 16 bits on
/// the wire and read as unsigned, so a port above 32767 is what it should be.
/// </summary>
internal sealed record Redirect(string Address, int Port);

/// <summary>
/// The place a character is in or goes to, as the gate and the districts tell it. It is the answer to the selection
/// of a character and to every request to enter another map.
/// </summary>
internal sealed record EnterMapResult(
    int Actor,
    int Account,
    int Jump,
    ulong Map,
    string Address,
    int Port,
    short Location,
    Vector3 Position,
    float Rotation,
    byte Type,
    bool ChangeServer,
    int Result);

internal sealed record KickOut(byte Reason, int Account, string Message);

/// <summary>
/// What the server says in a message the bot reads on the hot path. The fields are read at fixed places, so the
/// values come out without a reader object.
/// </summary>
internal static class Responses
{
    /// <summary>
    /// The size of the broadcast of a movement: opcode, actor, map, position (three floats), yaw, two target
    /// coordinates, run bit, pitch, speed, and the flag of a motion change.
    /// </summary>
    public const int MoveBroadcastSize = 2 + 4 + 8 + 12 + 4 + 4 + 4 + 1 + 4 + 4 + 1;

    private const int MoveActorOffset = 2;
    private const int MovePositionOffset = 2 + 4 + 8;

    public static LoginResult ReadLogin(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        var account = reader.ReadInt32();

        reader.ReadBool(); // Tutorial cleared.
        reader.ReadAsciiTerminated(); // The MAC address, as the request gave it.

        var message = reader.ReadUnicode();
        var code = reader.ReadInt32();

        reader.ReadByte(); // The kind of login.

        var login = reader.ReadUnicode();
        var session = reader.ReadInt64();

        return new LoginResult(account, login, code, message, session);
    }

    public static GateEntry[] ReadGateList(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        reader.ReadByte(); // The gate of the last visit, which the client ignores.

        var entries = new GateEntry[reader.ReadByte()];

        for (var i = 0; i < entries.Length; i++)
        {
            var id = reader.ReadInt16();
            var port = (int)reader.ReadUInt16();
            var name = reader.ReadAsciiWithTerminator();
            var address = reader.ReadAscii();
            var workload = reader.ReadInt32();
            var online = reader.ReadInt32();
            var persons = reader.ReadByte();

            entries[i] = new GateEntry(id, port, name, address, workload, online, persons);
        }

        return entries;
    }

    public static Redirect ReadGateConnect(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        return new Redirect(reader.ReadAsciiWithTerminator(), reader.ReadUInt16());
    }

    /// <summary>
    /// The answer of the gate to the entry: the account on success, or nothing.
    /// </summary>
    public static int? ReadEnterGate(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        var failed = reader.ReadBool();
        var account = reader.ReadInt32();

        return failed ? null : account;
    }

    public static KickOut ReadKickOut(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        return new KickOut(reader.ReadByte(), reader.ReadInt32(), reader.ReadUnicode());
    }

    /// <summary>
    /// A failed request: the opcode of the expected answer with the error flag, and a code.
    /// </summary>
    public static ushort ReadErrorCode(ReadOnlySpan<byte> frame)
    {
        return PacketReader.AfterOpcode(frame).ReadUInt16();
    }

    /// <summary>
    /// The place of the answers to the selection of a character, the entry to another map and the change of channel:
    /// a zone with the address of the next server, then the flags of the change.
    /// </summary>
    public static EnterMapResult ReadEnterMap(ReadOnlySpan<byte> frame)
    {
        var reader = PacketReader.AfterOpcode(frame);

        var actor = reader.ReadInt32();
        var account = reader.ReadInt32();

        reader.ReadInt32(); // Server.

        var jump = reader.ReadInt32();
        var map = reader.ReadUInt64();

        reader.ReadUInt64(); // The parent map.

        var address = reader.ReadAscii();
        var port = (int)reader.ReadUInt16();

        var location = reader.ReadInt16();

        reader.ReadUInt64(); // The map of the world value, the same as above.

        var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        var rotation = reader.ReadSingle();

        var type = reader.ReadByte();

        var changeServer = reader.ReadBool();

        reader.ReadByte(); // The kind of change.

        var result = reader.ReadInt32();

        return new EnterMapResult(actor, account, jump, map, address, port, location, position, rotation, type, changeServer, result);
    }

    /// <summary>
    /// The characters of an account, as far as the bot needs them: who they are and which one was used last.
    /// The values are read by the type of the server, so the whole of the layout is followed.
    /// </summary>
    public static (PersonSummary[] Persons, int Last) ReadPersonList(ReadOnlySpan<byte> frame)
    {
        using var stream = new MemoryStream(frame[sizeof(ushort)..].ToArray());
        using var reader = new BinaryReader(stream);

        var persons = new PersonSummary[reader.ReadByte()];

        for (var i = 0; i < persons.Length; i++)
        {
            var person = new PersonValue(reader);

            persons[i] = new PersonSummary(person.Actor, person.InfoValue.Name);
        }

        return (persons, reader.ReadInt32());
    }

    /// <summary>
    /// The ids of the skills the character has learned, which the district sends once the character has entered.
    /// </summary>
    public static int[] ReadLearnedSkills(ReadOnlySpan<byte> frame)
    {
        using var stream = new MemoryStream(frame[sizeof(ushort)..].ToArray());
        using var reader = new BinaryReader(stream);

        var snapshot = new SkillLoadInfoResponse(reader).Snapshot;

        return snapshot.SkillList.Select(e => e.Skill).ToArray();
    }

    /// <summary>
    /// The actor of a movement broadcast, with the three bits of its kind still in the top.
    /// </summary>
    public static int ReadMoveActor(ReadOnlySpan<byte> frame)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(frame[MoveActorOffset..]);
    }

    /// <summary>
    /// Where the broadcast of a movement puts the actor.
    /// </summary>
    public static (float X, float Y) ReadMovePosition(ReadOnlySpan<byte> frame)
    {
        return (
            BinaryPrimitives.ReadSingleLittleEndian(frame[MovePositionOffset..]),
            BinaryPrimitives.ReadSingleLittleEndian(frame[(MovePositionOffset + sizeof(float))..]));
    }

    /// <summary>
    /// The number a ping brings back, the same one the request carried.
    /// </summary>
    public static long ReadPingToken(ReadOnlySpan<byte> frame)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(frame[sizeof(ushort)..]);
    }

    public static long ReadKeepAliveTicks(ReadOnlySpan<byte> frame)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(frame[sizeof(ushort)..]);
    }

    /// <summary>
    /// The answer to a skill: zero is success.
    /// </summary>
    public static short ReadSkillResult(ReadOnlySpan<byte> frame)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(frame[sizeof(ushort)..]);
    }

    /// <summary>
    /// Who said a line aloud, and the number the line begins with. The bot says lines made of a number only,
    /// so the number identifies the line it said; a line of anybody else has none.
    /// </summary>
    public static bool TryReadChat(ReadOnlySpan<byte> frame, out int actor, out long number)
    {
        // Opcode, actor, the look of the message, then the length of the text in bytes and the text.
        const int textOffset = 2 + 4 + 4 + 2;

        actor = 0;
        number = 0;

        if (frame.Length < textOffset)
        {
            return false;
        }

        actor = BinaryPrimitives.ReadInt32LittleEndian(frame[sizeof(ushort)..]);

        var text = frame[textOffset..];
        var digits = 0;

        for (var i = 0; i + 1 < text.Length && digits < 18; i += 2)
        {
            if (text[i + 1] != 0 || text[i] is < (byte)'0' or > (byte)'9')
            {
                break;
            }

            number = (number * 10) + (text[i] - (byte)'0');
            digits++;
        }

        return digits > 0;
    }
}

internal sealed record PersonSummary(int Actor, string Name);
