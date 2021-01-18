using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SCharacterDeleteRequest
    {
        public int Id { get; }

        public SCharacterDeleteRequest(BinaryReader br) => Id = br.ReadInt32();
    }
}
