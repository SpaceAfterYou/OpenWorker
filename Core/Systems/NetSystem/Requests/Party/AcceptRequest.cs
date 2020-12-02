using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Party
{
    [Request]
    public readonly struct AcceptRequest
    {
        public int CharacterId { get; }

        public AcceptRequest(BinaryReader br)
            => CharacterId = br.ReadInt32();
    }
}
