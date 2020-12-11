using System.Numerics;

namespace ow.Service.District.Game
{
    public interface IReadOnlyCachedNpc
    {
        public uint Id { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public uint Waypoint { get; }
    }
}