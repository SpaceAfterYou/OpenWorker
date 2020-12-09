using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Post
{
    [Request]
    public readonly struct AccountListRequest
    {
        public uint Unknown1 { get; }

        public AccountListRequest(BinaryReader br)
            => Unknown1 = br.ReadUInt32();
    }
}
