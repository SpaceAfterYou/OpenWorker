using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests.Movement
{
    [Request]
    public readonly struct JumpRequest
    {
        public int CharacterId { get; }
        public short Unknown1 { get; }
        public short Unknown2 { get; }
        public ushort Location { get; }
        public short Unknown3 { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public Vector2 Interpolated { get; }
        public int Unknown5 { get; }
        public int Unknown6 { get; }

        public JumpRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Unknown1 = br.ReadInt16();
            Unknown2 = br.ReadInt16();
            Location = br.ReadUInt16();
            Unknown3 = br.ReadInt16();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            Interpolated = br.ReadVector2();
            Unknown5 = br.ReadInt32();
            Unknown6 = br.ReadInt32();
        }
    }
}
