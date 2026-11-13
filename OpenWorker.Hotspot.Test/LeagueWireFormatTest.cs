using System.Text;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Requests;
using OpenWorker.Hotspot.Modules.League.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Sizes and places below are what the client reads of the league messages, not what they happen to write.
/// </summary>
[TestClass]
public sealed class LeagueWireFormatTest
{
    // Two bytes of length, UTF-16 and a terminator of one byte.
    private static int TextSize(string value) => 2 + 2 * value.Length + 1;

    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

    private static BinaryReader Request(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();
        stream.Position = 0;

        return new BinaryReader(stream);
    }

    private static void Text(BinaryWriter writer, string value)
    {
        writer.Write((short)(2 * value.Length + 1));
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((byte)0);
    }

    private static LeagueInfo Info() => new()
    {
        Identifier = 5,
        Name = "Ab",
        MasterName = "Cd",
        ViceMasterName = string.Empty,
        Notice = "Hi",
        RecruitNotice = "Join",
        PositionList = ["One", "Two", string.Empty],
        RewardList = Enumerable.Range(0, 9).Select(i => new LeagueRewardInfo { Auth = i, LimitGoldOut = i * 2 }).ToList()
    };

    // The league the way the client reads it: ids, counts, the name, three dates, the master, nine pairs and the card, the
    // notice, three positions, a flag, the call for members and its date.
    private static int InfoSize() =>
        4 + 4 + 1 + 1 + 2 + 8 + TextSize("Ab") + 8 + 8 + 8 + 4 + TextSize("Cd") + TextSize(string.Empty) + 9 * 8 + 4 +
        TextSize("Hi") + TextSize("One") + TextSize("Two") + TextSize(string.Empty) + 1 + TextSize("Join") + 8;

    private static LeagueMember Member() => new()
    {
        Name = "Eve",
        Person = 9,
        LeagueInfo = new LeagueInfoForMember { League = 5, Position = OpenWorker.Hotspot.Modules.League.Enums.LeaguePosition.LeagueMaster }
    };

    // The member: the league, the position and three dates, a flag, the map, the channel, the number, the name, the level,
    // a time, the class and a date.
    private static int MemberSize() => 4 + 1 + 3 * 8 + 1 + 2 + 1 + 4 + TextSize("Eve") + 2 + 8 + 1 + 8;

    private static LeagueApplicant Applicant(int result) => new() { League = 5, Actor = 9, Name = "Fay", Level = 25, Date = 1000, Class = 3, Result = result };

    private static int ApplicantSize() => 4 + 4 + TextSize("Fay") + 2 + 8 + 1 + 4;

    [TestMethod]
    public void TheLeagueHasTheClientLayout()
    {
        var bytes = Written(new LeagueCreateResponse { Info = Info(), Member = Member() });

        Assert.HasCount(InfoSize() + MemberSize(), bytes);
        Assert.AreEqual(5, BitConverter.ToInt32(bytes, 0));
    }

    [TestMethod]
    public void TheWholeLeagueIsTheLeagueThenFourListsAndAFlag()
    {
        var bytes = Written(new LeagueInfoResponse
        {
            Info = Info(),
            Members = [Member(), Member()],
            Applicants = [Applicant(0)],
            Board = [new LeagueBoardEntry { Name = "Gus", Message = "Hello" }],
            Joined = true
        });

        var board = 4 + 4 + TextSize("Gus") + TextSize("Hello") + 8 + 4;

        Assert.HasCount(InfoSize() + 1 + 2 * MemberSize() + 1 + ApplicantSize() + 1 + board + 1, bytes);
        Assert.AreEqual(2, bytes[InfoSize()]);
        Assert.AreEqual(1, bytes[^1]);
    }

    [TestMethod]
    public void AnAnswerToAnApplicationHasTheTimeToWaitOnlyForThePenalty()
    {
        Assert.HasCount(ApplicantSize(), Written(new LeagueApplicantResResponse { Applicant = Applicant(0), Remain = 999 }));
        Assert.HasCount(ApplicantSize(), Written(new LeagueApplicantResResponse { Applicant = Applicant(57017), Remain = 999 }));

        var bytes = Written(new LeagueApplicantResResponse { Applicant = Applicant(57025), Remain = 999 });

        Assert.HasCount(ApplicantSize() + 8, bytes);
        Assert.AreEqual(999L, BitConverter.ToInt64(bytes, ApplicantSize()));
    }

    [TestMethod]
    public void AnAnswerToANoteHasTheMinutesToWaitOnlyWhenTheClientCanShowThem()
    {
        var note = new LeagueBoardEntry { Name = "Gus", Message = "Hi", Result = 57022 };

        Assert.HasCount(4 + 4 + TextSize("Gus") + TextSize("Hi") + 8 + 4 + 8, Written(new LeagueBoardResResponse { Entry = note, Remain = 3 }));
        Assert.HasCount(4 + 4 + TextSize("Gus") + TextSize("Hi") + 8 + 4, Written(new LeagueBoardResResponse { Entry = note with { Result = 0 }, Remain = 3 }));
    }

