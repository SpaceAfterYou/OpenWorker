using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct ExchangeInterestListRequest
    {
        public int Character { get; }

        public ExchangeInterestListRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
