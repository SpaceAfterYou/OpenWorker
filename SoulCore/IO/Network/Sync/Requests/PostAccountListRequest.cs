using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PostAccountListRequest
    {
        public uint Unknown1 { get; }

        public PostAccountListRequest(BinaryReader br) => Unknown1 = br.ReadUInt32();
    }
}
