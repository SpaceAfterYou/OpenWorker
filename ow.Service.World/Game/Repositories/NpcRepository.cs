using ow.Framework.Game.Datas.World.Table;
using ow.Framework.Game.Datas.World.Table.EventBox;
using ow.Framework.Game.Datas.World.Table.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ow.Service.District.Game.NpcRepository;

namespace ow.Service.District.Game
{
    public sealed class NpcRepository : List<Npc>
    {
        public sealed record Npc
        {
            public readonly uint Id;
            public readonly uint CreatureId;
            public readonly Vector3 Position;
            public readonly float Rotation;
            public readonly uint Waypoint;

            public Npc(uint id, uint creatureId, Vector3 position, float rotation, uint waypoint)
            {
                Id = id;
                CreatureId = creatureId;
                Position = position;
                Rotation = rotation;
                Waypoint = waypoint;
            }
        }

        public NpcRepository(Instance instance) : base(GetEntities(instance.Place))
        {
        }

        private static IEnumerable<Npc> GetEntities(VRoot root, uint id = uint.MinValue) => root.EventBox.MonsterSpawns
            .Select(c => c.Monsters
                .Where(m => m.Id != 0 && m.Type == MonsterSpawnType.Npc)
                .Select(m => new Npc(id++, m.Id, GetPosition(c), c.Rotation, c.Waypoint)))
            .SelectMany(i => i);

        private static float GetRandomValue(float min, float max) => (float)((new Random().NextDouble() * (max - min)) + min);

        private static Vector3 GetPosition(VMonsterSpawnBox box)
        {
            if (box.CreationPositionType == CreationPositionType.Center)
            {
                return box.PosBottomRight - ((box.PosBottomRight - box.PosTopLeft) / 2);
            }

            return new(
                GetRandomValue(box.PosTopLeft.X, box.PosBottomRight.X),
                GetRandomValue(box.PosTopLeft.Y, box.PosBottomRight.Y),
                GetRandomValue(box.PosTopLeft.Z, box.PosBottomRight.Z)
            );
        }
    }
}