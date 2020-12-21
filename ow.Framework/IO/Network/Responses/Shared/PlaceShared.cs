using System.Numerics;

namespace ow.Framework.IO.Network.Responses.Shared
{
    public sealed record PlaceShared
    {
        public Vector3 Position { get; init; }
        public float Rotation { get; init; }
        public ushort Location { get; init; }
    }
}