using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SkillUpgradeRequest
    {
        public uint Id { get; }

        /// <summary>
        /// left-right side
        /// </summary>
        public uint Modifier { get; }

        public uint Unknown2 { get; }
        public uint Unknown3 { get; }
        public uint Unknown4 { get; }

        public SkillUpgradeRequest(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Modifier = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            Unknown4 = br.ReadUInt32();
        }
    }
}
