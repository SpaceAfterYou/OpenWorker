using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Exchange
{
    [Request]
    public readonly struct InterestListRequest
    {
        public int CharacterId { get; }

        public InterestListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
