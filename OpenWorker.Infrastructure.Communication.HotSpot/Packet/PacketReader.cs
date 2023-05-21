using Microsoft.Extensions.Logging;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Abstractions;
using SoulWorkerResearch.SoulCore;
using SoulWorkerResearch.SoulCore.Defines;
using SoulWorkerResearch.SoulCore.IO.Net.Packet;
using System.Diagnostics;
using System.Text;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet;

internal sealed record PacketReader
{
    public PacketReader(IPacketExchanger exchanger, ILogger<PacketReader> logger)
    {
        _exchanger = exchanger;
        _logger = logger;
    }

    internal BinaryReader OpenBodyReader(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, true);

        var header = ReadHeader(reader);

        return new BinaryReader(new MemoryStream(ReadBody(reader, header.Size)), Encoding.ASCII, false);
    }

    private static byte[] ReadBufferAsync(BinaryReader reader, int size)
    {
        Debug.Assert(size >= 0, "Bad size");
        Trace.Assert(size <= ushort.MaxValue);

        return reader.ReadBytes(size);
    }

    private PacketHeader ReadHeader(BinaryReader reader)
    {
        var header = new PacketHeader(reader);

        Debug.Assert(header.Magic.Left == Config.PacketHeaderMagic0, "Bad packet first magic");
        Debug.Assert(header.Magic.Right == Config.PacketHeaderMagic1, "Bad packet second magic");
        Debug.Assert(header.Size >= Config.PacketHeaderSize, "Bad packet header packetSize");
        Debug.Assert(header.Protocol == PacketProtocol.ServerClient, "Bad packet protocol");

        _logger.LogDebug("Received header");

        return header;
    }

    private byte[] ReadBody(BinaryReader reader, ushort size)
    {
        var length = size - Config.PacketHeaderSize;
        Debug.Assert(length >= 0);

        var buffer = ReadBufferAsync(reader, length);

        _exchanger.Exchange(buffer);

        return buffer;
    }

    private readonly IPacketExchanger _exchanger;
    private readonly ILogger _logger;
}

// https://youtu.be/PVBBtnndBws?list=PLJ4jpRq1tGIlqoIPz7cdB0TTZ8hznDwZH
