using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct ExchangeInterestListRequest
    {
        public int Character { get; }

        public ExchangeInterestListRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
