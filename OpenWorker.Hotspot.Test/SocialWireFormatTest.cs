using System.Text;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Friends.Types;
using OpenWorker.Hotspot.Modules.Party;
using OpenWorker.Hotspot.Modules.Party.Requests;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Sizes and places below are what the client reads, not what these messages happen to write.
/// </summary>
[TestClass]
public sealed class SocialWireFormatTest
{
    private const int ActorSize = 4;
    private const int MapSize = 8;

    // Two bytes of length, UTF-16 and a terminator of one byte.
    private static int TextSize(string value) => 2 + 2 * value.Length + 1;

    private static int MemberSize(string name) => ActorSize + TextSize(name) + 1 + 1 + 4 * 4 + 1 + MapSize;

    private static int RecruitSize(string message, string master) =>
        4 + TextSize(message) + 2 + 2 + 1 + TextSize(master) + 1 + 4 + 4;

    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

    private static PartyMemberInfoValue Member(string name) => new()
    {
        Actor = new ActorValue(7, ActorType.User),
        Name = name,
        Level = 10,
        Class = Hero.Lily,
        Location = 10003,
        Channel = 2,
        MaxHealth = 300,
        Health = 200,
        IsLoggedIn = true,
        Map = default,
    };

    private static PartyRecruitMemberValue Recruit(string message, string master) => new()
    {
        Party = 5,
        Message = message,
        MinLevel = 1,
        MaxLevel = 85,
        Purpose = 1,
        MasterName = master,
        MemberCount = 2,
        RemainTime = 60,
        PurposeLocation = 21311,
    };

    private static string TextAt(byte[] bytes, int offset)
    {
        var length = BitConverter.ToInt16(bytes, offset);

        return Encoding.Unicode.GetString(bytes, offset + 2, length - 1);
    }

    [TestMethod]
    public void PartyInviteStartsWithTheNames()
    {
        var bytes = Written(new PartyInviteResponse
        {
            TargetName = "Ab",
            RequesterName = "Cde",
            RequesterActor = new ActorValue(1, ActorType.User),
            TargetActor = new ActorValue(2, ActorType.User),
            Server = 3,
            Result = 4,
        });

        var actors = TextSize("Ab") + TextSize("Cde");

        Assert.HasCount(actors + 4 * 4, bytes);
        Assert.AreEqual("Ab", TextAt(bytes, 0));
        Assert.AreEqual("Cde", TextAt(bytes, TextSize("Ab")));
        Assert.AreEqual(3, BitConverter.ToInt32(bytes, actors + 8));
        Assert.AreEqual(4, BitConverter.ToInt32(bytes, actors + 12));
    }

    [TestMethod]
    public void PartyInviteRequestEndsWithAChoice()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        foreach (var name in (ReadOnlySpan<string>)["Ab", ""])
        {
            writer.Write((short)(2 * name.Length + 1));
            writer.Write(Encoding.Unicode.GetBytes(name));
            writer.Write((byte)0);
        }

        writer.Write(1);
        writer.Write(2);
        writer.Write(3);
        writer.Write(4);
        writer.Write((byte)9);
        writer.Flush();
        stream.Position = 0;

        using var reader = new BinaryReader(stream);
        var request = new PartyInviteRequest(reader);

