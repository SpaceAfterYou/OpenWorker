using Core.Systems.NetSystem.Attributes;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Maze
{
    [Request]
    public readonly struct CreateRequest
    {
        public byte Unknown1 { get; }
        public uint Unknown2 { get; }
        public uint Unknown3 { get; }
        public uint Unknown4 { get; }
        public ushort MazeId { get; }

        public CreateRequest(BinaryReader br)
        {
            Unknown1 = br.ReadByte();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            Unknown4 = br.ReadUInt32();
            MazeId = br.ReadUInt16();
        }
    }
}
