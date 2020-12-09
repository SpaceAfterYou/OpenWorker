using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct InfoRequest
    {
        public int CharacterId { get; }
        public byte Unknown1 { get; }

        public InfoRequest(BinaryReader br) => (CharacterId, Unknown1) = (br.ReadInt32(), br.ReadByte());
    }
}
