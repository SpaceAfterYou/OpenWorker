using ow.Framework.Extensions;
using ow.Framework.IO.Network.Attributes;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public sealed record MovementStopRequest
    {
        public int Character { get; }
        public ulong Unknown1 { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public float Unknown4 { get; }
        public byte Unknown5 { get; }

        public MovementStopRequest(BinaryReader br)
        {
            Character = br.ReadInt32();
            Unknown1 = br.ReadUInt64();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            Unknown4 = br.ReadSingle();
            Unknown5 = br.ReadByte();
        }
    }
}
