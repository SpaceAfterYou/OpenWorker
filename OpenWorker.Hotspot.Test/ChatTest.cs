using System.Text;
using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Test;

[TestClass]
[DoNotParallelize]
public sealed class ChatTest
{
    private sealed class Party(params int[] members) : IPartyRoster
    {
        public ValueTask<IReadOnlyList<int>> MembersAsync(int person) =>
            ValueTask.FromResult<IReadOnlyList<int>>(members.Contains(person) ? members : []);

        public ValueTask<PartyView?> PartyAsync(int person) =>
            ValueTask.FromResult(members.Contains(person) ? new PartyView(1, members[0], members) : null);
    }

    private sealed class League(params int[] members) : ILeagueRoster
    {
        public ValueTask<IReadOnlyList<int>> MembersAsync(int league) => ValueTask.FromResult<IReadOnlyList<int>>(members);
    }

    private sealed class Relations : IPersonRelations
    {
        public HashSet<(int Person, int Other)> Friends { get; } = [];
        public HashSet<(int Person, int Other)> Blocks { get; } = [];

        public ValueTask<bool> IsFriendAsync(int person, int other) => ValueTask.FromResult(Friends.Contains((person, other)));

        public ValueTask<bool> IsBlockedAsync(int person, int other) => ValueTask.FromResult(Blocks.Contains((person, other)));
    }

    private sealed class Megaphone(ushort error) : IMegaphoneSource
    {
        public int Taken { get; private set; }

        public ValueTask<ushort> TakeAsync(Entity player, StorageGroup storage, short slot)
        {
            Taken++;

            return ValueTask.FromResult(error);
        }
    }

    private sealed class Service(
        TestServer server,
        IPartyRoster? parties = null,
        ILeagueRoster? leagues = null,
        IPersonRelations? relations = null,
        IMegaphoneSource? megaphones = null
    ) :
        ChatServiceBase(
            server.World,
            server.Network,
            new ChatThrottle(),
            parties ?? new Party(),
            leagues ?? new League(),
            relations ?? new Relations(),
            megaphones ?? new Megaphone(0))
    {
        protected override void SendNearby(Entity player, IResponseHotspotMessage message)
        {
            foreach (var other in server.Network.Online)
            {
                server.World.Get<ServerSessionComponent>(other).Send(message);
            }
        }

        protected override ValueTask<bool> TryCommandAsync(Entity player, string text) => ValueTask.FromResult(false);
    }

    private static void Text(BinaryWriter writer, string value)
    {
        writer.Write((short)(2 * value.Length + 1));
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((byte)0);
    }

    private static string ReadText(BinaryReader reader)
    {
        var length = reader.ReadInt16();

        return Encoding.Unicode.GetString(reader.ReadBytes(length), 0, length - 1);
    }

    private static BinaryReader Body(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();
        stream.Position = 0;

        return new BinaryReader(stream);
    }

    private static ChatNormalRequest Normal(ChatMessageAppearance appearance, string message) =>
        new(Body(w =>
        {
            w.Write((byte)appearance);
            Text(w, message);
        }));

    private static ChatWhisperRequest Whisper(string receiver, string message, string sender = "Whoever") =>
        new(Body(w =>
        {
            Text(w, sender);
            Text(w, receiver);
            Text(w, message);
            w.Write(0);
        }));

    private static ChatTradeRequest Trade(string message) => new(Body(w => Text(w, message)));

    private static ChatMegaphoneRequest MegaphoneOf(string message, string name = "Whoever") =>
        new(Body(w =>
        {
            w.Write((byte)StorageGroup.Common);
            w.Write((short)4);
            w.Write(12345);
            Text(w, name);
            Text(w, message);
        }));

    private static ServiceHandleContext Context(TestPlayer player) => new(player.Entity, CancellationToken.None);

    private static void Say(TestPlayer player, Service service, Func<ServiceHandleContext, ValueTask> handle)
    {
        player.Server.Run(async () => await handle(Context(player)));
    }

    private static (int Actor, int Type, string Message) NormalOf(ReceivedFrame frame)
    {
        using var reader = new BinaryReader(new MemoryStream(frame.Content));

        return (reader.ReadInt32(), reader.ReadInt32(), ReadText(reader));
    }

    private static (string Sender, string Receiver, string Message, int Result) WhisperOf(ReceivedFrame frame)
    {
        using var reader = new BinaryReader(new MemoryStream(frame.Content));

        return (ReadText(reader), ReadText(reader), ReadText(reader), reader.ReadInt32());
    }

