using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PartyAcceptRequest
    {
        public int Character { get; }

        public PartyAcceptRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
