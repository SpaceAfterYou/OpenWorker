using System.IO;
using SoulCore.IO.Network.Sync.Attributes;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PostRecvListRequest
    {
        public uint Unknown1 { get; }

        public PostRecvListRequest(BinaryReader br) => Unknown1 = br.ReadUInt32();
    }
}
