using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;
using System.Numerics;

namespace ow.Framework.IO.Network.Requests.Gesture
{
    [Request]
    public readonly struct DoRequest
    {
        public uint GestureId { get; }
        public Vector3 Position { get; }
        public uint Unknown1 { get; }
        public float Rotation { get; }

        public DoRequest(BinaryReader br)
        {
            GestureId = br.ReadUInt32();
            Position = br.ReadVector3();
            Unknown1 = br.ReadUInt32();
            Rotation = br.ReadSingle();
        }
    }
}
