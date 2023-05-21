using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Abstractions;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet;

internal sealed class PacketExchanger : IPacketExchanger
{
    public IEnumerable<byte> Exchange(IEnumerable<byte> buffer) => buffer;
}
