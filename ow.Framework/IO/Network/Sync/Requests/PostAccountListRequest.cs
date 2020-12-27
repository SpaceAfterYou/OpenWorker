using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PostAccountListRequest
    {
        public uint Unknown1 { get; }

        public PostAccountListRequest(BinaryReader br) => Unknown1 = br.ReadUInt32();
    }
}
