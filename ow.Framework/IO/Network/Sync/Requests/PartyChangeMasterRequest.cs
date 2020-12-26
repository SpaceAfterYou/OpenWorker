using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct PartyChangeMasterRequest
    {
        public int Id { get; }

        public PartyChangeMasterRequest(BinaryReader br)
            => Id = br.ReadInt32();
    }
}
