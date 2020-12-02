using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Friends
{
    [Request]
    public readonly struct RecruitListRequest
    {
        public ushort Unknown1 { get; }
        public ushort Unknown2 { get; }
        public ushort Unknown3 { get; }
        public byte Unknown4 { get; }

        public RecruitListRequest(BinaryReader br)
        {
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            Unknown3 = br.ReadUInt16();
            Unknown4 = br.ReadByte();
        }
    }
}
