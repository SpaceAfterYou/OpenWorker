using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Party
{
    [Request]
    public readonly struct AcceptRequest
    {
        public int CharacterId { get; }

        public AcceptRequest(BinaryReader br)
            => CharacterId = br.ReadInt32();
    }
}
