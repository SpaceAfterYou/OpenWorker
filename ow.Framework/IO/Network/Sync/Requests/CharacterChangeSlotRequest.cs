using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct CharacterChangeSlotRequest
    {
        private readonly ulong Unknown1;
        private readonly ulong Unknown2;
        public byte FirstSlot { get; }
        public byte SecondSlot { get; }

        public CharacterChangeSlotRequest(BinaryReader br)
        {
            Unknown1 = br.ReadUInt64();
            Unknown2 = br.ReadUInt64();
            FirstSlot = br.ReadByte();
            SecondSlot = br.ReadByte();
        }
    }
}
