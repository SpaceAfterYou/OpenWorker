using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct GateConnectRequest
    {
        public ushort Gate { get; }

        public GateConnectRequest(BinaryReader br) => Gate = br.ReadUInt16();
    }
}
