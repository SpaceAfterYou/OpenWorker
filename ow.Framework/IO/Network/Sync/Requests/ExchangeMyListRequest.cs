using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct ExchangeMyListRequest
    {
        public int Character { get; }

        public ExchangeMyListRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
