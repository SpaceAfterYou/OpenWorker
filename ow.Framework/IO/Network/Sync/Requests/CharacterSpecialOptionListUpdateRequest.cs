using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public sealed record CharacterSpecialOptionListUpdateRequest
    {
        public int Character { get; }

        public CharacterSpecialOptionListUpdateRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
