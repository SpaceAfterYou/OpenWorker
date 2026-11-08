using System.Numerics;
using BenchmarkDotNet.Attributes;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Benchmarks;

/// <summary>
/// A movement request as it was read before the generator: a <see cref="BinaryReader"/> and one read per field, the
/// constructor <see cref="MovementMoveRequest"/> had.
/// </summary>
public readonly struct LegacyMoveRequest(BinaryReader reader)
{
    public ActorValue Actor { get; } = new(reader);
    public MapValue Map { get; } = new(reader);
    public float PosX { get; } = reader.ReadSingle();
    public float PosY { get; } = reader.ReadSingle();
    public float PosZ { get; } = reader.ReadSingle();
    public float Yaw { get; } = reader.ReadSingle();
    public float TargetPosX { get; } = reader.ReadSingle();
    public float TargetPosY { get; } = reader.ReadSingle();
    public byte RunBit { get; } = reader.ReadByte();
    public float Pitch { get; } = reader.ReadSingle();
    public float MoveSpeed { get; } = reader.ReadSingle();
    public byte ChangeMotion { get; } = reader.ReadByte();
}

/// <summary>
/// The movement broadcast as it was written before the generator: <see cref="MovementMoveBtResponse"/>'s
/// hand-written <c>Write(BinaryWriter)</c>, so the encoder takes the path of the <see cref="BinaryWriter"/>.
/// </summary>
public sealed class LegacyMoveBroadcast(MovementMoveBtResponse move) : IResponseHotspotMessage
{
    public MessageOpcode Opcode => move.Opcode;

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(move.Actor);
        writer.WriteMapValue(move.Map);
        writer.Write(move.Pos);
        writer.Write(move.Yaw);
        writer.Write(move.TargetPosX);
        writer.Write(move.TargetPosY);
        writer.Write(move.RunBit);
        writer.Write(move.Pitch);
        writer.Write(move.MoveSpeed);
        writer.Write(move.ChangeMotion);
    }
}

/// <summary>
/// The two halves of the wire format on the hot path, each by the old way and the new: a move request parsed from the
/// body of a packet, and the movement broadcast encoded into a frame (opcode, body, encryption, header).
/// </summary>
[MemoryDiagnoser]
public class WireBenchmark : IDisposable
{
    // The body after the opcode, as the handler gets it.
    private readonly byte[] _body = Movement.Request()[2..];

    // Boxed once each, as the handler boxes its response itself.
    private readonly IResponseHotspotMessage _generated = Movement.Response();
    private readonly IResponseHotspotMessage _legacy = new LegacyMoveBroadcast(Movement.Response());

    private MemoryStream _stream = null!;
    private BinaryReader _reader = null!;

    [GlobalSetup]
    public void Setup()
    {
        // One stream and one reader for every packet, as the executor reuses them.
        _stream = new MemoryStream(_body, writable: false);
        _reader = new BinaryReader(_stream);

        using var legacy = Frame.Encode(_legacy)!;
        using var generated = Frame.Encode(_generated)!;

        if (!legacy.Memory.Span.SequenceEqual(generated.Memory.Span))
        {
            throw new InvalidOperationException("The two writers do not give the same frame.");
        }

        if (ParseWithBinaryReader() != ParseWithPacketReader())
        {
            throw new InvalidOperationException("The two readers do not read the same request.");
        }
    }

    [Benchmark]
    public float ParseWithBinaryReader()
    {
        _stream.Position = 0;

        var request = new LegacyMoveRequest(_reader);

        return request.PosX + request.MoveSpeed;
    }

    [Benchmark]
    public float ParseWithPacketReader()
    {
        var reader = new PacketReader(_body);
        var request = MovementMoveRequest.Read(ref reader);

        return request.fPosX + request.fMoveSpeed;
    }

    [Benchmark]
    public int EncodeWithBinaryWriter()
    {
        using var frame = Frame.Encode(_legacy)!;

        return frame.Length;
    }

    [Benchmark]
    public int EncodeWithPacketWriter()
    {
        using var frame = Frame.Encode(_generated)!;

        return frame.Length;
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _reader?.Dispose();
        _stream?.Dispose();

        GC.SuppressFinalize(this);
    }
}
