using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct CharacterDeleteRequest
    {
        public int Id { get; }

        public CharacterDeleteRequest(BinaryReader br) => Id = br.ReadInt32();
    }
}
