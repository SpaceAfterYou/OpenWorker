using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SCharacterChangeSlotRequest
    {
        private readonly ulong Unknown1;
        private readonly ulong Unknown2;
        public byte FirstSlot { get; }
        public byte SecondSlot { get; }

        public SCharacterChangeSlotRequest(BinaryReader br)
        {
            Unknown1 = br.ReadUInt64();
            Unknown2 = br.ReadUInt64();
            FirstSlot = br.ReadByte();
            SecondSlot = br.ReadByte();
        }
    }
}
