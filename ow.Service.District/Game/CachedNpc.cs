using System.Numerics;

namespace ow.Service.District.Game
{
    public sealed class CachedNpc
    {
        public uint Id { get; }
        public uint MobId { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public uint Waypoint { get; }

        public CachedNpc(uint id, uint mobId, in Vector3 position, float rotation, uint waypoint)
        {
            Id = id;
            MobId = mobId;
            Position = position;
            Rotation = rotation;
            Waypoint = waypoint;
        }
    }
}