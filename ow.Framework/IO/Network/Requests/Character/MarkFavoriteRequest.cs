using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct MarkFavoriteRequest
    {
        private int Unknown1 { get; }
        public int Id { get; }
        private IReadOnlyList<byte> Unknown2 { get; }

        public MarkFavoriteRequest(BinaryReader br)
        {
            Unknown1 = br.ReadInt32();
            Id = br.ReadInt32();
            Unknown2 = br.ReadBytes(23);
        }
    }
}