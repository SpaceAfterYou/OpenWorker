using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct CharacterInfoRequest
    {
        public int CharacterId { get; }
        public byte Unknown1 { get; }

        public CharacterInfoRequest(BinaryReader br) => (CharacterId, Unknown1) = (br.ReadInt32(), br.ReadByte());
    }
}
