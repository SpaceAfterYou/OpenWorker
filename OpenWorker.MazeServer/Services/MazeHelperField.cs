using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The maze as the place helpers are called to. A call is checked as the game checks it (the person on its feet, ten
/// seconds since its last call, in a maze, a maze that takes helpers, a helper of its own that is not called yet, room for
/// one more body); the helper then appears in the maze of the person, which everybody in it sees. Sending away, calling
/// back and the automatic call after the maze has loaded go through here too.
/// </summary>
public sealed partial class MazeHelperField(
    World world,
    HelperGear gear,
    HelperStats stats,
    IPartyRoster parties,
    ILogger<MazeHelperField> logger
) : IHelperField
{
    public async ValueTask SummonAsync(Entity player, int helper, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var party = await PartySizeAsync(player);

        if (!world.IsAlive(player))
        {
            return;
        }

        var maze = MazeOf(player);
        var inMaze = maze is not null;
        var code = HelperRules.CheckSummon(new HelperSummonCase(
            IsUp(player, maze),
            inMaze ? maze!.Helpers.SinceLastSummon(player) : TimeSpan.MaxValue,
            inMaze,
            inMaze && !maze!.RefusesHelpers,
            EntryOf(player, helper) is not null,
            inMaze && maze!.Helpers.Find(player, helper) is not null,
            inMaze ? maze!.Helpers.Count : 0,
            party));

        switch (code)
        {
            case null:
                return;

            case HelperRules.Allowed:
                if (!Summon(player, maze!, helper))
                {
                    SendError(player, HelperManager.CannotSummon);
                }

                return;

            default:
                SendError(player, code.Value);
                return;
        }
    }

    public void Release(Entity player, int helper)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var maze = MazeOf(player);
        var code = HelperRules.CheckRelease(IsUp(player, maze), maze is not null, maze?.Helpers.Find(player, helper) is not null);

        switch (code)
        {
            case null:
                return;

            case HelperRules.Allowed:
                maze!.ReleaseHelper(player, helper);
                return;

            default:
                SendError(player, code.Value);
                return;
        }
    }

    public void Recall(Entity player)
    {
        if (world.IsAlive(player) && MazeOf(player) is { } maze && maze.Creatures.Vitals.IsAlive(player))
        {
            maze.RecallHelpers(player);
        }
    }

    /// <summary>
    /// The maze has loaded for the person: alone and with the automatic call on, in a maze that takes helpers, its helpers
    /// come in the order of its list for as long as there is room. The delay between calls does not count here.
    /// </summary>
    public async ValueTask AutoSummonAsync(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet<HelperComponent>(player, out var component)
            || component.Book is not { AutoSummon: true, Helpers.Count: > 0 } book)
        {
            return;
        }

        if (await PartySizeAsync(player) > 1 || !world.IsAlive(player) || MazeOf(player) is not { RefusesHelpers: false } maze)
        {
            return;
        }

        foreach (var entry in book.Helpers.OrderBy(e => e.Order).ToArray())
        {
            if (!HelperRules.HasRoom(maze.Helpers.Count, 1))
            {
                return;
            }

            if (maze.Helpers.Find(player, entry.Helper) is null)
            {
                Summon(player, maze, entry.Helper);
            }
        }
    }

    /// <summary>
    /// The helper as the person has it, or nothing when it does not have it.
    /// </summary>
    private HelperEntry? EntryOf(Entity player, int helper)
    {
        return world.TryGet<HelperComponent>(player, out var component) ? component.Book?.Find(helper) : null;
    }

    /// <summary>
    /// The helper comes with its record as called, which carries what it wears and the orb of a friend, and it fights with
    /// what it has by itself at its owner's level and what those add.
    /// </summary>
    private bool Summon(Entity player, LuaMaze maze, int helper)
    {
        if (EntryOf(player, helper) is not { } entry)
        {
            return false;
        }

        var record = gear.Record(player, entry, summoned: true);

        return maze.SummonHelper(player, MazeSummonedHelpers.CombatOf(world, stats, player, record), record) is not null;
    }

    private LuaMaze? MazeOf(Entity player)
    {
        return world.TryGet(player, out LuaMaze? maze) && maze is { IsClosed: false } && maze.Members.Contains(player) ? maze : null;
    }

    private bool IsUp(Entity player, LuaMaze? maze)
    {
        return maze?.Creatures.Vitals.IsAlive(player) ?? world.IsAlive(player);
    }

    /// <summary>
    /// How many people the party of the person has, one for a person alone. When the parties cannot be read, the players
    /// of its maze count instead.
    /// </summary>
    private async ValueTask<int> PartySizeAsync(Entity player)
    {
        var person = world.Get<ActorComponent>(player).Identifier;

        try
        {
            return await parties.PartyAsync(person) is { } party ? Math.Max(1, party.Members.Count) : 1;
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogPartyUnknown(e, person);

            return world.IsAlive(player) && MazeOf(player) is { } maze ? maze.MemberCount : 1;
        }
    }

    private void SendError(Entity player, ushort code)
    {
        world.Get<ServerSessionComponent>(player)
            .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Helper, HelperOpcode.Summon), code));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The party of person {Person} could not be read; the players of its maze are counted for its helpers")]
    private partial void LogPartyUnknown(Exception exception, int person);
}

/// <summary>
/// Which helpers of a person stand in its maze now: those are told to the client as called, fight with what they wear after
/// every change of it, and give back the orbs of friends when the maze is cleared.
/// </summary>
public sealed class MazeSummonedHelpers(World world, HelperStats stats) : ISummonedHelpers
{
    public bool IsSummoned(Entity player, int helper)
    {
        return MazeOf(player) is { } maze && maze.Helpers.Find(player, helper) is not null;
    }

    public void Refit(Entity player, in HelperValue record)
    {
        MazeOf(player)?.RefitHelper(player, CombatOf(world, stats, player, record), record);
    }

    /// <summary>
    /// What a called helper fights with: what it has by itself at its owner's level, and what its record says its items and
    /// orb add.
    /// </summary>
    public static CombatStats CombatOf(World world, HelperStats stats, Entity player, in HelperValue record)
    {
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;

        return stats.Combat(record.Helper, level, record.Additional ?? []);
    }

    private LuaMaze? MazeOf(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { IsClosed: false } ? maze : null;
    }
}