        Assert.AreEqual("Ab", request.TargetName);
        Assert.AreEqual((byte)9, request.Select);
        Assert.AreEqual(stream.Length, stream.Position);
    }

    [TestMethod]
    public void PartyMemberHasTheClientLayout()
    {
        var bytes = Written(new PartyAddMemberResponse { Info = Member("Ab") });

        Assert.HasCount(MemberSize("Ab"), bytes);

        var after = ActorSize + TextSize("Ab");

        Assert.AreEqual((byte)10, bytes[after]);
        Assert.AreEqual((byte)Hero.Lily, bytes[after + 1]);
        Assert.AreEqual(10003, BitConverter.ToInt32(bytes, after + 2));
        Assert.AreEqual(2, BitConverter.ToInt32(bytes, after + 6));
        Assert.AreEqual(300, BitConverter.ToInt32(bytes, after + 10));
        Assert.AreEqual(200, BitConverter.ToInt32(bytes, after + 14));
        Assert.AreEqual((byte)1, bytes[after + 18]);
    }

    [TestMethod]
    public void MatchingAlwaysSendsAFullParty()
    {
        var bytes = Written(new PartyMatchingEnterResponse
        {
            Identifier = 1,
            MemberList = [Member("Ab"), Member("Cde")],
            RemainTick = 120,
        });

        var size = 4 + MemberSize("Ab") + MemberSize("Cde") + 2 * MemberSize("") + 4;

        Assert.AreEqual(4, PartyModuleDefines.MaxMemberCount);
        Assert.HasCount(size, bytes);
        Assert.AreEqual(120, BitConverter.ToInt32(bytes, size - 4));
    }

    [TestMethod]
    public void PenaltyIsAWholeNumberOfSeconds()
    {
        var bytes = Written(new PartyRecruitPenaltyResponse { Time = 30 });

        Assert.HasCount(4, bytes);
        Assert.AreEqual(30, BitConverter.ToInt32(bytes));
    }

    [TestMethod]
    public void RecruitAddIsResultTimeAndRecruit()
    {
        var bytes = Written(new PartyRecruitAddResponse { IsSuccess = true, RemainTime = 60, Member = Recruit("Hi", "Ab") });

        Assert.HasCount(1 + 4 + RecruitSize("Hi", "Ab"), bytes);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual(60, BitConverter.ToInt32(bytes, 1));
    }

    [TestMethod]
    public void RecruitListCountsWithFourBytes()
    {
        var bytes = Written(new PartyRecruitListResponse { Values = [Recruit("Hi", "Ab")] });

        Assert.HasCount(4 + RecruitSize("Hi", "Ab"), bytes);
        Assert.AreEqual(1, BitConverter.ToInt32(bytes));
    }

    [TestMethod]
    public void RecruitApplyIsErrorAndCount()
    {
        var bytes = Written(new PartyRecruitApplyResponse { ApplyCount = 3 });

        Assert.HasCount(4 + 1, bytes);
        Assert.AreEqual((byte)3, bytes[4]);
    }

    [TestMethod]
    public void ApplyInfoStartsWithTheParty()
    {
        var bytes = Written(new PartyRecruitApplyInfoResponse
        {
            Party = 8,
            MemberList = [Member("Ab")],
            Actor = new ActorValue(6, ActorType.User),
        });

        Assert.HasCount(4 + 4 + MemberSize("Ab") + ActorSize, bytes);
        Assert.AreEqual(8, BitConverter.ToInt32(bytes));
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 4));
    }

    [TestMethod]
    public void ApplyListIsAlwaysTenPlaces()
    {
        var values = Enumerable
            .Range(0, PartyModuleDefines.RecruitAppliesCount)
            .Select(_ => new PartyApplyMemberValue { Member = Member("Ab"), RegDate = 1 })
            .ToArray();

        Assert.HasCount(10 * (MemberSize("Ab") + 4), Written(new PartyRecruitApplyListResponse { ValueList = values }));
    }

    [TestMethod]
    public void UpdateInfoHasHeaderAndCountOfOneByte()
    {
        var bytes = Written(new PartyUpdateInfoResponse { InfoList = [Member("Ab")] });

        Assert.HasCount(4 + ActorSize + MapSize + 1 + 1 + 1 + MemberSize("Ab"), bytes);
        Assert.AreEqual((byte)1, bytes[4 + ActorSize + MapSize + 2]);
    }

    [TestMethod]
    public void SmallPartyMessagesHaveTheClientSize()
    {
        Assert.HasCount(4 + 4, Written(new PartyAcceptResponse()));
        Assert.HasCount(ActorSize, Written(new PartyChangeMasterResponse()));
        Assert.HasCount(4 + ActorSize + 1, Written(new PartyLeaveResponse()));
        Assert.HasCount(ActorSize + 4 + 4, Written(new PartyUpdateMemberHpResponse()));
        Assert.HasCount(ActorSize + ActorSize + TextSize("Ab") + 4, Written(new PartyCancelResponse { RejName = "Ab" }));
        Assert.HasCount(4 + 1, Written(new PartyMatchingExitResponse()));
        Assert.HasCount(ActorSize, Written(new PartyMatchingWaitResponse()));
        Assert.HasCount(ActorSize + 2 + 4, Written(new PartyRecruitApplyUpdateResponse()));
        Assert.HasCount(4, Written(new PartyRecruitDelResponse()));
        Assert.HasCount(ActorSize + 4, Written(new PartyRecruitApplyDelResponse()));
        Assert.HasCount(4 + MemberSize("Ab"), Written(new PartyUpdateMemberInfoResponse { Member = Member("Ab") }));
        Assert.HasCount(MemberSize("Ab") + 4, Written(new PartyRecruitApplyNoticeResponse
        {
            Member = new PartyApplyMemberValue { Member = Member("Ab") },
        }));
    }

    [TestMethod]
    public void AwaiterHasTheClientLayout()
    {
        var bytes = Written(new PartyAwaiterAddResponse
        {
            Awaiter = new PartyAwaiterValue
            {
                Actor = new ActorValue(7, ActorType.User),
                Name = "Ab",
                Level = 12,
                Class = Hero.Jin,
                RemainTime = 1800,
            },
        });

        var after = ActorSize + TextSize("Ab");

        Assert.HasCount(after + 2 + 1 + 4 + 4 + 4, bytes);
        Assert.AreEqual((short)12, BitConverter.ToInt16(bytes, after));
        Assert.AreEqual((byte)Hero.Jin, bytes[after + 2]);
        Assert.AreEqual(1800, BitConverter.ToInt32(bytes, after + 3));
    }

    [TestMethod]
    public void AwaiterListAndRemovalHaveTheClientSize()
    {
        Assert.HasCount(ActorSize, Written(new PartyAwaiterDelResponse()));

        var bytes = Written(new PartyAwaiterListResponse { List = [] });

        Assert.HasCount(4, bytes);
        Assert.AreEqual(0, BitConverter.ToInt32(bytes));
    }

    [TestMethod]
    public void FriendSearchEndsWithAFlag()
    {
        var bytes = Written(new FriendFindResponse
        {
            Values = [new FriendFindEntry { Person = 1, Name = "Ab", Level = 2, Channel = 3, World = 10003, IsLoggedIn = true }],
            IsLast = true,
        });

        Assert.HasCount(2 + 4 + TextSize("Ab") + 1 + 1 + 2 + 1 + 1, bytes);
        Assert.AreEqual((short)1, BitConverter.ToInt16(bytes));
        Assert.AreEqual((byte)1, bytes[^1]);
    }

    [TestMethod]
    public void FriendHasTheClientLayout()
    {
        var bytes = Written(new FriendLoadResponse
        {
            List =
            [
                new FriendValue
                {
                    Name = "Ab",
                    Friend = 1,
                    Level = 2,
                    Class = 3,
                    State = default,
                    CommunityState = CommunityState.Chair,
                    Note = "Cde",
                    Channel = 4,
                    World = 10003,
                    FriendPoint = 5,
                    Login = true,
                    LogOut = 6,
                    Remain = 7,
                }
            ],
        });

        Assert.AreEqual((byte)1, bytes[0]);
        Assert.HasCount(1 + TextSize("Ab") + 4 + 4 + TextSize("Cde") + 1 + 2 + 8 + 1 + 8 + 8, bytes);
    }

    [TestMethod]
    public void BlockedPersonHasTheClientLayout()
    {
        var bytes = Written(new FriendLoadBlocklistResponse { List = [new FriendBlockValue(1, "Ab", 2)] });

        Assert.AreEqual((byte)1, bytes[0]);
        Assert.HasCount(1 + 4 + TextSize("Ab") + 1, bytes);
    }

    [TestMethod]
    public void RecruitedFriendHasTheClientLayout()
    {
        var bytes = Written(new FriendRecruitListResponse
        {
            List =
            [
                new FriendRecruitValue { Name = "Ab", Person = 1, Level = 2, Hero = Hero.Iris, Status = 3, Memo = "Cde", Channel = 4, World = 10003 }
            ],
        });

        Assert.AreEqual((byte)1, bytes[0]);
        Assert.HasCount(1 + TextSize("Ab") + 4 + 1 + 1 + 1 + TextSize("Cde") + 1 + 2 + 1 + 8 + 8, bytes);
    }

    [TestMethod]
    public void RecommendedFriendHasTheClientLayout()
    {
        var bytes = Written(new FriendRecommendListResponse
        {
            Actor = new ActorValue(1, ActorType.User),
            List =
            [
                new FriendRecommendValue
                {
                    Name = "Ab",
                    Actor = new ActorValue(2, ActorType.User),
                    Level = 3,
                    Hero = Hero.Haru,
                    World = 10003,
                    Channel = 4,
                    IsLoggedIn = 1,
                }
            ],
        });

        var after = ActorSize + 1 + TextSize("Ab") + ActorSize;

        Assert.HasCount(after + 1 + 1 + 2 + 1 + 1, bytes);
        Assert.AreEqual((short)10003, BitConverter.ToInt16(bytes, after + 2));
        Assert.AreEqual((byte)4, bytes[after + 4]);
    }

    [TestMethod]
    public void SmallFriendMessagesHaveTheClientSize()
    {
        Assert.HasCount(4 + TextSize("Ab"), Written(new FriendInviteResponse { Name = "Ab" }));
        Assert.HasCount(TextSize("Ab"), Written(new FriendInviteReqResponse { Name = "Ab" }));
        Assert.HasCount(4 + TextSize("Ab"), Written(new FriendInviteAcceptResponse { Name = "Ab" }));
        Assert.HasCount(4 + 1, Written(new FriendDeleteResponse()));
        Assert.HasCount(4 + TextSize("Ab") + 1, Written(new FriendBlockAddResponse { Value = new FriendBlockValue(1, "Ab", 2) }));
        Assert.HasCount(4 + TextSize("Ab"), Written(new FriendBlockDelResponse { Name = "Ab" }));
        Assert.HasCount(4 + TextSize("Ab"), Written(new FriendRecruitAddResponse { Result = 0, Name = "Ab" }));
        Assert.HasCount(4 + TextSize("Ab"), Written(new FriendRecruitDelResponse { Name = "Ab" }));
        Assert.HasCount(1, Written(new FriendRecruitInfoResponse { IsRegistered = true }));
    }

    [TestMethod]
    public void QuestListHasTheClientLayout()
    {
        var conditions = Enumerable
            .Range(0, QuestModuleDefines.ConditionsPerEpisode)
            .Select(i => new QuestCondition { Condition = i, Step = (byte)i })
            .ToArray();

        var bytes = Written(new QuestListResponse
        {
            List =
            [
                new QuestEpisodeEntry
                {
                    Episode = 42,
                    Info = new QuestInfoEntry { IsAddHelper = true, CompleteBit = 3, IsFailed = false, Condition = conditions },
                }
            ],
        });

        Assert.AreEqual(10, QuestModuleDefines.ConditionsPerEpisode);
        Assert.HasCount(2 + 4 + 1 + 2 + 1 + 10 * (4 + 1), bytes);
        Assert.AreEqual((short)1, BitConverter.ToInt16(bytes));
        Assert.AreEqual(42, BitConverter.ToInt32(bytes, 2));
        Assert.AreEqual((byte)1, bytes[6]);
        Assert.AreEqual((short)3, BitConverter.ToInt16(bytes, 7));
        Assert.AreEqual(9, BitConverter.ToInt32(bytes, 10 + 9 * 5));
    }

    [TestMethod]
    public void CompletedEpisodeCountsItsItems()
    {
        var empty = Written(new QuestEpisodeCompleteResponse { Title = new TitleValue(0, 0), Items = [] });

        Assert.HasCount(4 + 4 * 4 + 8 + 4 + 1, empty);
        Assert.AreEqual((byte)0, empty[^1]);

        var bytes = Written(new QuestEpisodeCompleteResponse
        {
            Title = new TitleValue(0, 0),
            Items = [new CreateItemValue { Item = 100, Count = 2 }, new CreateItemValue { Item = 200, Count = 3 }],
        });

        Assert.HasCount(4 + 4 * 4 + 8 + 4 + 1 + 2 * (4 + 2), bytes);
        Assert.AreEqual((byte)2, bytes[32]);
        Assert.AreEqual(100, BitConverter.ToInt32(bytes, 33));
        Assert.AreEqual((short)3, BitConverter.ToInt16(bytes, 43));
    }

    [TestMethod]
    public void SmallQuestMessagesHaveTheClientSize()
    {
        Assert.HasCount(4 + 1, Written(new QuestAcceptResponse()));
        Assert.HasCount(4, Written(new QuestGiveUpResponse()));
        Assert.HasCount(4, Written(new QuestFailResponse()));
        Assert.HasCount(4, Written(new QuestEpisodeResetResponse()));
        Assert.IsEmpty(Written(new QuestEventUpdateResponse()));
        Assert.IsEmpty(Written(new QuestResetResponse()));
        Assert.HasCount(2 + 4 + 1, Written(new QuestUpdateResponse
        {
            ConditionList = [new QuestCondition { Condition = 1, Step = 2 }],
        }));

        var helper = Written(new QuestHelperResponse { Type = QuestHelperType.Add, Episode = 42 });

        Assert.HasCount(1 + 4, helper);
        Assert.AreEqual((byte)1, helper[0]);
        Assert.AreEqual(42, BitConverter.ToInt32(helper, 1));
    }

    [TestMethod]
    public void CompleteListIsAFixedBlock()
    {
        Assert.AreEqual(128, QuestModuleDefines.CompleteListByteLength);
    }
}
