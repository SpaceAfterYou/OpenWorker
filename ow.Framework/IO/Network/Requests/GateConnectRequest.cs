using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct GateConnectRequest
    {
        public ushort Gate { get; }

        public GateConnectRequest(BinaryReader br) => Gate = br.ReadUInt16();
    }
}
