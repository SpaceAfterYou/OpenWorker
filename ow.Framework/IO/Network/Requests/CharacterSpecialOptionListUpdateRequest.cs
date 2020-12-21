using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public sealed record CharacterSpecialOptionListUpdateRequest
    {
        public int Character { get; }

        public CharacterSpecialOptionListUpdateRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}