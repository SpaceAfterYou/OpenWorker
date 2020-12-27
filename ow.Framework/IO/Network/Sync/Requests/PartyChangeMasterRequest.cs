using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PartyChangeMasterRequest
    {
        public int Id { get; }

        public PartyChangeMasterRequest(BinaryReader br)
            => Id = br.ReadInt32();
    }
}
