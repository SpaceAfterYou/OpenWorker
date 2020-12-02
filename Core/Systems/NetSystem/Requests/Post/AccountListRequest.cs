using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Post
{
    [Request]
    public readonly struct AccountListRequest
    {
        public uint Unknown1 { get; }

        public AccountListRequest(BinaryReader br)
            => Unknown1 = br.ReadUInt32();
    }
}
