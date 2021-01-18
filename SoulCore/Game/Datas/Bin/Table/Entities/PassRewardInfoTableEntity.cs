using SoulCore.Attributes;
using System;
using System.IO;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_Pass_Reward")]
    public sealed record PassRewardInfoTableEntity : ITableEntity<KeyType>
    {
        public readonly struct ItemInfo
        {
            public uint Id { get; init; }
            public ushort Count { get; init; }

            public ItemInfo(BinaryReader br) => (Id, Count) = (br.ReadUInt32(), br.ReadUInt16());
        }

        public readonly struct ItemsInfo
        {
            public ItemInfo Slave { get; }
            public ItemInfo Premium { get; }

            public ItemsInfo(BinaryReader br) => (Slave, Premium) = (new(br), new(br));
        }

        public KeyType Id { get; }
        public uint PassInfo { get; }
        public byte SequenceId { get; }
        public uint PointRequired { get; }
        public ItemsInfo RewardItem { get; init; }

        internal PassRewardInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            PassInfo = br.ReadUInt32();
            SequenceId = br.ReadByte();
            PointRequired = br.ReadUInt32();
            RewardItem = new(br);
        }
    }
}
