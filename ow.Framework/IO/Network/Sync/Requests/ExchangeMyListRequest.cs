using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct ExchangeMyListRequest
    {
        public int Character { get; }

        public ExchangeMyListRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
