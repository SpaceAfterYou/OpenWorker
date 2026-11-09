using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// What the members of a party hear of each other while some of them play a maze: the life of a member in the maze for
/// the others in it (12/12, once a pass), where a member who came in is (12/06, to the whole party), the window at the end
/// of the maze with every member in it, and the clear of the maze for the members who are not in it (12/46).
/// </summary>
public sealed partial class PartyMazeEvents(
    World world,
    MazeResults results,
    IPartyRoster roster,
    CommunityNetwork community,
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
    /// A member of a party came into the maze: the party learns where the member is now.
    /// </summary>
    public async ValueTask EnteredAsync(Entity player, LuaMaze maze)
    {
        var person = world.Get<ActorComponent>(player).Identifier;
        var party = await roster.PartyAsync(person);

        if (party is null || !world.IsAlive(player))
        {
            return;
        }

        var info = world.Get<PersonInfoComponent>(player);
        var place = world.Get<WorldComponent>(player);
        var health = world.Get<AbilityComponent>(player).Health;

        var update = new PartyUpdateMemberInfoResponse
        {
            Party = party.Id,
            Member = new PartyMemberInfoValue
            {
                Actor = world.Get<ActorComponent>(player),
                Name = info.Name,
                Level = world.TryGet(player, out LevelComponent level) ? level.Level : (byte)1,
                Class = info.Hero,
                Location = place.Location,
                Channel = (int)(ulong)place.Map.Channel,
                MaxHealth = health.Max,
                Health = health.Current,
                IsLoggedIn = true,
                Map = place.Map
            }
        };

        LogEntered(person, party.Id, maze.Maze);

        await community.SendAsync(party.Members.Where(e => e != person), update);
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
            _ = TellClearAsync(world.Get<ActorComponent>(members[0]).Identifier, members.Select(e => world.Get<ActorComponent>(e).Identifier).ToArray());
        }
    }

    private async Task TellClearAsync(int person, int[] inMaze)
    {
        try
        {
            if (await roster.PartyAsync(person) is not { } party)
            {
                return;
            }

            var away = party.Members.Except(inMaze).ToArray();

            if (away.Length > 0)
            {
                await community.SendAsync(away, new PartyMazeClearResponse());
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogClearNotTold(e, person);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Person {Person} of party {Party} came into maze {Maze}")]
    private partial void LogEntered(int person, int party, short maze);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The party of person {Person} could not be told that its maze is cleared")]
    private partial void LogClearNotTold(Exception exception, int person);
}
