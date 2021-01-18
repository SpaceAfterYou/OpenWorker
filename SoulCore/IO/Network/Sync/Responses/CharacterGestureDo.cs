using System.Numerics;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record CharacterGestureDo
    {
        public int Character { get; init; }
        public uint Gesture { get; init; }
        public Vector3 Position { get; init; }
        public float Rotation { get; init; }
    }
}
