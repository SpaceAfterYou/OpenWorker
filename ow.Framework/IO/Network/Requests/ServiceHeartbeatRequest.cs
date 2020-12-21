using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public sealed record ServiceHeartbeatRequest
    {
        public ulong Tick { get; }
        private ulong Unknown1 { get; }
        private uint Unknown2 { get; }

        public ServiceHeartbeatRequest(BinaryReader br) => (Tick, Unknown1, Unknown2) = (br.ReadUInt64(), br.ReadUInt64(), br.ReadUInt32());
    }
}