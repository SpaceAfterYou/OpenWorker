using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Storage.Item
{
    [Request]
    public readonly struct UseRequest
    {
        public StorageType Storage { get; }
        public ushort Slot { get; }
        public uint Seed { get; }
        public int Item { get; }
        public byte Count { get; }

        public UseRequest(BinaryReader br)
        {
            Storage = br.ReadStorageType();
            Slot = br.ReadUInt16();
            Seed = br.ReadUInt32();
            Item = br.ReadInt32();
            Count = br.ReadByte();
        }
    }
}
