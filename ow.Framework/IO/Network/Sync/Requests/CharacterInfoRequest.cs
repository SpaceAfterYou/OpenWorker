using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct CharacterInfoRequest
    {
        public int CharacterId { get; }
        public byte Unknown1 { get; }

        public CharacterInfoRequest(BinaryReader br) => (CharacterId, Unknown1) = (br.ReadInt32(), br.ReadByte());
    }
}
