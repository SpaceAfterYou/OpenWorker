namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet.Abstractions;

internal interface IPacketExchanger
{
    IEnumerable<byte> Exchange(IEnumerable<byte> buffer);
}
