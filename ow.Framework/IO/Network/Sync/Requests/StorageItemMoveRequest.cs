using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct StorageItemMoveRequest
    {
        public readonly struct Item
        {
            public StorageType SrcStorage { get; }
            public int SrcItem { get; }
            public ushort SrcSlot { get; }

            public Item(BinaryReader br) => (SrcStorage, SrcItem, SrcSlot) = (br.ReadStorageType(), br.ReadInt32(), br.ReadUInt16());
        }

        public Item Source { get; }
        public Item Destination { get; }
        private uint Unknown1 { get; }
        private uint Unknown2 { get; }
        private uint Unknown3 { get; }

        public StorageItemMoveRequest(BinaryReader br)
        {
            Source = new(br);
            Destination = new(br);
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
        }
    }
}