    [TestMethod]
    public void ARecruitNoticeIsTheLeagueATextADateAndAResult()
    {
        Assert.HasCount(4 + TextSize("Hi") + 8 + 4, Written(new LeagueRecruitNoticeResponse { League = 1, Notice = "Hi", Date = 2, Result = 0 }));
        Assert.HasCount(4 + TextSize("Hi") + 8 + 4 + 8, Written(new LeagueRecruitNoticeResponse { League = 1, Notice = "Hi", Date = 2, Result = 57029, Remain = 5 }));
    }

    [TestMethod]
    public void RightsAreNinePairsAndAResult()
    {
        Assert.HasCount(9 * 8 + 4, Written(new LeagueAuthChangeResponse { Result = 0 }));
        Assert.HasCount(9 * 8 + 4, Written(new LeagueAuthChangeResResponse { Result = 0 }));
    }

    [TestMethod]
    public void SmallLeagueMessagesHaveTheClientSize()
    {
        Assert.HasCount(4, Written(new LeagueDeleteResponse { Result = 0 }));
        Assert.HasCount(4, Written(new LeagueWithdrawResponse { Actor = 1 }));
        Assert.HasCount(8, Written(new LeagueWithdrawResResponse { Remain = 1 }));
        Assert.HasCount(0, Written(new LeagueKickResponse()));
        Assert.HasCount(4, Written(new LeagueKickResResponse { Result = 0 }));
        Assert.HasCount(8, Written(new LeagueMemberLeaveResponse { Target = 1, Kicker = 2 }));
        Assert.HasCount(4 + 4 + 8 + 2, Written(new LeagueInfoUpdateResponse { League = 1, Members = 3 }));
        Assert.HasCount(12 + TextSize("Ab"), Written(new LeagueInfoAlertResponse { Actor = 1, League = 2, Name = "Ab" }));
        Assert.HasCount(4 + 1, Written(new LeagueOpenOrNotResponse { League = 1, Open = true }));
        Assert.HasCount(4 + 1 + 1 + 4, Written(new LeagueMemberPositionChangeResponse { Actor = 1, Position = 2 }));
        Assert.HasCount(8, Written(new LeagueCreateResResponse { Remain = 1 }));
        Assert.HasCount(1, Written(new LeagueOverlapNameResponse { Used = true }));
        Assert.HasCount(4 + 4 + 8 + 2, Written(new LeagueInfoUpdateResponse { League = 1, Money = 2, Members = 3 }));
        Assert.HasCount(4 + 4 + 4 + 4, Written(new LeagueApplicantRejectResResponse { League = 1 }));
        Assert.HasCount(4, Written(new LeagueApplicantDeleteResponse { Actor = 1 }));
        Assert.HasCount(4 + 4 + 1 + 1 + 2 + 8 + TextSize("Eve") + 1, Written(new LeagueMemberUpdateResponse { League = 1, Actor = 2, Name = "Eve" }));
    }

    [TestMethod]
    public void AnInvitationIsThreeNamesAndFourNumbers()
    {
        var bytes = Written(new LeagueInviteResponse { LeagueName = "Ab", TargetName = "Cd", RequesterName = "Ef", Actor = 1, TargetActor = 2, League = 3, Result = 4 });

        Assert.HasCount(TextSize("Ab") + TextSize("Cd") + TextSize("Ef") + 16, bytes);
        Assert.AreEqual(4, BitConverter.ToInt32(bytes, bytes.Length - 4));
    }

    [TestMethod]
    public void TheSearchEndsWithTheMasters()
    {
        var bytes = Written(new LeagueSearchResponse { State = 1, Name = "Ab", Master = "Cd", Reserved = 0, Leagues = [Info()], Actors = [7, 8] });

        Assert.HasCount(1 + TextSize("Ab") + TextSize("Cd") + 4 + 1 + InfoSize() + 1 + 8, bytes);
    }

