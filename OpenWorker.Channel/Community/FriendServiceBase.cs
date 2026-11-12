using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Requests;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Friends.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Friends, requests to be friends, the block list, finding people and the list of those who look for friends. The lists
/// are in the database, so any server can change them; whoever else is affected is told through the network.
/// </summary>
public abstract class FriendServiceBase(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    FriendRecruitBoard recruits,
    OtherPersonService others
) :
    IHotspotHandler<FriendLoadRequest>,
    IHotspotHandler<FriendLoadBlocklistRequest>,
    IHotspotHandler<FriendInviteRequest>,
    IHotspotHandler<FriendInviteAcceptRequest>,
    IHotspotHandler<FriendDeleteRequest>,
    IHotspotHandler<FriendBlockAddRequest>,
    IHotspotHandler<FriendBlockDelRequest>,
    IHotspotHandler<FriendInfoRequest>,
    IHotspotHandler<FriendFindRequest>,
    IHotspotHandler<FriendRecruitListRequest>,
    IHotspotHandler<FriendRecruitAddRequest>,
    IHotspotHandler<FriendRecruitDelRequest>,
    IHotspotHandler<FriendRecruitInfoRequest>,
    IHotspotHandler<FriendRecommendListRequest>
{
    private const int MaxFound = 20;
    private const int MaxRecommended = 5;
    private const int MaxRecruits = 16;
    private const int Candidates = 40;

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendLoadRequest request)
    {
        var me = Me(context);
        var entries = await factory.ListAsync(me);
        var online = await network.FindManyAsync(entries.Select(e => e.Person).ToArray());

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        var list = entries
            .Select((e, i) => FriendBook.ToValue(e, online[i]))
            .Take(byte.MaxValue)
            .ToArray();

        Session(context).Send(new FriendLoadResponse { List = list });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendLoadBlocklistRequest request)
    {
        var blocked = await factory.BlockedAsync(Me(context));

        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new FriendLoadBlocklistResponse
            {
                List = blocked.Select(e => new FriendBlockValue(e.Person, e.Name, e.Level)).Take(byte.MaxValue).ToArray()
            });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendInviteRequest request)
    {
        var me = Me(context);
        var card = await factory.FindPersonAsync(request.Name);

        if (card is not { } target)
        {
            Fail(context, FriendOpcode.Invite, FriendMessages.NoSuchPerson);
            return;
        }

        var outcome = await factory.InviteAsync(me, target.Person);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        switch (outcome)
        {
            case InviteOutcome.Sent:
                await RequestAsync(context, target);
                break;

            case InviteOutcome.Nobody:
                Fail(context, FriendOpcode.Invite, FriendMessages.NoSuchPerson);
                break;

            case InviteOutcome.AlreadyFriend:
                Fail(context, FriendOpcode.Invite, FriendMessages.AlreadyFriend);
                break;

            case InviteOutcome.AlreadyInvited:
                Fail(context, FriendOpcode.Invite, FriendMessages.AlreadyAsked);
                break;

            case InviteOutcome.YouBlocked:
                Fail(context, FriendOpcode.Invite, FriendMessages.AlreadyBlocked);
                break;

            case InviteOutcome.TargetFull:
                Fail(context, FriendOpcode.Invite, FriendMessages.TheyHaveTooMany);
                break;

            // The one who is blocked is told it was refused, nothing more.
            case InviteOutcome.Blocked:
                Session(context).Send(new FriendInviteAcceptResponse { Result = FriendMessages.Refused, Name = target.Name });
                break;

            case InviteOutcome.YouFull:
                Session(context).Send(new FriendInviteResponse { Result = FriendMessages.YouHaveTooMany, Name = target.Name });
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendInviteAcceptRequest request)
    {
        var me = Me(context);
        var inviter = request.PersonTarget;

        if (inviter == 0 || request.TargetName.Length == 0)
        {
            Fail(context, FriendOpcode.InviteAccept, FriendMessages.NotOnServer);
            return;
        }

        if (await factory.IsBlockedAsync(me, inviter))
        {
            Fail(context, FriendOpcode.InviteAccept, FriendMessages.AlreadyBlocked);
            return;
        }

        var outcome = await factory.AnswerAsync(me, inviter, request.IsAccepted);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        switch (outcome)
        {
            case AnswerOutcome.NoRequest:
                Fail(context, FriendOpcode.InviteAccept, FriendMessages.TheyHaveTooMany);
                break;

            case AnswerOutcome.YouFull:
                Fail(context, FriendOpcode.InviteAccept, FriendMessages.YouHaveTooMany);
                break;

            case AnswerOutcome.TargetFull:
                Fail(context, FriendOpcode.InviteAccept, FriendMessages.TheyHaveTooMany);
                break;

            case AnswerOutcome.Declined:
                Session(context).Send(new FriendDeleteResponse { Friend = inviter, IsShowMessage = false });

                await network.SendAsync(inviter, new FriendInviteAcceptResponse { Result = FriendMessages.Refused, Name = myName });
                await network.SendAsync(inviter, new FriendDeleteResponse { Friend = me, IsShowMessage = false });
                break;

            case AnswerOutcome.Befriended:
                await BefriendedAsync(context, me, inviter);
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendDeleteRequest request)
    {
        var me = Me(context);

        if (!await factory.RemoveAsync(me, request.Friend) || !world.IsAlive(context.Player))
        {
            return;
        }

        Session(context).Send(new FriendDeleteResponse { Friend = request.Friend, IsShowMessage = request.IsShowMessage });

        await network.SendAsync(request.Friend, new FriendDeleteResponse { Friend = me, IsShowMessage = false });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendBlockAddRequest request)
    {
        var me = Me(context);
        var card = await factory.FindPersonAsync(request.Name);

        if (card is not { } target)
        {
            Fail(context, FriendOpcode.BlockAdd, FriendMessages.NoSuchPerson);
            return;
        }

        // A request that is still open between them goes away with the block, on both lists.
        var pending = await factory.EntryAsync(me, target.Person) is { Kind: FriendKind.Wait or FriendKind.Invite };

        var outcome = await factory.BlockAsync(me, target.Person);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        switch (outcome)
        {
            case BlockOutcome.Done:
                if (pending)
                {
                    Session(context).Send(new FriendDeleteResponse { Friend = target.Person, IsShowMessage = false });
                    await network.SendAsync(target.Person, new FriendDeleteResponse { Friend = me, IsShowMessage = false });
                }

                Session(context).Send(new FriendBlockAddResponse
                {
                    Value = new FriendBlockValue(target.Person, target.Name, target.Level)
                });
                break;

            case BlockOutcome.AlreadyBlocked:
                Fail(context, FriendOpcode.BlockAdd, FriendMessages.AlreadyBlocked);
                break;

            case BlockOutcome.Full:
                Fail(context, FriendOpcode.BlockAdd, FriendMessages.TooManyBlocked);
                break;

            case BlockOutcome.IsFriend:
                Fail(context, FriendOpcode.BlockAdd, FriendMessages.FriendsCannotBeBlocked);
                break;

            default:
                Fail(context, FriendOpcode.BlockAdd, FriendMessages.NoSuchPerson);
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendBlockDelRequest request)
    {
        var card = await factory.FindPersonAsync(request.Name);

        if (card is not { } target || !await factory.UnblockAsync(Me(context), target.Person))
        {
            Fail(context, FriendOpcode.BlockDel, FriendMessages.NoSuchPerson);
            return;
        }

        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new FriendBlockDelResponse { Person = target.Person, Name = target.Name });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendInfoRequest request)
    {
        // The friend's choice of who may look at them holds for the friends list too: "nobody" refuses a friend as well.
        await others.AskAsync(context.Player, request.Friend, OtherInfoWindow.Friend, checkOption: true, calculateStat: false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendFindRequest request)
    {
        var me = Me(context);
        var text = request.Name.Trim();

        var found = text.Length == 0 ? [] : await factory.SearchAsync(text, 0, MaxFound + 1);
        var online = await network.FindManyAsync(found.Select(e => e.Person).ToArray());

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        // The ones who asked do not find themselves.
        var values = found
            .Select((e, i) => (Card: e, Online: online[i]))
            .Where(e => e.Card.Person != me)
            .Take(MaxFound)
            .Select(e => new FriendFindEntry
            {
                Person = e.Card.Person,
                Name = e.Card.Name,
                Level = e.Online?.Level ?? e.Card.Level,
                Channel = (byte)(e.Online?.Channel ?? 0),
                World = e.Online?.Location ?? 0,
                IsLoggedIn = e.Online is not null
            })
            .ToArray();

        Session(context).Send(new FriendFindResponse { Values = values, IsLast = true });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecommendListRequest request)
    {
        var me = Me(context);
        var strangers = await factory.StrangersAsync(me, Candidates);
        var online = await network.FindManyAsync(strangers.Select(e => e.Person).ToArray());

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        // Those who are here are worth more than those who are not.
        var list = strangers
            .Select((e, i) => (Card: e, Online: online[i]))
            .OrderByDescending(e => e.Online is not null)
            .Take(MaxRecommended)
            .Select(e => new FriendRecommendValue
            {
                Name = e.Card.Name,
                Actor = new ActorValue(e.Card.Person, ActorType.User),
                Level = e.Online?.Level ?? e.Card.Level,
                Hero = (Hero)e.Card.Hero,
                Channel = (byte)(e.Online?.Channel ?? 0),
                World = e.Online?.Location ?? 0,
                IsLoggedIn = (byte)(e.Online is not null ? 1 : 0)
            })
            .ToArray();

        Session(context).Send(new FriendRecommendListResponse { Actor = world.Get<ActorComponent>(context.Player), List = list });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitListRequest request)
    {
        var me = Me(context);
        var board = await recruits.ListAsync();
        var online = await network.FindManyAsync(board.Select(e => e.Person).ToArray());

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        var list = board
            .Select((e, i) => (Recruit: e, Online: online[i]))
            .Where(e => e.Recruit.Person != me
                        && (request.Hero == 0 || e.Recruit.Hero == (byte)request.Hero)
                        && e.Online is not null
                        && InLevels(e.Online.Level, request))
            .Take(MaxRecruits)
            .Select(e => new FriendRecruitValue
            {
                Name = e.Recruit.Name,
                Person = e.Recruit.Person,
                Level = e.Online!.Level,
                Hero = (Hero)e.Recruit.Hero,
                Status = e.Recruit.State,
                Memo = e.Recruit.Memo,
                Channel = (byte)e.Online.Channel,
                World = e.Online.Location,
                IsLoggedIn = true,
                tLogOut = 0,
                tAddTime = FriendBook.ToUnix(e.Recruit.Since)
            })
            .ToArray();

        Session(context).Send(new FriendRecruitListResponse { List = list });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitAddRequest request)
    {
        var info = world.Get<PersonInfoComponent>(context.Player);
        var community = world.Get<CommunityComponent>(context.Player);

        await recruits.AddAsync(new FriendRecruit(
            world.Get<ActorComponent>(context.Player).Identifier,
            info.Name,
            (byte)info.Hero,
            community.Memo,
            (byte)community.State,
            DateTime.UtcNow));

        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new FriendRecruitAddResponse { Result = 0, Name = info.Name });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitDelRequest request)
    {
        await recruits.RemoveAsync(Me(context));

        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new FriendRecruitDelResponse { Result = 0, Name = world.Get<PersonInfoComponent>(context.Player).Name });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitInfoRequest request)
    {
        var registered = await recruits.ContainsAsync(Me(context));

        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new FriendRecruitInfoResponse { IsRegistered = registered });
        }
    }

    private static bool InLevels(byte level, FriendRecruitListRequest request)
    {
        return (request.MinLevel == 0 || level >= request.MinLevel) && (request.MaxLevel == 0 || level <= request.MaxLevel);
    }

    /// <summary>
    /// The request is made: the asker sees the other one waiting in their list, the other one sees the asker and is asked.
    /// The list entry comes first, the client looks the name up in it when the question arrives.
    /// </summary>
    private async ValueTask RequestAsync(ServiceHandleContext context, PersonCard target)
    {
        var me = Me(context);
        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        var session = Session(context);
        var hero = (byte)world.Get<PersonInfoComponent>(context.Player).Hero;
        var level = MyLevel(context);
        var community = world.Get<CommunityComponent>(context.Player);

        var targetOnline = await network.FindAsync(target.Person);
        var myOnline = await network.FindAsync(me);

        session.Send(new FriendInviteResponse { Result = 0, Name = target.Name });
        session.Send(new FriendAddResponse { Value = ValueOf(target, FriendKind.Invite, targetOnline) });

        await network.SendAsync(target.Person, new FriendAddResponse
        {
            Value = ValueOf(new PersonCard(me, myName, hero, level), FriendKind.Wait, myOnline, community)
        });

        await network.SendAsync(target.Person, new FriendInviteReqResponse { Name = myName });
    }

    /// <summary>
    /// Both are friends now. The one who answered has the entry changed, the one who asked has the waiting entry replaced by
    /// a friend, which also makes the client say so.
    /// </summary>
    private async ValueTask BefriendedAsync(ServiceHandleContext context, int me, int inviter)
    {
        var mine = await factory.EntryAsync(me, inviter);
        var theirs = await factory.EntryAsync(inviter, me);

        if (mine is not { } mineEntry || theirs is not { } theirEntry || !world.IsAlive(context.Player))
        {
            return;
        }

        var inviterOnline = await network.FindAsync(inviter);
        var myOnline = await network.FindAsync(me);

        Session(context).Send(new FriendUpdateInfoResponse { Value = FriendBook.ToValue(mineEntry, inviterOnline) });

        await network.SendAsync(inviter, new FriendDeleteResponse { Friend = me, IsShowMessage = false });
        await network.SendAsync(inviter, new FriendAddResponse { Value = FriendBook.ToValue(theirEntry, myOnline) });
    }

    private static FriendValue ValueOf(PersonCard person, FriendKind kind, PresenceRecord? online, CommunityComponent? community = null)
    {
        return FriendBook.ToValue(
            new FriendEntry(
                person.Person,
                person.Name,
                person.Hero,
                kind,
                0,
                (byte)(community?.State ?? CommunityState.LookingForParty),
                community?.Memo ?? string.Empty,
                null,
                DateTime.UtcNow,
                person.Level),
            online);
    }

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private byte MyLevel(ServiceHandleContext context) =>
        world.TryGet<LevelComponent>(context.Player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel;

    private ServerSessionComponent Session(ServiceHandleContext context) => world.Get<ServerSessionComponent>(context.Player);

    private void Fail(ServiceHandleContext context, FriendOpcode command, int code)
    {
        if (world.IsAlive(context.Player))
        {
            Session(context).Send(new ErrorMessageResponse(new MessageOpcode((byte)GroupOpcode.Friend, (byte)command), (ushort)code));
        }
    }
}
