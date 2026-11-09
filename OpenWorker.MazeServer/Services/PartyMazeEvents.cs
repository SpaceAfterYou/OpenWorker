using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// What the members of a party hear of each other while some of them play a maze: the life of a member in the maze for
/// the others in it (12/12, once a pass), the window at the end of the maze with every member in it, and the clear of the
/// maze for the party (12/46). Where a member is (12/06) the party tells by itself when the member comes into a server.
/// </summary>
public sealed partial class PartyMazeEvents(
    World world,
    MazeResults results,
    PartyNotifier parties,
    ILogger<PartyMazeEvents> logger
) : IEndOfPass
{
    private readonly List<Entity> _hurt = [];
    private bool _waiting;

    /// <summary>
    /// The life of a player in a maze has changed. The others of the party in it are told at the end of the pass, once
    /// however often it changed; a player alone tells nobody.
    /// </summary>
    public void HealthChanged(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out LuaMaze? maze) || maze is not { MemberCount: > 1 })
        {
            return;
        }

        if (SynchronizationContext.Current is not WorldLoop { IsCurrent: true } loop)
        {
            SendHealth(player);

            return;
        }

        if (!_hurt.Contains(player))
        {
            _hurt.Add(player);
        }

        if (!_waiting)
        {
            _waiting = true;
            loop.AtEndOfPass(this);
        }
    }

    void IEndOfPass.EndOfPass()
    {
        _waiting = false;

        foreach (var player in _hurt)
        {
            SendHealth(player);
        }

        _hurt.Clear();
    }

    private void SendHealth(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out LuaMaze? maze) || maze is null || !world.Has<AbilityComponent>(player))
        {
            return;
        }

        var health = world.Get<AbilityComponent>(player).Health;

        maze.Creatures.BroadcastToOthers(player, new PartyUpdateMemberHpResponse
        {
            Actor = world.Get<ActorComponent>(player),
            Health = health.Current,
            MaxHealth = health.Max
        });
    }

    /// <summary>
    /// The maze is won: every member in it gets the window with the whole party in it, and the members who are elsewhere
    /// hear that their party has cleared a maze.
    /// </summary>
    public void Completed(LuaMaze maze)
    {
        var members = maze.Members.Where(world.IsAlive).ToArray();

        foreach (var member in members)
        {
            results.Complete(member, members);
        }

        if (maze.Party != 0 && members.Length > 0)
        {
            _ = TellClearAsync(world.Get<ActorComponent>(members[0]).Identifier);
        }
    }

    private async Task TellClearAsync(int person)
    {
        try
        {
            await parties.MazeClearAsync(person);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogClearNotTold(e, person);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The party of person {Person} could not be told that its maze is cleared")]
    private partial void LogClearNotTold(Exception exception, int person);
}
