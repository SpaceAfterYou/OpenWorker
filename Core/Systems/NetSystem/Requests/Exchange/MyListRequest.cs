using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Exchange
{
    [Request]
    public readonly struct MyListRequest
    {
        public int CharacterId { get; }

        public MyListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
