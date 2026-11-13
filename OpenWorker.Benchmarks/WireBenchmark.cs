using System.Numerics;
using BenchmarkDotNet.Attributes;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot;
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
/// The two halves of the wire format on the hot path: a move request parsed from the body of a packet, by the old way and
/// the new, and the movement broadcast encoded into a frame (opcode, body, encryption, header).
/// </summary>
[MemoryDiagnoser]
public class WireBenchmark : IDisposable
{
    // The body after the opcode, as the handler gets it.
    private readonly byte[] _body = Movement.Request()[2..];

    // Boxed once, as the handler boxes its response itself.
    private readonly IResponseHotspotMessage _generated = Movement.Response();

    private MemoryStream _stream = null!;
    private BinaryReader _reader = null!;

    [GlobalSetup]
    public void Setup()
    {
        // One stream and one reader for every packet, as the executor reuses them.
        _stream = new MemoryStream(_body, writable: false);
        _reader = new BinaryReader(_stream);

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
