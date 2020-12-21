using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct CharacterMarkFavoriteRequest
    {
        private int Unknown1 { get; }
        public int Id { get; }
        private IReadOnlyList<byte> Unknown2 { get; }

        public CharacterMarkFavoriteRequest(BinaryReader br)
        {
            Unknown1 = br.ReadInt32();
            Id = br.ReadInt32();
            Unknown2 = br.ReadBytes(23);
        }
    }
}
