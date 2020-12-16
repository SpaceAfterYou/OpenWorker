using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Server
{
    [Request]
    public readonly struct HeartbeatRequest
    {
        public ulong Tick { get; }
        private ulong _1 { get; }
        private uint _2 { get; }

        public HeartbeatRequest(BinaryReader br)
            => (Tick, _1, _2) = (br.ReadUInt64(), br.ReadUInt64(), br.ReadUInt32());
    }
}