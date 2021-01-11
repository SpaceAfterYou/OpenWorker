using ow.Framework.IO.Network.Sync.Attributes;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SCharacterMarkFavoriteRequest
    {
        private int Unknown1 { get; }
        public int Id { get; }
        private IReadOnlyList<byte> Unknown2 { get; }

        public SCharacterMarkFavoriteRequest(BinaryReader br)
        {
            Unknown1 = br.ReadInt32();
            Id = br.ReadInt32();
            Unknown2 = br.ReadBytes(23);
        }
    }
}
