using SoulCore.Extensions;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;
using System.Numerics;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public sealed record SRMJumpRequest
    {
        public int Character { get; }
        public short Unknown1 { get; }
        public short Unknown2 { get; }
        public ushort Location { get; }
        public short Unknown3 { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public Vector2 InterpolatedPosition { get; }
        public int Unknown5 { get; }
        public int Unknown6 { get; }

        public SRMJumpRequest(BinaryReader br)
        {
            Character = br.ReadInt32();
            Unknown1 = br.ReadInt16();
            Unknown2 = br.ReadInt16();
            Location = br.ReadUInt16();
            Unknown3 = br.ReadInt16();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            InterpolatedPosition = br.ReadVector2();
            Unknown5 = br.ReadInt32();
            Unknown6 = br.ReadInt32();
        }
    }
}
