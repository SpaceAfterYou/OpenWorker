using ow.Framework.Extensions;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public sealed record MovementMoveRequest
    {
        public int Character { get; }
        public ulong Unknown1 { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public Vector2 InterpolatedPosition { get; }
        public byte Unknown5 { get; }
        public float Unknown6 { get; }
        public short Unknown7 { get; }
        public int Unknown8 { get; }

        public MovementMoveRequest(BinaryReader br)
        {
            Character = br.ReadInt32();
            Unknown1 = br.ReadUInt64();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            InterpolatedPosition = br.ReadVector2();
            Unknown5 = br.ReadByte();
            Unknown6 = br.ReadSingle();
            Unknown7 = br.ReadInt16();
            Unknown8 = br.ReadInt32();
        }
    }
}
