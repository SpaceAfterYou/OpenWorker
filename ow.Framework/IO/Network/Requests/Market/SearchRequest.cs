using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Market
{
    [Request]
    public readonly struct SearchRequest
    {
        public uint Page { get; }
        public uint KeywordId { get; }
        public uint Category { get; }
        public uint Group { get; }
        public uint CharacterId { get; }
        public uint MinLevel { get; }
        public uint MaxLevel { get; }
        public uint Rarity { get; }
        public uint Unknown1 { get; }
        public uint PriceBelow { get; }
        public uint Unknown2 { get; }
        public uint Upgrade { get; }

        public SearchRequest(BinaryReader br)
        {
            Page = br.ReadUInt32();
            KeywordId = br.ReadUInt32();
            Category = br.ReadUInt32();
            Group = br.ReadUInt32();
            CharacterId = br.ReadUInt32();
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
