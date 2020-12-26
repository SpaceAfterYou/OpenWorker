using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct MarketSearchRequest
    {
        public uint Page { get; }
        public uint Keyword { get; }
        public uint Category { get; }
        public uint Group { get; }
        public uint Character { get; }
        public uint MinLevel { get; }
        public uint MaxLevel { get; }
        public uint Rarity { get; }
        public uint Unknown1 { get; }
        public uint PriceBelow { get; }
        public uint Unknown2 { get; }
        public uint Upgrade { get; }

        public MarketSearchRequest(BinaryReader br)
        {
            Page = br.ReadUInt32();
            Keyword = br.ReadUInt32();
            Category = br.ReadUInt32();
            Group = br.ReadUInt32();
            Character = br.ReadUInt32();
            MinLevel = br.ReadUInt32();
            MaxLevel = br.ReadUInt32();
            Rarity = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
            PriceBelow = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Upgrade = br.ReadUInt32();
        }
    }
}
