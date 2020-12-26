using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct PostRecvListRequest
    {
        public uint Unknown1 { get; }

        public PostRecvListRequest(BinaryReader br) => Unknown1 = br.ReadUInt32();
    }
}
