using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Monster.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// What a game master's command does in the maze: things no map or script of the maze may ever start.
/// </summary>
public partial class LuaMaze
{
    // A monster a command gives is let go by a command or by leaving, never by the end of a skill.
    private const int CommandControl = -1;

    private const float CommandControlAlpha = 1f;
    private const float CommandControlBlend = 0.4f;

    /// <summary>
    /// How far around the first one the others of a group a command calls in appear.
    /// </summary>
    private const float CommandMonsterScatter = 300f;

    /// <summary>
    /// Starts the unity event of that number in the sector the player stands in. False when the maze has no such event.
    /// </summary>
    [LuaIgnoreMember]
    public bool StartUnityModeFor(Entity player, int table)
    {
        if (Closed || Find(player) is null || UnityEvents is not { } events || !events.Has(table))
        {
            return false;
        }

        BeginUnity(table, SectorAt(Ecs.Get<WorldComponent>(player).Position)?.Id ?? 0);

        return true;
    }

    /// <summary>
    /// A defensive weapon: the clients have no model for any, and a client shown one fails and closes. Commands bring none in.
    /// </summary>
    [LuaIgnoreMember]
    public bool IsDefensiveWeapon(int monster)
    {
        return creatureManager.Services?.Behaviors.Get(monster) is not null && combat.GetMonster(monster).IsDefensiveWeapon;
    }

    /// <summary>
    /// Brings monsters of that row in where the player stands, facing as it faces, the others of the group scattered around
    /// the first. False when the table has no such monster, or it is a defensive weapon.
    /// </summary>
    [LuaIgnoreMember]
    public bool CallMonsters(Entity player, int monster, int count)
    {
        if (Closed || Find(player) is null || creatureManager.Services?.Behaviors.Get(monster) is null || IsDefensiveWeapon(monster))
        {
            return false;
        }

        var place = Ecs.Get<WorldComponent>(player);
        var sector = SectorAt(place.Position)?.Id ?? 0;
        var positions = Enumerable.Range(0, count)
            .Select(e => e == 0 ? place.Position : place.Position + new Vector3(Scatter(), Scatter(), 0f))
            .ToArray();

        creatureManager.CreateMonstersAt(monster, positions, place.Rotation, sector);

        return true;
    }

    /// <summary>
    /// The player takes a monster of that row as a skill would give it, where it stands; row 0 lets its monster go. False
    /// when the table has no such monster, it is a defensive weapon, the player has fallen, or it has no monster to let go.
    /// </summary>
    [LuaIgnoreMember]
    public bool ControlMonster(Entity player, int monster)
    {
        if (Closed || !Started || Find(player) is not { } member)
        {
            return false;
        }

        if (monster == 0)
        {
            var had = _controlled.ContainsKey(member);

            EndControl(member);

            return had;
        }

        if (!creatureManager.Vitals.IsAlive(player) || creatureManager.Services?.Behaviors.Get(monster) is not { } behavior || IsDefensiveWeapon(monster))
        {
            return false;
        }

        TakeControl(member, behavior, monster, Ecs.Get<WorldComponent>(player).Position, CommandControlAlpha, CommandControlBlend,
            MonsterControlResponse.NoAnimation, follows: true, CommandControl);

        return true;
    }

    private float Scatter() => (creatureManager.Random.NextSingle() * 2f - 1f) * CommandMonsterScatter;
}