    [TestMethod]
    public void NormalChatIsHeardByThoseNearby()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);
        using var other = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");
        using var bob = server.Join(2, "Bob");
        using var carol = other.Join(3, "Carol");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Normal, "hello")));

        foreach (var heard in new[] { alice, bob })
        {
            var frames = heard.Read();

            Assert.HasCount(1, frames);
            Assert.AreEqual((byte)ChatOpcode.Normal, frames[0].Command);
            Assert.AreEqual((1, (int)ChatMessageAppearance.Normal, "hello"), NormalOf(frames[0]));
        }

        // Another server has other ground.
        Assert.IsEmpty(carol.Read());
    }

    [TestMethod]
    public void EmptyAndOversizedWordsAreNotSaid()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Normal, "   ")));
        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Normal, new string('a', 300))));

        Assert.IsEmpty(alice.Read());
    }

    [TestMethod]
    public void ASixthMessageInThreeSecondsIsDropped()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");

        for (var i = 0; i < 8; i++)
        {
            var text = $"line {i}";

            Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Normal, text)));
        }

        Assert.HasCount(5, alice.Read());
    }

    [TestMethod]
    public void PartyChatReachesTheWholePartyOnEveryServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var service = new Service(first, parties: new Party(1, 2, 3));

        using var alice = first.Join(1, "Alice");
        using var bob = second.Join(2, "Bob");
        using var carol = first.Join(3, "Carol");
        using var dave = second.Join(4, "Dave");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Party, "go")));

        foreach (var heard in new[] { alice, bob, carol })
        {
            Assert.AreEqual((1, (int)ChatMessageAppearance.Party, "go"), NormalOf(heard.Read().Single()));
        }

        Assert.IsEmpty(dave.Read());
    }

    [TestMethod]
    public void PartyChatOfAPersonAloneComesBackToThem()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server, parties: new Party());

        using var alice = server.Join(1, "Alice");
        using var bob = server.Join(2, "Bob");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Party, "anyone?")));

        Assert.HasCount(1, alice.Read());
        Assert.IsEmpty(bob.Read());
    }

    [TestMethod]
    public void LeagueChatReachesTheMembersAndNobodyWithoutALeague()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var service = new Service(first, leagues: new League(1, 2));

        using var alice = first.Join(1, "Alice", league: 9);
        using var bob = second.Join(2, "Bob", league: 9);
        using var carol = second.Join(3, "Carol");
        using var loner = first.Join(5, "Loner");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.League, "hi league")));
        Say(loner, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.League, "hi nobody")));

        Assert.AreEqual((1, (int)ChatMessageAppearance.League, "hi league"), NormalOf(alice.Read().Single()));
        Assert.AreEqual((1, (int)ChatMessageAppearance.League, "hi league"), NormalOf(bob.Read().Single()));
        Assert.IsEmpty(carol.Read());
        Assert.IsEmpty(loner.Read());
    }

    [TestMethod]
    public void OtherKindsOfWordsAreNotSaidThroughTheNormalChat()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");

        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Notice, "fake notice")));
        Say(alice, service, c => service.OnHandleAsync(c, Normal(ChatMessageAppearance.Yell, "yell")));

        Assert.IsEmpty(alice.Read());
    }

    [TestMethod]
    public void AWhisperGoesToBothAcrossServersWithTheNamesOfTheServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var service = new Service(first);

        using var alice = first.Join(1, "Alice");
        using var bob = second.Join(2, "Bob");
        using var carol = second.Join(3, "Carol");

        // The sender named in the request is not believed.
        Say(alice, service, c => service.OnHandleAsync(c, Whisper("bob", "psst", sender: "Mallory")));

        foreach (var heard in new[] { alice, bob })
        {
            var frame = heard.Read().Single();

            Assert.AreEqual((byte)ChatOpcode.Whisper, frame.Command);
            Assert.AreEqual(("Alice", "Bob", "psst", 0), WhisperOf(frame));
        }

        Assert.IsEmpty(carol.Read());
    }

    [TestMethod]
    public void AWhisperToNobodyIsAnError()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");

        Say(alice, service, c => service.OnHandleAsync(c, Whisper("Ghost", "boo")));

        var frame = alice.Read().Single();

        Assert.AreEqual((byte)ChatOpcode.Whisper | 0x80, frame.Command);
        Assert.AreEqual(ChatServiceBase.WhisperError.NotOnline, BitConverter.ToUInt16(frame.Content));
    }

    [TestMethod]
    public void AWhisperToYourselfIsAnError()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");

        Say(alice, service, c => service.OnHandleAsync(c, Whisper("ALICE", "me")));

        var frame = alice.Read().Single();

        Assert.AreEqual((byte)ChatOpcode.Whisper | 0x80, frame.Command);
        Assert.AreEqual(ChatServiceBase.WhisperError.ToSelf, BitConverter.ToUInt16(frame.Content));
    }

    [TestMethod]
    public void AWhisperToSomeoneWhoTakesNoneIsRefusedToTheSenderAlone()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var service = new Service(first);

        using var alice = first.Join(1, "Alice");
        using var bob = second.Join(2, "Bob", whisper: PersonOptions.Nobody);

        Say(alice, service, c => service.OnHandleAsync(c, Whisper("Bob", "hello")));

        Assert.AreEqual(("Alice", "Bob", "hello", ChatServiceBase.WhisperResult.Refused), WhisperOf(alice.Read().Single()));
        Assert.IsEmpty(bob.Read());
    }

    [TestMethod]
    public void AWhisperToSomeoneWhoTakesOnlyFriendsNeedsFriendship()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var relations = new Relations();
        var service = new Service(first, relations: relations);

        using var alice = first.Join(1, "Alice");
        using var carol = first.Join(3, "Carol");
        using var bob = second.Join(2, "Bob", whisper: PersonOptions.OnlyFriends);

        relations.Friends.Add((2, 1));

        Say(alice, service, c => service.OnHandleAsync(c, Whisper("Bob", "from a friend")));
        Say(carol, service, c => service.OnHandleAsync(c, Whisper("Bob", "from a stranger")));

        Assert.AreEqual(0, WhisperOf(alice.Read().Single()).Result);
        Assert.AreEqual(ChatServiceBase.WhisperResult.Refused, WhisperOf(carol.Read().Single()).Result);

        var heard = bob.Read();

        Assert.HasCount(1, heard);
        Assert.AreEqual("from a friend", WhisperOf(heard[0]).Message);
    }

    [TestMethod]
    public void AWhisperToSomeoneYouBlockedIsNotSent()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var relations = new Relations();
        var service = new Service(server, relations: relations);

        using var alice = server.Join(1, "Alice");
        using var bob = server.Join(2, "Bob");

        relations.Blocks.Add((1, 2));

        Say(alice, service, c => service.OnHandleAsync(c, Whisper("Bob", "sorry")));

        Assert.AreEqual(("Alice", "Bob", "sorry", ChatServiceBase.WhisperResult.Blocked), WhisperOf(alice.Read().Single()));
        Assert.IsEmpty(bob.Read());
    }

    [TestMethod]
    public void TradeChatIsHeardByTheNeighboursWithTheNameOfTheServer()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);

        var service = new Service(server);

        using var alice = server.Join(1, "Alice");
        using var bob = server.Join(2, "Bob");

        Say(alice, service, c => service.OnHandleAsync(c, Trade("selling")));

        foreach (var heard in new[] { alice, bob })
        {
            var frame = heard.Read().Single();
            using var reader = new BinaryReader(new MemoryStream(frame.Content));

            Assert.AreEqual((byte)ChatOpcode.Trade, frame.Command);
            Assert.AreEqual("Alice", ReadText(reader));
            Assert.AreEqual("selling", ReadText(reader));
        }
    }

    [TestMethod]
    public void AMegaphoneIsHeardOnEveryServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var items = new Megaphone(0);
        var service = new Service(first, megaphones: items);

        using var alice = first.Join(1, "Alice");
        using var bob = second.Join(2, "Bob");

        Say(alice, service, c => service.OnHandleAsync(c, MegaphoneOf("everybody!")));

        Assert.AreEqual(1, items.Taken);

        foreach (var heard in new[] { alice, bob })
        {
            var frame = heard.Read().Single();
            using var reader = new BinaryReader(new MemoryStream(frame.Content));

            Assert.AreEqual((byte)ChatOpcode.Megaphone, frame.Command);

            reader.ReadByte();
            reader.ReadInt16();

            // The number of the speaker, not the one in the request.
            Assert.AreEqual(1, reader.ReadInt32());
            Assert.AreEqual("Alice", ReadText(reader));
            Assert.AreEqual("everybody!", ReadText(reader));
        }
    }

    [TestMethod]
    public void AMegaphoneWithoutTheItemIsAnError()
    {
        var community = new MemoryCommunity();

        using var server = new TestServer(community);
        using var other = new TestServer(community);

        var service = new Service(server, megaphones: new Megaphone(52011));

        using var alice = server.Join(1, "Alice");
        using var bob = other.Join(2, "Bob");

        Say(alice, service, c => service.OnHandleAsync(c, MegaphoneOf("no item")));

        var frame = alice.Read().Single();

        Assert.AreEqual((byte)ChatOpcode.Megaphone | 0x80, frame.Command);
        Assert.AreEqual(52011, BitConverter.ToUInt16(frame.Content));
        Assert.IsEmpty(bob.Read());
    }
}
