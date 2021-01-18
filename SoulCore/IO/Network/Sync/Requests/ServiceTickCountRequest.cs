using System.IO;
using SoulCore.IO.Network.Sync.Attributes;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct ServiceTickCountRequest
    {
        public uint Seed { get; }
        public uint Unknown1 { get; }
        public ulong Unknown2 { get; }
        public uint Unknown3 { get; }

        public ServiceTickCountRequest(BinaryReader br)
        {
            Seed = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt64();
            Unknown3 = br.ReadUInt32();
        }
    }
}
