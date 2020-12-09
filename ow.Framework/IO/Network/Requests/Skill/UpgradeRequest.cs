using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Skill
{
    [Request]
    public readonly struct UpgradeRequest
    {
        public uint Id { get; }

        /// <summary>
        /// left-right side
        /// </summary>
        public uint Modifier { get; }

        public uint Unknown2 { get; }
        public uint Unknown3 { get; }
        public uint Unknown4 { get; }

        public UpgradeRequest(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Modifier = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            Unknown4 = br.ReadUInt32();
        }
    }
}
