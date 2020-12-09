using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests.Movement
{
    [Request]
    public readonly struct MoveRequest
    {
        public int CharacterId { get; }
        public ulong Unknown1 { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public Vector2 Interpolated { get; }
        public byte Unknown5 { get; }
        public float Unknown6 { get; }
        public short Unknown7 { get; }
        public int Unknown8 { get; }

        public MoveRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Unknown1 = br.ReadUInt64();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            Interpolated = br.ReadVector2();
            Unknown5 = br.ReadByte();
            Unknown6 = br.ReadSingle();
            Unknown7 = br.ReadInt16();
            Unknown8 = br.ReadInt32();
        }
    }
}
