using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Benchmarks;

/// <summary>
/// One step of a player, as the client sends it and as the server tells the others.
/// </summary>
internal static class Movement
{
    private const int Actor = 0x123456;
    private const long Map = 0x0001_2713_0000_0000;

    /// <summary>
    /// The decrypted body of a move request, as the read loop hands it over: the opcode, then the fields in the order the
    /// client writes them.
    /// </summary>
    public static byte[] Request()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(MessageOpcode.From(GroupOpcode.Move, MoveOpcode.Move).Value);
        writer.Write(Actor);
        writer.Write(Map);
        writer.Write(1f);
        writer.Write(2f);
        writer.Write(3f);
        writer.Write(4f);
        writer.Write(5f);
        writer.Write(6f);
        writer.Write((byte)1);
        writer.Write(0f);
        writer.Write(500f);
        writer.Write((byte)0);
        writer.Flush();

        return stream.ToArray();
    }

    /// <summary>
    /// What the others on the map are told about the step.
    /// </summary>
    public static MovementMoveBtResponse Response() => new()
    {
        Actor = Actor,
        Map = new MapValue { Location = 10003, Server = 1 },
        Pos = new Vector3(1f, 2f, 3f),
        Yaw = 4f,
        TargetPosX = 5f,
        TargetPosY = 6f,
        RunBit = 1,
        Pitch = 0f,
        MoveSpeed = 500f,
        ChangeMotion = 0
    };
}
