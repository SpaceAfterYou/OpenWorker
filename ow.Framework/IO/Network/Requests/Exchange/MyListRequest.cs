using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Exchange
{
    [Request]
    public readonly struct MyListRequest
    {
        public int CharacterId { get; }

        public MyListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
