using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public sealed record CharacterSpecialOptionListUpdateRequest
    {
        public int Character { get; }

        public CharacterSpecialOptionListUpdateRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
