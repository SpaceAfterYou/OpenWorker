using ow.Framework.Game.Datas.World.Table;
using ow.Framework.Game.Datas.World.Table.EventBox;
using ow.Framework.Game.Datas.World.Table.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ow.Service.District.Game
{
    public sealed class CachedNpcRepository : List<CachedNpc>
    {
        public CachedNpcRepository(Zone zone) : base(GetNpcs(zone.Place))
        {
        }

        private static IEnumerable<CachedNpc> GetNpcs(VRoot root, uint id = 0) => root.EventBox.MonsterSpawns
            .Select(c => c.Monsters
                .Where(m => m.Id != 0 && m.Type == MonsterSpawnType.Npc)
                .Select(m => new CachedNpc(id++, m.Id, GetPosition(c), c.Rotation, c.Waypoint)))
            .SelectMany(i => i);

        private static float GetRandomValue(float min, float max) =>
            (float)(new Random().NextDouble() * (max - min) + min);

        private static Vector3 GetPosition(VMonsterSpawnBox box)
        {
            if (box.CreationPositionType == CreationPositionType.Center)
                return box.PosBottomRight - (box.PosBottomRight - box.PosTopLeft) / 2;

            return new(
                GetRandomValue(box.PosTopLeft.X, box.PosBottomRight.X),
                GetRandomValue(box.PosTopLeft.Y, box.PosBottomRight.Y),
                GetRandomValue(box.PosTopLeft.Z, box.PosBottomRight.Z)
            );
        }
    }
}