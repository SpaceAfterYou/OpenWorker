using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Keeps the recruitment board and the awaiters true and tells those concerned: the master of a recruitment about its
/// applicants, an applicant about their applications. Recruitments whose time is up are taken down by whichever server sees
/// it first.
/// </summary>
public sealed class PartyRecruitNotifier(
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier parties,
    PartyRecruitBoard board,
    PartyAwaiterBoard awaiters,
    TimeProvider time
) :
    IPartyJoinListener,
    ICommunityListener,
    IPartyClock
{
    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    /// <summary>
    /// A recruitment as the board lists it: the party it fills and how many are in it.
    /// </summary>
    public async Task<PartyRecruitMemberValue> ValueAsync(PartyRecruit recruit)
    {
        var party = await book.GetAsync(recruit.Id);

        return new PartyRecruitMemberValue
        {
            Party = recruit.Id,
            Message = recruit.Terms.Message,
            MinLevel = recruit.Terms.MinLevel,
            MaxLevel = recruit.Terms.MaxLevel,
            Purpose = recruit.Terms.Purpose,
            MasterName = recruit.MasterName,
            MemberCount = (byte)(party?.Members.Count ?? 1),
            RemainTime = (int)Math.Max(0, recruit.Until - Now),
            PurposeLocation = recruit.Terms.Place
        };
    }

    /// <summary>
    /// An applicant as the master's list shows them, with the time left of the application.
    /// </summary>
    public async Task<PartyApplyMemberValue> ApplicantAsync(PartyApplicant applicant)
    {
        var online = await network.FindAsync(applicant.Card.Person);

        return new PartyApplyMemberValue
        {
            Member = parties.Info(applicant.Card, online),
            RegDate = (int)Math.Max(0, applicant.Until - Now)
        };
    }

    /// <summary>
    /// Takes the recruitment down: the master and the party see it go, the applicants lose their applications. A master who
    /// takes theirs down by hand waits before they put up another.
    /// </summary>
    public async Task TakeDownAsync(PartyRecruit recruit, bool penalize)
    {
        if (await board.TakeAsync(recruit.Id) is not { } taken)
        {
            return;
        }

        if (penalize)
        {
            await board.PenalizeAsync(taken.Master);
        }

        await TellGoneAsync(taken);
    }

    /// <summary>
    /// Takes the application off the recruitment: the master's list loses it, and the applicant hears of it.
    /// </summary>
    public async Task WithdrawAsync(PartyRecruit recruit, int applicant)
    {
        if (!await board.WithdrawAsync(recruit.Id, applicant))
        {
            return;
        }

        var gone = new PartyRecruitApplyDelResponse { Applicant = new ActorValue(applicant, ActorType.User), Recruit = recruit.Id };

        await network.SendAsync([recruit.Master, applicant], gone);
    }

    private async Task TellGoneAsync(PartyRecruit recruit)
    {
        var party = await book.GetAsync(recruit.Id);
        var members = party?.Persons.ToArray() ?? [];

        await network.SendAsync(members.Append(recruit.Master), new PartyRecruitDelResponse { Party = recruit.Id });

        foreach (var applicant in recruit.Applicants)
        {
            await board.WithdrawAsync(recruit.Id, applicant.Card.Person);
            await network.SendAsync(applicant.Card.Person, new PartyRecruitApplyDelResponse
            {
                Applicant = new ActorValue(applicant.Card.Person, ActorType.User),
                Recruit = recruit.Id
            });
        }
    }

    /// <summary>
    /// Somebody in a party looks for none: their applications and their entry among the awaiters go, a recruitment of their
    /// own that is not of this party goes, and a party that is full needs no recruitment.
    /// </summary>
    public async ValueTask OnJoinedAsync(PartyRecord party, int person)
    {
        await awaiters.RemoveAsync(person);

        foreach (var recruit in await board.AppliedAsync(person))
        {
            await WithdrawAsync(recruit, person);
        }

        if (await board.OfAsync(person) is { } own && own.Id != party.Id)
        {
            await TakeDownAsync(own, penalize: false);
        }

        if (await book.GetAsync(party.Id) is { IsFull: true } && await board.GetAsync(party.Id) is { } recruitment)
        {
            await TakeDownAsync(recruitment, penalize: false);
        }

        // The others of the party see the recruitment the master has up.
        else if (await board.GetAsync(party.Id) is { } shown)
        {
            await network.SendAsync(person, new PartyRecruitInfoResponse { Recruit = await ValueAsync(shown) });
        }
    }

    /// <summary>
    /// The masters of the recruitments the person applied to see where they are now.
    /// </summary>
    public async ValueTask OnEnteredAsync(Entity player, PresenceRecord person)
    {
        foreach (var recruit in await board.AppliedAsync(person.Person))
        {
            await network.SendAsync(recruit.Master, new PartyRecruitApplyUpdateResponse
            {
                Actor = new ActorValue(person.Person, ActorType.User),
                Level = person.Level,
                Location = person.Location
            });
        }
    }

    public ValueTask OnLeftAsync(Entity player, PresenceRecord person) => ValueTask.CompletedTask;

    public async Task TickAsync()
    {
        foreach (var recruit in await board.ExpireAsync())
        {
            await TellGoneAsync(recruit);
        }
    }
}
