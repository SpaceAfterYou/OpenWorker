using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Post
{
    [Request]
    public readonly struct RecvListRequest
    {
        public uint Unknown1 { get; }

        public RecvListRequest(BinaryReader br)
            => Unknown1 = br.ReadUInt32();
    }
}
