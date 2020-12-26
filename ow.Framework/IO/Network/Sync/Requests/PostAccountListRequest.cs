using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct PostAccountListRequest
    {
        public uint Unknown1 { get; }

        public PostAccountListRequest(BinaryReader br) => Unknown1 = br.ReadUInt32();
    }
}
