using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct CharacterTitleUpdateRequest
    {
        private uint Unknown1 { get; }
        private uint Unknown2 { get; }
        private uint Unknown3 { get; }
        private uint Unknown4 { get; }

        public CharacterTitleUpdateRequest(BinaryReader br)
        {
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            Unknown4 = br.ReadUInt32();
        }
    }
}
