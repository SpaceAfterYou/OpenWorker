using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Exchange
{
    [Request]
    public readonly struct InterestListRequest
    {
        public int CharacterId { get; }

        public InterestListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
