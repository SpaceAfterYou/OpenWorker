namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;

internal interface IReadOnlyRawPacketPipe
{
    Stream OpenStream();
}
