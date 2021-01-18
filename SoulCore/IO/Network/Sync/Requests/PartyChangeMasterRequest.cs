using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PartyChangeMasterRequest
    {
        public int Id { get; }

        public PartyChangeMasterRequest(BinaryReader br)
            => Id = br.ReadInt32();
    }
}
