using System.Text;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Gives the client the whole league of a player, and builds the pieces of it.
/// </summary>
public sealed class LeagueSnapshots(IDbContextFactory<PersistenceContext> factory, CommunityNetwork network)
{
    /// <summary>
    /// The league of the person with everything in it, or null if they are in none. The applicants are for those who may
    /// answer them.
    /// </summary>
    public async Task<LeagueInfoResponse?> InfoAsync(int person, bool joined)
    {
        var member = await factory.MemberAsync(person);
        var league = member is null ? null : await factory.LeagueAsync(member.League);

        if (member is null || league is null)
        {
            return null;
        }

        var members = await factory.MembersAsync(league.Id);
        var online = await network.FindManyAsync(members.Select(e => e.Person).ToArray());

        var applicants = league.Allows(member.Position, LeagueRight.Join)
            ? await factory.ApplicantsAsync(league.Id)
            : [];

        var board = await factory.BoardAsync(league.Id);

        return new LeagueInfoResponse
        {
            Info = LeagueMapper.ToInfo(league),
            Members = members.Select((e, i) => LeagueMapper.ToMember(e, online[i])).Take(byte.MaxValue).ToArray(),
            Applicants = applicants.Select(LeagueMapper.ToApplicant).Take(byte.MaxValue).ToArray(),
            Board = board.Select(LeagueMapper.ToBoard).Take(byte.MaxValue).ToArray(),
            Joined = joined
        };
    }

    public static LeagueMemberUpdateResponse Update(LeagueMemberRecord member, PresenceRecord? online)
    {
        return new LeagueMemberUpdateResponse
        {
            League = member.League,
            Actor = member.Person,
            Login = (byte)(online is null ? 0 : 1),
            Level = online?.Level ?? member.Level,
            World = online?.Location ?? 0,
            PlayDate = online is null ? LeagueMapper.ToUnix(DateTime.UtcNow) : 0,
            Name = member.Name,
            Channel = (byte)(online?.Channel ?? 0)
        };
    }
}

/// <summary>
/// Keeps what a server shows of a league true: the league above the head of a player, and the members who see each
/// other come and go.
/// </summary>
public sealed class LeagueNotifier(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    LeagueSnapshots snapshots,
    ServiceChannels channels
) :
    ICommunityListener,
    ICommunityModule
{
    public const string SyncEvent = "league.sync";

    public void Attach(CommunityNetwork network)
    {
        network.On(SyncEvent, OnSyncAsync);
    }

    /// <summary>
    /// Has the servers of the persons record which league they belong to now, null for none, and show it to those around.
    /// </summary>
    public ValueTask SyncAsync(IEnumerable<int> persons, LeagueRecord? league)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(league?.Id ?? 0);
        writer.Write(league?.Name ?? string.Empty);
        writer.Write(league is null ? 0 : LeagueMapper.CardOf(league));
        writer.Write(league?.Emblem ?? (short)0);
        writer.Write(league?.Border ?? (short)0);
        writer.Flush();

        return network.PublishAsync(persons, SyncEvent, stream.ToArray());
    }

    public async ValueTask OnEnteredAsync(Entity player, PresenceRecord person)
    {
        var member = await factory.MemberAsync(person.Person);

        if (member is null || !world.IsAlive(player))
        {
            return;
        }

        var info = await snapshots.InfoAsync(person.Person, joined: false);

        if (info is not { } full || !world.IsAlive(player))
        {
            return;
        }

        var league = await factory.LeagueAsync(member.League);

        if (league is null)
        {
            return;
        }

        Apply(player, league.Id, league.Name, LeagueMapper.CardOf(league), league.Emblem, league.Border);

        world.Get<ServerSessionComponent>(player).Send(full);

        await Others(member.League, person.Person, LeagueSnapshots.Update(member, person));
    }

    public async ValueTask OnLeftAsync(Entity player, PresenceRecord person)
    {
        var member = await factory.MemberAsync(person.Person);

        if (member is not null)
        {
            await Others(member.League, person.Person, LeagueSnapshots.Update(member, null));
        }
    }

    private async ValueTask Others<T>(int league, int except, T message) where T : IResponseHotspotMessage
    {
        var members = await factory.MemberIdsAsync(league);

        await network.SendAsync(members.Where(e => e != except), message);
    }

    private ValueTask OnSyncAsync(CommunityEvent value)
    {
        using var stream = new MemoryStream(value.Body.ToArray());
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        var league = reader.ReadInt32();
        var name = reader.ReadString();
        var card = reader.ReadInt32();
        var emblem = reader.ReadInt16();
        var border = reader.ReadInt16();

        foreach (var person in value.Persons)
        {
            if (network.TryGet(person, out var player))
            {
                Apply(player, league, name, card, emblem, border);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player belongs to the league now, and those close by see its name.
    /// </summary>
    private void Apply(Entity player, int league, string name, int card, short emblem, short border)
    {
        world.Set(player, new LeagueComponent
        {
            Id = league,
            Name = name,
            Card = new LeagueComponentCard { Emblem = emblem, Border = border }
        });

        var alert = new LeagueInfoAlertResponse
        {
            Actor = world.Get<ActorComponent>(player).Identifier,
            League = league,
            Card = card,
            Name = name
        };

        // A maze has no channels, every entity only carries the component.
        if (channels.Count > 0 && world.Has<ChannelMemberComponent>(player) && channels.Get(player).Contains(player))
        {
            channels.Get(player).BroadcastNearby(player, alert);
        }

        else
        {
            world.Get<ServerSessionComponent>(player).Send(alert);
        }
    }
}
