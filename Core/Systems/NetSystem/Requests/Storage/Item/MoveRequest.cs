using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Storage.Item
{
    [Request]
    public readonly struct MoveRequest
    {
        public StorageType SrcStorage { get; }
        public int SrcItemId { get; }
        public ushort SrcSlot { get; }
        public StorageType DstStorage { get; }
        public int DstItemId { get; }
        public ushort DstSlot { get; }
        private uint Unknown1 { get; }
        private uint Unknown2 { get; }
        private uint Unknown3 { get; }

        public MoveRequest(BinaryReader br)
        {
            SrcStorage = br.ReadStorageType();
            SrcItemId = br.ReadInt32();
            SrcSlot = br.ReadUInt16();
            DstStorage = br.ReadStorageType();
            DstItemId = br.ReadInt32();
            DstSlot = br.ReadUInt16();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
        }
    }
}
