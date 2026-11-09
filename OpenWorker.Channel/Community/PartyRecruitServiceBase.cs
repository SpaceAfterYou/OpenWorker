using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Requests;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The recruitment board (who looks for members, and who applies) and the awaiters (who looks for a party). Both live in
/// Redis for every server; an accepted application puts the applicant into the party of the recruitment.
/// </summary>
public abstract class PartyRecruitServiceBase(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier parties,
    PartyRecruitBoard board,
    PartyAwaiterBoard awaiters,
    PartyRecruitNotifier recruits,
    OtherPersonService others,
    TimeProvider time
) :
    IHotspotHandler<PartyRecruitListRequest>,
    IHotspotHandler<PartyRecruitAddRequest>,
    IHotspotHandler<PartyRecruitApplyRequest>,
    IHotspotHandler<PartyRecruitApplyAcceptRequest>,
    IHotspotHandler<PartyRecruitApplyRejectRequest>,
    IHotspotHandler<PartyRecruitDelRequest>,
    IHotspotHandler<PartyRecruitApplyListRequest>,
    IHotspotHandler<PartyRecruitApplyInfoRequest>,
    IHotspotHandler<PartyAwaiterAddRequest>,
    IHotspotHandler<PartyAwaiterDelRequest>,
    IHotspotHandler<PartyAwaiterListRequest>,
    IHotspotHandler<PartyAwaiterInfoRequest>
{
    /// <summary>
    /// The highest level a recruitment can ask for.
    /// </summary>
    public const short MaxLevel = 100;

    #region Recruitments

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitListRequest request)
    {
        var list = new List<PartyRecruitMemberValue>();

        foreach (var recruit in await board.ListAsync())
        {
            list.Add(await recruits.ValueAsync(recruit));
        }

        Send(context, new PartyRecruitListResponse { Values = list.ToArray() });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitAddRequest request)
    {
        var me = Me(context);
        var wanted = request.Member;
        var message = wanted.Message.Trim();

        if (message.Length > PartyRules.RecruitMessageLength || wanted.MinLevel < 1 || wanted.MaxLevel < wanted.MinLevel || wanted.MaxLevel > MaxLevel)
        {
            Fail(context, PartyOpcode.RecruitAdd, PartyRules.Text.Unexpected);
            return;
        }

        var party = await book.OfAsync(me);

        if (party is not null && party.Master != me)
        {
            Fail(context, PartyOpcode.RecruitAdd, PartyRules.Text.NotMaster);
            return;
        }

        if (party is { IsFull: true })
        {
            Fail(context, PartyOpcode.RecruitAdd, PartyRules.Text.Full);
            return;
        }

        // Who took their own recruitment down waits; the client shows how long from the answer.
        if (await board.PenaltyAsync(me) is { TotalSeconds: > 0 } penalty)
        {
            Send(context, new PartyRecruitAddResponse { IsSuccess = false, RemainTime = (int)Math.Ceiling(penalty.TotalSeconds), Member = wanted });
            return;
        }

        if (!Alive(context))
        {
            return;
        }

        var name = parties.CardOf(context.Player).Name;
        var id = party?.Id ?? (await board.OfAsync(me))?.Id ?? await book.ReserveAsync();
        var terms = new PartyRecruitTerms(message, wanted.MinLevel, wanted.MaxLevel, wanted.Purpose, wanted.PurposeLocation);
        var recruit = await recruits.ValueAsync(await board.PutAsync(id, me, name, terms));

        Send(context, new PartyRecruitAddResponse { IsSuccess = true, RemainTime = recruit.RemainTime, Member = recruit });

        if (party is not null)
        {
            await network.SendAsync(party.Persons.Where(e => e != me), new PartyRecruitInfoResponse { Recruit = recruit });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyRequest request)
    {
        var me = Me(context);

        if (await book.OfAsync(me) is not null)
        {
            Fail(context, PartyOpcode.RecruitApply, PartyRules.Text.InParty);
            return;
        }

        if (await board.GetAsync(request.Recruit.Identifier) is not { } recruit || recruit.Master == me || !Alive(context))
        {
            Send(context, new PartyRecruitApplyResponse { ErrorCode = PartyApplyError.PartyRecruit, ApplyCount = (byte)(await board.AppliedAsync(me)).Count });
            return;
        }

        var card = parties.CardOf(context.Player);
        var (outcome, applications) = await board.ApplyAsync(recruit.Id, card);

        Send(context, new PartyRecruitApplyResponse { ErrorCode = outcome, ApplyCount = (byte)applications });

        if (outcome == PartyApplyError.Success && (await board.GetAsync(recruit.Id))?.Applicant(me) is { } applicant)
        {
            await network.SendAsync(recruit.Master, new PartyRecruitApplyNoticeResponse { Member = await recruits.ApplicantAsync(applicant) });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyAcceptRequest request)
    {
        var me = Me(context);
        var applicant = request.Actor.Identifier;

        if (await board.OfAsync(me) is not { } recruit)
        {
            Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.NoRecruit);
            return;
        }

        if (recruit.Applicant(applicant) is null)
        {
            Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.NoApplicant);
            return;
        }

        if (await network.FindAsync(applicant) is not { } online)
        {
            await recruits.WithdrawAsync(recruit, applicant);
            Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.NoMember);
            return;
        }

        if (!Alive(context))
        {
            return;
        }

        var outcome = await parties.JoinAsync(parties.CardOf(context.Player), parties.CardOf(online), keep: recruit.Id);

        switch (outcome)
        {
            case PartyJoinOutcome.Created or PartyJoinOutcome.Joined:
                Send(context, new PartyRecruitApplyAcceptResponse { Actor = new ActorValue(applicant, ActorType.User) });
                break;

            case PartyJoinOutcome.InParty:
                await recruits.WithdrawAsync(recruit, applicant);
                Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.InParty);
                break;

            case PartyJoinOutcome.Full:
                Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.Full);
                break;

            default:
                Fail(context, PartyOpcode.RecruitApplyAccept, PartyRules.Text.NotMaster);
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyRejectRequest request)
    {
        var applicant = request.Actor.Identifier;

        if (await board.OfAsync(Me(context)) is not { } recruit || recruit.Applicant(applicant) is null)
        {
            Fail(context, PartyOpcode.RecruitApplyReject, PartyRules.Text.NoApplicant);
            return;
        }

        await recruits.WithdrawAsync(recruit, applicant);

        Send(context, new PartyRecruitApplyRejectResponse { Actor = new ActorValue(applicant, ActorType.User) });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitDelRequest request)
    {
        if (await board.OfAsync(Me(context)) is not { } recruit)
        {
            Fail(context, PartyOpcode.RecruitDel, PartyRules.Text.NoRecruit);
            return;
        }

        // The master hears it with the party, from the board.
        await recruits.TakeDownAsync(recruit, penalize: true);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyListRequest request)
    {
        var recruit = await board.OfAsync(Me(context));
        var list = new PartyApplyMemberValue[PartyModuleDefines.RecruitAppliesCount];
        var applicants = recruit?.Applicants ?? [];

        for (var i = 0; i < list.Length; i++)
        {
            list[i] = i < applicants.Count
                ? await recruits.ApplicantAsync(applicants[i])
                : new PartyApplyMemberValue { Member = PartyMemberInfoValue.Empty, RegDate = 0 };
        }

        Send(context, new PartyRecruitApplyListResponse { ValueList = list });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyInfoRequest request)
    {
        // A recruitment that is gone is answered with no party, after which the client reads nothing more.
        if (await board.GetAsync(request.Recruit.Identifier) is not { } recruit)
        {
            Send(context, new PartyRecruitApplyInfoResponse { Party = 0, MemberList = [], Actor = default });
            return;
        }

        PartyMemberInfoValue[] members;

        if (await book.GetAsync(recruit.Id) is { } party)
        {
            members = await parties.InfosAsync(party);
        }

        else
        {
            var online = await network.FindAsync(recruit.Master);

            members = [parties.Info(online is null ? new PartyMemberCard(recruit.Master, recruit.MasterName, 0, 0) : parties.CardOf(online), online)];
        }

        Send(context, new PartyRecruitApplyInfoResponse
        {
            Party = recruit.Id,
            MemberList = members,
            Actor = new ActorValue(recruit.Master, ActorType.User)
        });
    }

    #endregion

    #region Awaiters

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterAddRequest request)
    {
        if (await book.OfAsync(Me(context)) is not null)
        {
            Fail(context, PartyOpcode.AwaiterAdd, PartyRules.Text.InParty);
            return;
        }

        if (!Alive(context))
        {
            return;
        }

        var awaiter = await awaiters.AddAsync(parties.CardOf(context.Player));

        Send(context, new PartyAwaiterAddResponse { Awaiter = Value(awaiter) });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterDelRequest request)
    {
        await awaiters.RemoveAsync(Me(context));

        if (Alive(context))
        {
            Send(context, new PartyAwaiterDelResponse { Actor = new ActorValue(Me(context), ActorType.User) });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterListRequest request)
    {
        var list = await awaiters.ListAsync();

        Send(context, new PartyAwaiterListResponse { List = list.Select(Value).ToArray() });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterInfoRequest request)
    {
        await others.AskAsync(context.Player, request.Actor.Identifier, OtherInfoWindow.Awaiter, checkOption: false, calculateStat: request.Select != 0);
    }

    private PartyAwaiterValue Value(PartyAwaiter awaiter) => new()
    {
        Actor = new ActorValue(awaiter.Card.Person, ActorType.User),
        Name = awaiter.Card.Name,
        Level = awaiter.Card.Level,
        Class = (Hero)awaiter.Card.Hero,
        RemainTime = (int)Math.Max(0, awaiter.Until - time.GetUtcNow().ToUnixTimeSeconds()),
        Unknown1 = 0,
        Unknown2 = 0
    };

    #endregion

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private bool Alive(ServiceHandleContext context) => world.IsAlive(context.Player);

    private void Send(ServiceHandleContext context, IResponseHotspotMessage message)
    {
        if (Alive(context))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(message);
        }
    }

    private void Fail(ServiceHandleContext context, PartyOpcode command, int code)
    {
        Send(context, new ErrorMessageResponse(new MessageOpcode((byte)GroupOpcode.Party, (byte)command), (ushort)code));
    }
}
