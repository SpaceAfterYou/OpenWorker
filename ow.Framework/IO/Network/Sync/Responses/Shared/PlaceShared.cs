using System.Numerics;

namespace ow.Framework.IO.Network.Sync.Responses.Shared
{
    public sealed record PlaceShared
    {
        public readonly struct SuperArmourGageInfo
        {
            public float Current { get; init; }
            public float Max { get; init; }
        }

        public ushort World { get; init; }
        public ushort Location { get; init; }
        public Vector3 Position { get; init; }
        public float Rotation { get; init; }
        public SuperArmourGageInfo SuperArmourGage { get; init; }
    }
}