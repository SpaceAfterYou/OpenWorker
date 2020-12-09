using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.SpecialOption
{
    [Request]
    public readonly struct UpdateListRequest
    {
        public int CharacterId { get; }

        public UpdateListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
