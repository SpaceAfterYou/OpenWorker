using ow.Framework.Extensions;
using ow.Framework.IO.Network.Attributes;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public sealed record GestureDoRequest
    {
        public uint Gesture { get; }
        public Vector3 Position { get; }
        public uint Unknown1 { get; }
        public float Rotation { get; }

        public GestureDoRequest(BinaryReader br)
        {
            Gesture = br.ReadUInt32();
            Position = br.ReadVector3();
            Unknown1 = br.ReadUInt32();
            Rotation = br.ReadSingle();
        }
    }
}
