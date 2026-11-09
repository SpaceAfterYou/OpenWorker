using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What else has to happen when somebody joins a party: an applicant's applications and an awaiter's entry go.
/// </summary>
public interface IPartyJoinListener
{
    ValueTask OnJoinedAsync(PartyRecord party, int person);
}

/// <summary>
/// Changes parties and tells their members, wherever they are: the district and the maze servers both run it, and a message
/// for a member on another server goes through the <see cref="CommunityNetwork"/>. The maze sends what it has to say to a
/// party (the health of a member, a cleared maze) through <see cref="SendAsync"/> and <see cref="SendToOthersAsync"/>.
/// </summary>
public sealed partial class PartyNotifier(
    World world,
    CommunityNetwork network,
    PartyBook book,
    TimeProvider time,
    IServiceProvider services,
    ILogger<PartyNotifier> logger
) :
    ICommunityListener
{
    private IEnumerable<IPartyJoinListener> JoinListeners => services.GetServices<IPartyJoinListener>();

    #region For other parts of the game

    /// <summary>
    /// Sends the message to everybody in the party of the person, the person too. Nothing when they are alone.
    /// </summary>
    public async ValueTask SendAsync(int person, IResponseHotspotMessage message)
    {
        if (await book.OfAsync(person) is { } party)
        {
            await network.SendAsync(party.Persons, message);
        }
    }

    /// <summary>
    /// Sends the message to the others in the party of the person.
    /// </summary>
    public async ValueTask SendToOthersAsync(int person, IResponseHotspotMessage message)
    {
        if (await book.OfAsync(person) is { } party)
        {
            await network.SendAsync(party.Persons.Where(e => e != person), message);
        }
    }

    /// <summary>
    /// The health of the member changed: the others see it on their party list.
    /// </summary>
    public ValueTask MemberHealthAsync(int person, int health, int maxHealth)
    {
        return SendToOthersAsync(person, new PartyUpdateMemberHpResponse
        {
            Actor = new ActorValue(person, ActorType.User),
            Health = health,
            MaxHealth = maxHealth
        });
    }

    /// <summary>
    /// The maze of the party is cleared.
    /// </summary>
    public ValueTask MazeClearAsync(int person) => SendAsync(person, new PartyMazeClearResponse());

    /// <summary>
    /// What a party shows of the player who is connected here.
    /// </summary>
    public PartyMemberCard CardOf(Entity player)
    {
        var info = world.Get<PersonInfoComponent>(player);
        var level = world.Has<LevelComponent>(player) ? world.Get<LevelComponent>(player).Level : PersonModuleDefines.DefaultLevel;

        return new PartyMemberCard(world.Get<ActorComponent>(player).Identifier, info.Name, (byte)info.Hero, level);
    }

    /// <summary>
    /// What a party shows of somebody online on any server.
    /// </summary>
    public PartyMemberCard CardOf(PresenceRecord person)
    {
        return network.TryGet(person.Person, out var player)
            ? CardOf(player)
            : new PartyMemberCard(person.Person, person.Name, person.Hero, person.Level);
    }

    /// <summary>
    /// The members as the client lists them, with where they are now.
    /// </summary>
    public async Task<PartyMemberInfoValue[]> InfosAsync(PartyRecord party)
    {
        var online = await network.FindManyAsync(party.Persons.ToArray());

        return party.Members.Select((e, i) => Info(e.Card, online[i])).ToArray();
    }

    public PartyMemberInfoValue Info(PartyMemberCard card, PresenceRecord? online)
    {
        var health = 1;
        var maxHealth = 1;

        if (network.TryGet(card.Person, out var player) && world.Has<AbilityComponent>(player))
        {
            var ability = world.Get<AbilityComponent>(player);

            health = ability.Health.Current;
            maxHealth = ability.Health.Max;
        }

        var location = online?.Location ?? 0;
        var channel = online?.Channel ?? 0;

        return new PartyMemberInfoValue
        {
            Actor = new ActorValue(card.Person, ActorType.User),
            Name = card.Name,
            Level = card.Level,
            Class = (Hero)card.Hero,
            Location = location,
            Channel = channel,
            MaxHealth = maxHealth,
            Health = health,
            IsLoggedIn = online is not null,
            Map = new MapValue { Channel = channel, Location = location, Server = 0 }
        };
    }

    /// <summary>
    /// The whole party, as the client rebuilds its list from it.
    /// </summary>
    public async Task<PartyUpdateInfoResponse> ListAsync(PartyRecord party, PartyUpdateType update)
    {
        return new PartyUpdateInfoResponse
        {
            Party = party.Id,
            Master = new ActorValue(party.Master, ActorType.User),
            Maze = default,
            UpdateType = update,
            Type = party.Type,
            InfoList = await InfosAsync(party)
        };
    }

    #endregion

    #region Changes

    /// <summary>
    /// Puts the member into the master's party, or makes a party of the two. The members hear of it.
    /// </summary>
    public async Task<PartyJoinOutcome> JoinAsync(PartyMemberCard master, PartyMemberCard member, int keep = 0)
    {
        var (outcome, id) = await book.JoinAsync(master, member, keep);

        if (outcome is not (PartyJoinOutcome.Created or PartyJoinOutcome.Joined) || await book.GetAsync(id) is not { } party)
        {
            return outcome;
        }

        if (outcome == PartyJoinOutcome.Created)
        {
            await network.SendAsync(party.Persons, await ListAsync(party, PartyUpdateType.Create));
        }

        else
        {
            var online = await network.FindAsync(member.Person);

            await network.SendAsync(party.Persons.Where(e => e != member.Person), new PartyAddMemberResponse { Info = Info(member, online) });
            await network.SendAsync(member.Person, await ListAsync(party, PartyUpdateType.AddMember));
        }

        foreach (var person in outcome == PartyJoinOutcome.Created ? party.Persons : [member.Person])
        {
            await OnJoinedAsync(party, person);
        }

        return outcome;
    }

    /// <summary>
    /// Makes a party of the people an auto matching brought together, the first its master. Zero when one of them found a
    /// party meanwhile.
    /// </summary>
    public async Task<int> CreateAsync(IReadOnlyList<PartyMemberCard> members, PartyType type)
    {
        var id = await book.CreateAsync(members, type);

        if (id == 0 || await book.GetAsync(id) is not { } party)
        {
            return 0;
        }

        await network.SendAsync(party.Persons, await ListAsync(party, PartyUpdateType.AutoMatching));

        foreach (var person in party.Persons)
        {
            await OnJoinedAsync(party, person);
        }

        return id;
    }

    /// <summary>
    /// The person leaves their party, or is kicked out of it. A master who leaves hands it over to the member with the lowest
    /// number; the second to last who leaves ends it. False when they were in no party.
    /// </summary>
    public async Task<bool> LeaveAsync(int person, bool kicked)
    {
        var leave = await book.LeaveAsync(person);

        if (!leave.Left || leave.Before is not { } before)
        {
            return false;
        }

        var everybody = before.Persons.ToArray();
        var rest = everybody.Where(e => e != person).ToArray();

        if (!leave.Deleted && before.Master != leave.Master)
        {
            await network.SendAsync(rest, new PartyChangeMasterResponse { Master = new ActorValue(leave.Master, ActorType.User) });
        }

        await network.SendAsync(everybody, new PartyLeaveResponse
        {
            Identifier = before.Id,
            Member = new ActorValue(person, ActorType.User),
            IsKickOut = kicked
        });

        if (leave.Deleted)
        {
            await network.SendAsync(everybody, new PartyDeleteResponse { Member = PartyMemberInfoValue.Empty });
        }

        return true;
    }

    /// <summary>
    /// Makes the member the master. False when the one who asks is not the master or the other no member.
    /// </summary>
    public async Task<bool> HandOverAsync(PartyRecord party, int next)
    {
        if (!await book.HandOverAsync(party.Id, party.Master, next))
        {
            return false;
        }

        await network.SendAsync(party.Persons, new PartyChangeMasterResponse { Master = new ActorValue(next, ActorType.User) });

        return true;
    }

    private async ValueTask OnJoinedAsync(PartyRecord party, int person)
    {
        foreach (var listener in JoinListeners)
        {
            try
            {
                await listener.OnJoinedAsync(party, person);
            }

            catch (Exception e)
            {
                LogListenerFailed(logger, e, person);
            }
        }
    }

    #endregion

    #region Coming and going

    public async ValueTask OnEnteredAsync(Entity player, PresenceRecord person)
    {
        if (await book.OfAsync(person.Person) is not { } party)
        {
            return;
        }

        var me = party.Members.First(e => e.Person == person.Person);

        // Gone for longer than a move between servers takes: they left the party in between.
        if (me.OfflineSince is { } since && Now - since > (long)PartyRules.MemberGrace.TotalSeconds)
        {
            await LeaveAsync(person.Person, kicked: false);
            return;
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        var card = CardOf(player);

        await book.MarkOnlineAsync(party.Id, card);

        if (await book.GetAsync(party.Id) is not { } now || !now.Has(person.Person))
        {
            return;
        }

        await network.SendAsync(person.Person, await ListAsync(now, PartyUpdateType.None));

        await network.SendAsync(now.Persons.Where(e => e != person.Person), new PartyUpdateMemberInfoResponse
        {
            Party = now.Id,
            Member = Info(card, person)
        });
    }

    public async ValueTask OnLeftAsync(Entity player, PresenceRecord person)
    {
        if (await book.OfAsync(person.Person) is not { } party)
        {
            return;
        }

        await book.MarkOfflineAsync(party.Id, person.Person);

        var card = party.Members.First(e => e.Person == person.Person).Card;

        await network.SendAsync(party.Persons.Where(e => e != person.Person), new PartyUpdateMemberInfoResponse
        {
            Party = party.Id,
            Member = Info(card, online: null)
        });
    }

    /// <summary>
    /// Keeps the parties of the people of this server alive, and lets go of their members who have not been online anywhere
    /// for longer than <see cref="PartyRules.MemberGrace"/>. Another server may do the same for the same party: the book
    /// lets only one of them take a member out.
    /// </summary>
    public async Task UpkeepAsync()
    {
        var here = network.Online
            .Where(world.IsAlive)
            .Select(e => world.Get<ActorComponent>(e).Identifier)
            .ToArray();

        await book.RenewAsync(here);

        var seen = new HashSet<int>();

        foreach (var person in here)
        {
            if (await book.OfAsync(person) is not { } party || !seen.Add(party.Id))
            {
                continue;
            }

            var online = await network.FindManyAsync(party.Persons.ToArray());

            foreach (var (member, presence) in party.Members.Zip(online))
            {
                if (presence is not null)
                {
                    continue;
                }

                if (member.OfflineSince is null)
                {
                    await book.MarkOfflineAsync(party.Id, member.Person);
                }

                else if (Now - member.OfflineSince > (long)PartyRules.MemberGrace.TotalSeconds)
                {
                    await LeaveAsync(member.Person, kicked: false);
                }
            }
        }
    }

    #endregion

    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not finish the joining of person {Person} to a party.")]
    private static partial void LogListenerFailed(ILogger logger, Exception exception, int person);
}
