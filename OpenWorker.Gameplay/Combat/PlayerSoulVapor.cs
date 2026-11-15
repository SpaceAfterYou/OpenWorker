using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The soul vapor of a player: empty at every entry into a district or a maze, it fills by the soul vapor the player picks up,
/// by buffs and by combos, up to the size its level and its gear give, and the skills that cost soul vapor spend it. Only the
/// player is told its soul vapor (03/34).
/// </summary>
public static class PlayerSoulVapor
{
    public static int Of(World world, Entity player)
    {
        return world.TryGet<AbilityComponent>(player, out var ability) ? ability.SoulVapor.Current : 0;
    }

    /// <summary>
    /// The player enters with no soul vapor and the size its stats give. It is told with the rest of its stats.
    /// </summary>
    public static void Empty(World world, Entity player, float maximum)
    {
        Set(world, player, 0, (int)Math.Max(0f, maximum));
    }

    /// <summary>
    /// The player gets soul vapor, up to its size, and is told when it has more.
    /// </summary>
    public static void Gain(World world, Entity player, int amount)
    {
        if (amount <= 0 || !world.TryGet<AbilityComponent>(player, out var ability))
        {
            return;
        }

        var current = (int)Math.Min((long)ability.SoulVapor.Current + amount, ability.SoulVapor.Max);

        if (current > ability.SoulVapor.Current)
        {
            Set(world, player, current, ability.SoulVapor.Max);
            Tell(world, player, current);
        }
    }

    /// <summary>
    /// A skill spends soul vapor the player has, and the player is told.
    /// </summary>
    public static void Spend(World world, Entity player, int amount)
    {
        if (amount <= 0 || !world.TryGet<AbilityComponent>(player, out var ability))
        {
            return;
        }

        var current = Math.Max(0, ability.SoulVapor.Current - amount);

        Set(world, player, current, ability.SoulVapor.Max);
        Tell(world, player, current);
    }

    /// <summary>
    /// The size changed with the level or the gear: what the player has stays, up to the new size. Returns the soul vapor the
    /// player has now when it had to come down, for the player to be told with the new size.
    /// </summary>
    public static ST_STAT? Resize(World world, Entity player, float maximum)
    {
        if (!world.TryGet<AbilityComponent>(player, out var ability))
        {
            return null;
        }

        var size = (int)Math.Max(0f, maximum);
        var current = Math.Min(ability.SoulVapor.Current, size);

        Set(world, player, current, size);

        return current != ability.SoulVapor.Current ? new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SV_CUR, fValue = current } : null;
    }

    private static void Set(World world, Entity player, int current, int maximum)
    {
        if (world.TryGet<AbilityComponent>(player, out var ability))
        {
            world.Set(player, ability with { SoulVapor = new AbilityComponentEntry { Current = current, Max = maximum } });
        }
    }

    private static void Tell(World world, Entity player, int current)
    {
        if (world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new PersonUpdateStatListResponse
            {
                Actor = world.Get<ActorComponent>(player),
                Stats = [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SV_CUR, fValue = current }]
            });
        }
    }
}
