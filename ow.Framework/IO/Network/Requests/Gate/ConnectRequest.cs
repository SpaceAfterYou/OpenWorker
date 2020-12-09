using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct ConnectRequest
    {
        public ushort GateId { get; }

        public ConnectRequest(BinaryReader br) =>
            GateId = br.ReadUInt16();
    }
}
