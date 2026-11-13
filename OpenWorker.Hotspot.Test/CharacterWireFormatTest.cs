using System.Numerics;
using System.Text;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class CharacterWireFormatTest
{
    private const int Actor = 0x123456;

    private static readonly ushort[] NoCategories = new ushort[7];

    private static BinaryReader Request(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();
        stream.Position = 0;

        return new BinaryReader(stream);
    }

    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

    private static byte[] Expected(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static void WriteClientString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);

        writer.Write((short)(bytes.Length + 1));
        writer.Write(bytes);
        writer.Write((byte)0);
    }

    [TestMethod]
    public void ChangeMotionRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            w.Write((short)7);
            w.Write((short)8);
        });

        var request = new PersonChangeMotionReqRequest(reader);

        Assert.AreEqual((short)7, request.Motion);
        Assert.AreEqual((short)8, request.SubMotion);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void ChangeMotionBroadcastHasTheClientLayout()
    {
        var bytes = Written(new PersonChangeMotionBroadcastResponse { Actor = Actor, Motion = 7, SubMotion = 8 });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Actor);
            w.Write((short)7);
            w.Write((short)8);
        }), bytes);
    }

    [TestMethod]
    public void UpdateTitleRequestReadsFourNumbers()
    {
        using var reader = Request(w =>
        {
            w.Write(1);
            w.Write(2);
            w.Write(3);
            w.Write(4);
        });

        var request = new PersonUpdateTitleRequest(reader);

        Assert.AreEqual(1, request.Ability.Primary);
        Assert.AreEqual(2, request.Ability.Secondary);
        Assert.AreEqual(3, request.Represent.Primary);
        Assert.AreEqual(4, request.Represent.Secondary);
    }

    [TestMethod]
    public void UpdateTitleResponseEndsWithTheResult()
    {
        var bytes = Written(new PersonUpdateTitleResponse
        {
            Ability = new TitleValue(1, 2),
            Represent = new TitleValue(3, 4),
            Result = true
        });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(1);
            w.Write(2);
            w.Write(3);
            w.Write(4);
            w.Write((byte)1);
        }), bytes);
    }

    [TestMethod]
    public void UpdateTitleBroadcastPutsTheActorLast()
    {
        var bytes = Written(new PersonUpdateTitleBroadcastResponse { Title = new TitleValue(1, 2), Actor = Actor });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(1);
            w.Write(2);
            w.Write(Actor);
        }), bytes);
    }

    [TestMethod]
    public void TitleListResponsesHaveTheClientSize()
    {
        Assert.HasCount(8, Written(new PersonAddTitleResponse { Title = 1 }));
        Assert.IsEmpty(Written(new PersonClearTitleResponse()));
        Assert.HasCount(4, Written(new PersonDeleteTitleResponse { Title = 1 }));
        Assert.HasCount(4, Written(new PersonUpdateOpenTitleResponse { Title = 1 }));
    }

    [TestMethod]
    public void StatResponsesHaveTheClientLayout()
    {
        var one = Written(new PersonUpdateStatResponse { Value = 1.5f, Stat = 16 });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(1.5f);
            w.Write((ushort)16);
        }), one);

        var list = Written(new PersonUpdateStatListResponse
        {
            Type = 1,
            Actor = Actor,
            Stats = [new ST_STAT { fValue = 1.5f, wStatID = 16 }]
        });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write((byte)1);
            w.Write(Actor);
            w.Write((byte)1);
            w.Write(1.5f);
            w.Write((ushort)16);
        }), list);
    }

    [TestMethod]
    public void ReviveResponseHasTheClientSize()
    {
        var bytes = Written(new PersonReviveResponse
        {
            Actor = Actor,
            Owner = 2,
            Type = 3,
            Position = new Vector3(1, 2, 3),
            Rotation = 4
        });

        Assert.HasCount(4 + 4 + 1 + 12 + 4, bytes);
    }

    [TestMethod]
    public void EchelonResponsesHaveTheClientSize()
    {
        Assert.HasCount(1 + 4 + 1, Written(new PersonUpdateEchelonInfoResponse { Level = 1, Experience = 2, LevelUp = true }));
        Assert.HasCount(4 + 1, Written(new PersonUpdateEchelonBroadcastResponse { Actor = Actor, Level = 1 }));
    }

    [TestMethod]
    public void SharePointResponsesHaveTheClientSize()
    {
        var district = new DistrictStateValue { District = 1, Point = 2, Reward = true };
        var maze = new MazeStateValue { Maze = 1, Group = 2, District = 3, ClearCount = 4, SharePoint = 5, RewardStep = 6 };

        Assert.HasCount(9 + 21, Written(new PersonUpdateSharePointResponse { District = district, Maze = maze }));
        Assert.HasCount(1 + 2 * 9, Written(new PersonLoadDistrictStateResponse { States = [district, district] }));
        Assert.HasCount(2 + 2 * 21, Written(new PersonLoadMazeStateResponse { States = [maze, maze] }));
    }

    [TestMethod]
    public void RewardOfSharePointListsItems()
    {
        var bytes = Written(new PersonGetRewardSharePointResponse
        {
            Identifier = 1,
            RewardStep = 2,
            Result = 0,
            Items = [new RewardItemValue { Item = 5, Count = 6 }]
        });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(1);
            w.Write((byte)2);
            w.Write((byte)0);
            w.Write((byte)1);
            w.Write(5);
            w.Write((short)6);
        }), bytes);
    }

    [TestMethod]
    public void NpcCreditHasTheClientSize()
    {
        var credit = new NpcCreditValue { Group = 1, Grade = 2, Point = 3, CurrentPoint = 4, UpdatedAt = 5 };

        Assert.HasCount(19, Written(new PersonNpcCreditUpdateResponse { Credit = credit }));
        Assert.HasCount(1 + 2 * 19, Written(new PersonNpcCreditLoadResponse { Credits = [credit, credit] }));
        Assert.IsEmpty(Written(new PersonNpcCreditInitResponse()));
    }

    [TestMethod]
    public void FatiguePointsHaveTheClientSize()
    {
        Assert.HasCount(5, Written(new PersonFpUpdateResponse { Common = 1, Bonus = 2, ShowMessage = true }));
    }

    [TestMethod]
    public void AchievementsHaveTheClientLayout()
    {
        var bits = new byte[128];

        Assert.HasCount(128 + 1 + 12 + 14, Written(new PersonAchieveSelectResponse
        {
            RewardBits = bits,
            Achievements = [new AchieveValue { Index = 1, Count = 2 }],
            Categories = NoCategories
        }));

        Assert.HasCount(1 + 19 + 128 + 14, Written(new PersonAchieveUpdateResponse
        {
            Updates = [new AchieveUpdateValue { Index = 1, Count = 2, NextIndex = 3, Category = 4, CategoryCount = 5 }],
            RewardBits = bits,
            Categories = NoCategories
        }));

        Assert.HasCount(4, Written(new PersonAchieveRewardResponse { Index = 1 }));
    }

    [TestMethod]
    public void RefusalNamesThePerson()
    {
        var bytes = Written(new PersonOtherInfoRefuseResponse { Error = 59200, Name = "Ab" });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(59200);
            WriteClientString(w, "Ab");
        }), bytes);
    }

    [TestMethod]
    public void OtherPersonHasTheClientLayout()
    {
        var bytes = Written(new PersonOtherInfoResponse
        {
            CalculateStat = true,
            Target = new OtherPersonValue
            {
                Level = 25,
                State = CommunityState.Solo,
                Comment = "Hi",
                BaseStats = [],
                Stats = [],
                Equipment = [],
                EchelonLevel = 2,
                EchelonExperience = 3,
                Memo = "Me",
                EquipMemorySlot = 4,
                Class = 5,
                Name = "Ab",
                Ability = new TitleValue(6, 7),
                Represent = new TitleValue(8, 9),
                Sockets = [],
                Broaches = []
            },
            Categories = NoCategories
        });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write((byte)1);
            w.Write((short)25);
            w.Write((byte)CommunityState.Solo);
            WriteClientString(w, "Hi");
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write(0);
            w.Write((byte)2);
            w.Write(3);
            WriteClientString(w, "Me");
            w.Write(4);
            w.Write((byte)5);
            WriteClientString(w, "Ab");
            w.Write(6);
            w.Write(7);
            w.Write(8);
            w.Write(9);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write(new byte[14]);
        }), bytes);
    }

    [TestMethod]
    public void OtherInfoRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            w.Write(Actor);
            w.Write((byte)1);
            w.Write((byte)0);
        });

        var request = new PersonOtherInfoRequest(reader);

        Assert.AreEqual(Actor, request.Person);
        Assert.IsTrue(request.CheckOption);
        Assert.IsFalse(request.CalculateStat);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void CommunityRoundTrips()
    {
        var bytes = Written(new PersonCommunityResponse { State = CommunityState.Rookie, Comment = "Hi", Memo = "Me" });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write((byte)CommunityState.Rookie);
            WriteClientString(w, "Hi");
            WriteClientString(w, "Me");
        }), bytes);

        using var reader = Request(w => w.Write(bytes));

        var request = new PersonCommunityRequest(reader);

        Assert.AreEqual(CommunityState.Rookie, request.State);
        Assert.AreEqual("Hi", request.Comment);
        Assert.AreEqual("Me", request.Memo);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void CutsceneMessagesHaveTheClientSize()
    {
        Assert.HasCount(1, Written(new PersonPlayCutsceneResponse { State = 1 }));

        using var reader = Request(w =>
        {
            w.Write(Actor);
            w.Write((byte)1);
            w.Write(new byte[256]);
        });

        var request = new PersonUpdateCutsceneRequest(reader);

        Assert.IsTrue(request.Play);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }
}
