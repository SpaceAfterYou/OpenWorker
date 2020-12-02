using Core.Systems.NetSystem.Attributes;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Character
{
    [Request]
    public readonly struct MarkFavoriteRequest
    {
        private int Unknown1 { get; }
        public int Id { get; }
        private byte[] Unknown2 { get; }

        public MarkFavoriteRequest(BinaryReader br)
        {
            Unknown1 = br.ReadInt32();
            Id = br.ReadInt32();
            Unknown2 = br.ReadBytes(23);
        }
    }
}
