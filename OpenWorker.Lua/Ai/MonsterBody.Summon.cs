using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// An attack of a monster that brings in another monster: at a random point of a square round the centre of the attack's space,
/// as wide as its reach, on the floor, in the monster's own sector, looking the way the monster looks turned by the attack's angle.
/// Only a monster of the maze brings monsters in.
/// </summary>
internal partial class MonsterBody
{
    private void Summon(AttackTrigger trigger)
    {
        if (Owner is not null || trigger.SpawnObject <= 0 || !float.IsFinite(trigger.Radius))
        {
            return;
        }

        var (sin, cos) = MathF.SinCos(Yaw * (MathF.PI / 180f));
        var position = Position;
        var center = new Vector2(trigger.Center.X * cos - trigger.Center.Y * sin + position.X, trigger.Center.X * sin + trigger.Center.Y * cos + position.Y);
        var reach = (int)Math.Clamp(trigger.Radius, 0f, int.MaxValue / 2f) + 1;
        var place = OnFloor(new Vector3(
            center.X + (_manager.Random.Next(2 * reach) - reach),
            center.Y + (_manager.Random.Next(2 * reach) - reach),
            position.Z + trigger.Center.Z));
        var yaw = Yaw + trigger.Angle;

        if (yaw > 180f)
        {
            yaw -= 360f;
        }

        _manager.CreateMonstersAt(trigger.SpawnObject, [place], yaw, _ecs.Get<NonPlayableCreatureComponent>(Entity).Sector);
    }
}
