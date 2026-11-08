using System.Text;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the client reads of the answers about friends and of the window that shows another player.
/// </summary>
[TestClass]
public sealed class FriendWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static void WriteClientString(BinaryWriter writer, string value)
    {
        writer.Write((short)(2 * value.Length + 1));
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((byte)0);
    }

    private static OtherPersonValue Person() => new()
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
    };

    private static byte[] PersonBytes()
    {
        return Written(new PersonOtherInfoResponseProbe(Person()));
    }

    private readonly struct PersonOtherInfoResponseProbe(OtherPersonValue value) : IResponseHotspotMessage
    {
        public GameServer.SoulWorker.Network.DataTypes.MessageOpcode Opcode => default;

        public void Write(BinaryWriter writer) => value.Write(writer);
    }

    [TestMethod]
    public void TheWindowOfAFriendReadsAResultThePersonAndTwoTitles()
    {
        var bytes = Written(new FriendInfoResponse { Result = 0, Person = Person(), Title = new TitleValue(8, 9) });

        var person = PersonBytes();

        Assert.HasCount(4 + person.Length + 8, bytes);
        CollectionAssert.AreEqual(person, bytes[4..(4 + person.Length)]);
        Assert.AreEqual(8, BitConverter.ToInt32(bytes, 4 + person.Length));
        Assert.AreEqual(9, BitConverter.ToInt32(bytes, 4 + person.Length + 4));
    }

    [TestMethod]
    public void TheWindowOfSomebodyWhoWaitsForAPartyReadsThePersonAndTwoTitles()
    {
        var bytes = Written(new PartyAwaiterInfoResponse { Person = Person(), Title = new TitleValue(8, 9) });

        var person = PersonBytes();

        Assert.HasCount(person.Length + 8, bytes);
        CollectionAssert.AreEqual(person, bytes[..person.Length]);
        Assert.AreEqual(9, BitConverter.ToInt32(bytes, person.Length + 4));
    }

    [TestMethod]
    public void ARefusedRequestCarriesTheNumberOfTheText()
    {
        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        expected.Write(55107);
        WriteClientString(expected, "Ab");
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), Written(new FriendInviteAcceptResponse { Result = 55107, Name = "Ab" }));

        stream.SetLength(0);
        expected.Write(55103);
        WriteClientString(expected, "Ab");
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), Written(new FriendInviteResponse { Result = 55103, Name = "Ab" }));
    }

    [TestMethod]
    public void ANoticeIsATypeTwoTextsAndAColour()
    {
        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        expected.Write((byte)0);
        WriteClientString(expected, "soon");
        WriteClientString(expected, "FF0000");
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), Written(new ChatNoticeResponse { Message = "soon" }));
    }

    [TestMethod]
    public void AFriendInTheListShowsWhereTheyAreOnlyWhileTheyAreOnline()
    {
        var logout = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        var entry = new FriendEntry(7, "Bob", 3, FriendKind.Friend, 120, (byte)CommunityState.Rookie, "memo", logout, logout);

        var offline = FriendBook.ToValue(entry, null);

        Assert.AreEqual("Bob", offline.Name);
        Assert.AreEqual(7, offline.Friend);
        Assert.AreEqual(3, offline.Class);
        Assert.AreEqual(FriendState.Friend, offline.State);
        Assert.AreEqual(CommunityState.Rookie, offline.CommunityState);
        Assert.AreEqual("memo", offline.Note);
        Assert.AreEqual(120UL, offline.FriendPoint);
        Assert.IsFalse(offline.Login);
        Assert.AreEqual((ulong)new DateTimeOffset(logout).ToUnixTimeSeconds(), offline.LogOut);
        Assert.AreEqual(0, offline.Channel);
        Assert.AreEqual(0, offline.World);

        var online = FriendBook.ToValue(entry, new PresenceRecord(Guid.NewGuid(), 7, "Bob", 3, 30, 10003, 4, false, 0, 0));

        Assert.IsTrue(online.Login);
        Assert.AreEqual(4, online.Channel);
        Assert.AreEqual(10003, online.World);
        Assert.AreEqual(30, online.Level);
    }

    [TestMethod]
    public void WaitingAndAskedMapToTheNumbersTheClientKnows()
    {
        Assert.AreEqual((byte)FriendState.Friend, (byte)FriendKind.Friend);
        Assert.AreEqual((byte)FriendState.Wait, (byte)FriendKind.Wait);
        Assert.AreEqual((byte)FriendState.Invite, (byte)FriendKind.Invite);
        Assert.AreEqual((byte)FriendState.Block, (byte)FriendKind.Block);
    }

    [TestMethod]
    public void OptionsAreReadByTheDigitOfEachSetting()
    {
        var options = Enumerable.Repeat((byte)'0', 64).ToArray();

        options[0] = (byte)'5';
        options[PersonOptions.Whisper] = (byte)'1';
        options[PersonOptions.OtherInfo] = (byte)'2';

        Assert.AreEqual(PersonOptions.OnlyFriends, PersonOptions.Level(options, PersonOptions.Whisper));
        Assert.AreEqual(PersonOptions.Nobody, PersonOptions.Level(options, PersonOptions.OtherInfo));
        Assert.AreEqual(PersonOptions.AllowAll, PersonOptions.Level(options, 3));
    }

    [TestMethod]
    public void OptionsThatWereNeverSavedAllowEverything()
    {
        // The server fills a new character with ones, and a client that finds that resets its own.
        var options = Enumerable.Repeat((byte)'1', 64).ToArray();

        Assert.AreEqual(PersonOptions.AllowAll, PersonOptions.Level(options, PersonOptions.Whisper));
        Assert.AreEqual(PersonOptions.AllowAll, PersonOptions.Level(null, PersonOptions.OtherInfo));
        Assert.AreEqual(PersonOptions.AllowAll, PersonOptions.Level([(byte)'5'], PersonOptions.OtherInfo));
    }
}
