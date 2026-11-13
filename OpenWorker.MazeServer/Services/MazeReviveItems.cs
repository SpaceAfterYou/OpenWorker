using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The revive items of the maze. A call gets a fallen player up: the player itself, or the fallen of its party in the maze;
/// a player who uses one while fallen uses one of the items the maze allows. A buff or an aura lets the player, or its
/// party, get up once later. The players of a maze of a party are that party.
/// </summary>
public sealed class MazeReviveItems(World world, MazeRevival revival) : IReviveItems
{
    /// <summary>The party has nobody the item is for.</summary>
    public const ushort NobodyInParty = 52604;

    /// <summary>The player carries a buff that lets it get up already.</summary>
    public const ushort AlreadyRevivable = 52609;

    public ushort? Check(Entity player, ReviveItemKind kind)
    {
        if (MazeOf(player) is not { } maze || !revival.LetsUp(maze.Maze))
        {
            return ItemUseCodes.NoRevive;
        }

        var fallen = !maze.Creatures.Vitals.IsAlive(player);
        var party = maze.Party != 0;

        var refusal = kind switch
        {
            ReviveItemKind.Call when !fallen => MazeRevival.NotFallen,
            ReviveItemKind.PartyCall when party && !maze.Members.Any(e => !maze.Creatures.Vitals.IsAlive(e)) => NobodyInParty,
            ReviveItemKind.PartyCall when !party && !fallen => MazeRevival.NotFallen,
            ReviveItemKind.Buff when Revivable(player) => AlreadyRevivable,
            ReviveItemKind.PartyBuff when party && maze.Members.All(Revivable) => NobodyInParty,
            ReviveItemKind.PartyBuff when !party && Revivable(player) => AlreadyRevivable,
            _ => (ushort?)null
        };

        if (refusal is not null)
        {
            return refusal;
        }

        // A fallen player who calls uses one of the items the maze allows.
        return kind is ReviveItemKind.Call or ReviveItemKind.PartyCall && fallen ? revival.CheckItem(player, maze.Maze, fallen: true) : null;
    }

    public void GetUp(Entity player, ReviveItemKind kind)
    {
        if (MazeOf(player) is not { } maze)
        {
            return;
        }

        var actor = world.Get<ActorComponent>(player).Identifier;

        if (!maze.Creatures.Vitals.IsAlive(player))
        {
            revival.ItemUsed(player);
            maze.Creatures.Revive(player, ReviveType.Buff, actor);
        }

        // The call of a party gets its fallen up at once: this client cannot be asked to accept.
        if (kind == ReviveItemKind.PartyCall && maze.Party != 0)
        {
            foreach (var member in maze.Members.ToArray())
            {
                if (member != player && !maze.Creatures.Vitals.IsAlive(member))
                {
                    maze.Creatures.Revive(member, ReviveType.Buff, actor);
                }
            }
        }
    }

    private bool Revivable(Entity player)
    {
        return BuffBook.Of(world, player) is { } book && book.Buffs.Any(e => e.Definition.Effect == BuffEffect.Revive);
    }

    private LuaMaze? MazeOf(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { IsClosed: false } && maze.Members.Contains(player) ? maze : null;
    }
}
