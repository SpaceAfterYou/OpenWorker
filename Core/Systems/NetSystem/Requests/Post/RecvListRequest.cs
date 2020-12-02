using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Post
{
    [Request]
    public readonly struct RecvListRequest
    {
        public uint Unknown1 { get; }

        public RecvListRequest(BinaryReader br)
            => Unknown1 = br.ReadUInt32();
    }
}
