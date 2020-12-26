using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct PartyAcceptRequest
    {
        public int Character { get; }

        public PartyAcceptRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
