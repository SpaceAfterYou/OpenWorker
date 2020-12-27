using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct GateConnectRequest
    {
        public ushort Gate { get; }

        public GateConnectRequest(BinaryReader br) => Gate = br.ReadUInt16();
    }
}