    [TestMethod]
    public void RequestsReadWhatTheClientSends()
    {
        using (var reader = Request(w =>
               {
                   Text(w, "Name");
                   w.Write(1);
                   w.Write(2);
                   w.Write(3);
               }))
        {
            var request = new LeagueCreateRequest(reader);

            Assert.AreEqual("Name", request.Name);
            Assert.AreEqual(1, request.Npc);
            Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        }

        using (var reader = Request(w =>
               {
                   w.Write((byte)1);
                   Text(w, "Ab");
                   Text(w, "Cd");
                   w.Write(0);
                   w.Write((byte)0);
               }))
        {
            var request = new LeagueSearchRequest(reader);

            Assert.AreEqual("Ab", request.Name);
            Assert.AreEqual("Cd", request.Master);
            Assert.IsEmpty(request.Leagues);
            Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        }

        using (var reader = Request(w =>
               {
                   w.Write(4);
                   w.Write((byte)2);
                   w.Write((byte)1);
                   w.Write(0);
               }))
        {
            var request = new LeagueMemberPositionChangeRequest(reader);

            Assert.AreEqual(4, request.Actor);
            Assert.AreEqual(2, request.Position);
            Assert.AreEqual(1, request.State);
            Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        }

        using (var reader = Request(w =>
               {
                   w.Write(1);
                   Text(w, "Hi");
                   w.Write(0L);
                   w.Write(0);
               }))
        {
            var request = new LeagueNoticeChangeRequest(reader);

            Assert.AreEqual("Hi", request.Notice);
            Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        }

        using (var reader = Request(w =>
               {
                   w.Write(3);
                   w.Write((byte)1);
               }))
        {
            var request = new LeagueOpenOrNotRequest(reader);

            Assert.AreEqual(3, request.League);
            Assert.IsTrue(request.Open);
        }
    }

    [TestMethod]
    public void AMemberMayDoWhatTheLeagueSaysHisPositionMay()
    {
        var league = new LeagueRecord(
            1, "Ab", 9, "Cd", string.Empty, 1, 0, 0, true, 0, 0, string.Empty, null, string.Empty, null,
            ["", "", ""], LeagueServiceBase.DefaultAuth, new int[9], DateTime.UtcNow, 1);

        // The bits are the ones the client tests before it offers a button.
        Assert.AreEqual(1, (int)LeagueRight.Join);
        Assert.AreEqual(2, (int)LeagueRight.KickOut);
        Assert.AreEqual(16, (int)LeagueRight.Notice);
        Assert.AreEqual(64, (int)LeagueRight.SetPosition);
        Assert.AreEqual(128, (int)LeagueRight.LimitOut);
        Assert.AreEqual(256, (int)LeagueRight.RecruitNotice);

        // The master can do everything, whatever is written.
        Assert.IsTrue(league.Allows(LeagueRules.Master, LeagueRight.KickOut));

        // A new member can do nothing; a named position invites and answers those who ask, but throws nobody out.
        Assert.IsFalse(league.Allows(0, LeagueRight.Join));
        Assert.IsTrue(league.Allows(1, LeagueRight.Join));
        Assert.IsFalse(league.Allows(1, LeagueRight.KickOut));
        Assert.IsTrue(league.Allows(LeagueRules.SubMaster, LeagueRight.KickOut));

        // Changing what a position may do takes the right to set positions, and only below one's own position.
        var auth = (int[])LeagueServiceBase.DefaultAuth.Clone();
        auth[1] = (int)LeagueRight.All;

        Assert.IsTrue(league.MayChange(LeagueRules.SubMaster, auth, new int[9]));
        Assert.IsFalse(league.MayChange(LeagueRules.Elder, auth, new int[9]));
        Assert.IsFalse(league.MayChange(LeagueRules.SubMaster, LeagueServiceBase.DefaultAuth, [0, 0, 0, 0, 0, 0, 0, 100, 0]));
    }

    [TestMethod]
    public void PositionsAreGivenAsTheClientOffersThem()
    {
        Assert.IsTrue(LeagueRules.CanBeGiven(LeagueRules.BasePosition));
        Assert.IsTrue(LeagueRules.CanBeGiven(LeagueRules.LastNamed));
        Assert.IsTrue(LeagueRules.CanBeGiven(LeagueRules.Elder));
        Assert.IsTrue(LeagueRules.CanBeGiven(LeagueRules.SubMaster));
        Assert.IsFalse(LeagueRules.CanBeGiven(LeagueRules.ChairMan));
        Assert.IsFalse(LeagueRules.CanBeGiven(LeagueRules.Master));

        Assert.AreEqual(1, LeagueRules.HoldersOf(LeagueRules.SubMaster));

        // Below oneself only, unless one is the master.
        Assert.IsTrue(LeagueRules.MayAppoint(LeagueRules.SubMaster, LeagueRules.BasePosition, LeagueRules.Manager));
        Assert.IsFalse(LeagueRules.MayAppoint(LeagueRules.Manager, LeagueRules.BasePosition, LeagueRules.SubMaster));
        Assert.IsFalse(LeagueRules.MayAppoint(LeagueRules.Manager, LeagueRules.Manager, LeagueRules.BasePosition));
        Assert.IsTrue(LeagueRules.MayAppoint(LeagueRules.Master, LeagueRules.SubMaster, LeagueRules.BasePosition));
    }
}
