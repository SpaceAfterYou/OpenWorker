namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;

internal interface IWriteOnlyRawPacketPipe
{
    void Write(ReadOnlySpan<byte> _raw);
}
