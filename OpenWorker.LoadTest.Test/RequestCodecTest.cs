using System.Numerics;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.LoadTest.Protocol;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// The requests of the bot are read back with the request types of the server, and, where a real client's packet
/// was saved, compared to it byte for byte.
/// </summary>
[TestClass]
public sealed class RequestCodecTest
{
    private static void AssertConsumed(BinaryReader reader)
    {
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position, "The request has bytes the server does not read.");
    }

    private static void AssertSameAsDump(Dump dump, byte[] ours)
    {
        CollectionAssert.AreEqual(dump.WireFrame(), ours, "The bot's frame differs from the one the client sent.");
    }

    [TestMethod]
    public void OpcodesAreTheOnesOfTheMessagesOfTheServer()
    {
        Assert.AreEqual(default(LoginAuthRequest).Opcode, Of(Opcodes.LoginRequest));
        Assert.AreEqual(default(LoginGateListRequest).Opcode, Of(Opcodes.GateListRequest));
        Assert.AreEqual(default(LoginGateConnectRequest).Opcode, Of(Opcodes.GateConnectRequest));
        Assert.AreEqual(default(LoginEnterServerRequest).Opcode, Of(Opcodes.EnterGateRequest));
        Assert.AreEqual(default(PersonListRequest).Opcode, Of(Opcodes.PersonListRequest));
        Assert.AreEqual(default(PersonCreateRequest).Opcode, Of(Opcodes.PersonCreateRequest));
        Assert.AreEqual(default(PersonSelectRequest).Opcode, Of(Opcodes.PersonSelectRequest));
        Assert.AreEqual(default(PersonEnterGameServerRequest).Opcode, Of(Opcodes.PersonEnterRequest));
        Assert.AreEqual(default(WorldObjectRequest).Opcode, Of(Opcodes.WorldObjectRequest));
        Assert.AreEqual(default(MazeCreateRequest).Opcode, Of(Opcodes.MazeCreateRequest));
        Assert.AreEqual(default(MovementMoveRequest).Opcode, Of(Opcodes.MoveRequest));
        Assert.AreEqual(default(SystemPingRequest).Opcode, Of(Opcodes.Ping));
        Assert.AreEqual(default(SystemKeepAliveRequest).Opcode, Of(Opcodes.KeepAlive));
        Assert.AreEqual(default(ChatNormalRequest).Opcode, Of(Opcodes.ChatNormal));
        Assert.AreEqual(default(ChannelChangeRequest).Opcode, Of(Opcodes.ChannelChangeRequest));
        Assert.AreEqual(default(MazeCompleteRequest).Opcode, Of(Opcodes.MazeCompleteRequest));
        Assert.AreEqual(default(SkillActiveSkillRequest).Opcode, Of(Opcodes.SkillActiveRequest));
    }

    private static short Of(ushort opcode) => (short)opcode;

    [TestMethod]
    public void LoginIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.Login(ref w, "bot00001", "secret", "00-11-22-33-44-55")));

        using var reader = frame.Reader();
        var request = new LoginAuthRequest(reader);

        Assert.AreEqual("bot00001", request.Username);
        Assert.AreEqual("secret", request.Password);
        Assert.AreEqual("00-11-22-33-44-55", request.MacAddress);
        AssertConsumed(reader);

        var dump = Dump.Read(0x02, 0x01);
        using var original = dump.Reader();
        var sent = new LoginAuthRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.Login(ref w, sent.Username, sent.Password, sent.MacAddress)));
    }

    [TestMethod]
    public void GateListIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.GateList(ref w, 4711)));

        using var reader = frame.Reader();

        Assert.AreEqual(4711, new LoginGateListRequest(reader).Account);
        AssertConsumed(reader);

        var dump = Dump.Read(0x02, 0x03);
        using var original = dump.Reader();

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.GateList(ref w, new LoginGateListRequest(original).Account)));
    }

    [TestMethod]
    public void GateConnectIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.GateConnect(ref w, 3)));

        using var reader = frame.Reader();

        Assert.AreEqual((short)3, new LoginGateConnectRequest(reader).Gate);
        AssertConsumed(reader);

        var dump = Dump.Read(0x02, 0x05);
        using var original = dump.Reader();

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.GateConnect(ref w, new LoginGateConnectRequest(original).Gate)));
    }

    [TestMethod]
    public void EnterGateIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.EnterGate(ref w, 7, 2, 0x1234_5678_9ABC_DEF0)));

        using var reader = frame.Reader();
        var request = new LoginEnterServerRequest(reader);

        Assert.AreEqual(7, request.Account);
        Assert.AreEqual((short)2, request.Gate);
        Assert.AreEqual(0x1234_5678_9ABC_DEF0, request.Session.Key);
        AssertConsumed(reader);

        var dump = Dump.Read(0x02, 0x13);
        using var original = dump.Reader();
        var sent = new LoginEnterServerRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.EnterGate(ref w, sent.Account, sent.Gate, sent.Session.Key)));
    }

    [TestMethod]
    public void PersonListIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.PersonList(ref w, -5)));

        using var reader = frame.Reader();

        Assert.AreEqual(-5L, new PersonListRequest(reader).Session);
        AssertConsumed(reader);

        var dump = Dump.Read(0x03, 0x11);
        using var original = dump.Reader();

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.PersonList(ref w, new PersonListRequest(original).Session)));
    }

    [TestMethod]
    public void PersonCreateIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.PersonCreate(ref w, "Lt00001")));

        using var reader = frame.Reader();
        var request = new PersonCreateRequest(reader);

        Assert.AreEqual("Lt00001", request.Person.InfoValue.Name);
        Assert.AreEqual(Requests.Template.Hero, request.Person.InfoValue.Hero);
        Assert.AreEqual(Requests.Template.HairStyle, request.Person.InfoValue.AppearanceShape.HairStyle);
        Assert.AreEqual(Requests.Template.HairColor, request.Person.InfoValue.AppearanceShape.HairColor);
        Assert.AreEqual(Requests.Template.EyeColor, request.Person.InfoValue.AppearanceShape.EyeColor);
        Assert.AreEqual(Requests.Template.SkinColor, request.Person.InfoValue.AppearanceShape.SkinColor);
        Assert.AreEqual(Requests.Template.Outfit, request.Outfit);
        AssertConsumed(reader);

        // The template of the bot is what the creation screen of the real client sent.
        var dump = Dump.Read(0x03, 0x01);
        using var original = dump.Reader();
        var sent = new PersonCreateRequest(original);

        Assert.AreEqual(Hero.Stella, sent.Person.InfoValue.Hero);
        Assert.AreEqual(Requests.Template.Outfit, sent.Outfit, "The bot does not send the outfit of the dump.");

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.PersonCreate(ref w, sent.Person.InfoValue.Name)));
    }

    [TestMethod]
    public void PersonSelectIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.PersonSelect(ref w, 12, 34, true)));

        using var reader = frame.Reader();
        var request = new PersonSelectRequest(reader);

        Assert.AreEqual(12, request.Actor.Identifier);
        Assert.AreEqual(34, request.Account);
        Assert.IsTrue(request.IsClearTutorial);
        AssertConsumed(reader);

        var dump = Dump.Read(0x03, 0x13);
        using var original = dump.Reader();
        var sent = new PersonSelectRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.PersonSelect(ref w, sent.Actor.Identifier, sent.Account, sent.IsClearTutorial)));
    }

    [TestMethod]
    public void PersonEnterIsReadByTheServerAndEqualsTheDump()
    {
        var map = 0x0001_2713_0100_0000UL;
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.PersonEnter(ref w, 5, 6, map, true, 99)));

        using var reader = frame.Reader();
        var request = new PersonEnterGameServerRequest(reader);

        Assert.AreEqual(5, request.Account);
        Assert.AreEqual(6, request.Actor.Identifier);
        Assert.AreEqual(map, (ulong)request.Map);
        Assert.IsTrue(request.FirstConnect);
        Assert.AreEqual(99L, request.Session.Key);
        AssertConsumed(reader);

        var dump = Dump.Read(0x03, 0x21);
        using var original = dump.Reader();
        var sent = new PersonEnterGameServerRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) =>
            Requests.PersonEnter(ref w, sent.Account, sent.Actor.Identifier, sent.Map, sent.FirstConnect, sent.Session.Key)));
    }

    [TestMethod]
    public void WorldObjectCarriesOnlyTheOpcodeAndEqualsTheDump()
    {
        var ours = Wire.Encode((ref FrameWriter w) => Requests.WorldObject(ref w));

        Assert.AreEqual(2, Wire.Split(ours).Body.Length);

        AssertSameAsDump(Dump.Read(0x04, 0x06), ours);
    }

    [TestMethod]
    public void MazeCreateIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.MazeCreate(ref w, 21311, 21107)));

        using var reader = frame.Reader();
        var request = new MazeCreateRequest(reader);

        Assert.AreEqual((short)21311, request.Location);
        Assert.AreEqual(21107, request.Portal);
        Assert.AreEqual(0, request.Account);
        Assert.AreEqual(ChangeServerType.EnterMap, request.ChangeType);
        AssertConsumed(reader);

        // The client asked for this maze from a district.
        var dump = Dump.Read(0x04, 0x41);
        using var original = dump.Reader();
        var sent = new MazeCreateRequest(original);

        Assert.AreEqual((short)21311, sent.Location);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.MazeCreate(ref w, sent.Location, sent.Portal)));
    }

    [TestMethod]
    public void MoveIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) =>
            Requests.Move(ref w, 9, 0x0001_2713_0100_0000UL, new Vector3(1.5f, 2.5f, 3.5f), 90f, 10f, 20f, true, -5.75f, 560f, 0)));

        using var reader = frame.Reader();
        var request = new MovementMoveRequest(reader);

        Assert.AreEqual(9, request.dwActorID.Identifier);
        Assert.AreEqual(1.5f, request.fPosX);
        Assert.AreEqual(2.5f, request.fPosY);
        Assert.AreEqual(3.5f, request.fPosZ);
        Assert.AreEqual(90f, request.fYaw);
        Assert.AreEqual(10f, request.fTargetPosX);
        Assert.AreEqual(20f, request.fTargetPosY);
        Assert.AreEqual((byte)1, request.byRunBit);
        Assert.AreEqual(-5.75f, request.fPitch);
        Assert.AreEqual(560f, request.fMoveSpeed);
        AssertConsumed(reader);

        var dump = Dump.Read(0x05, 0x01);
        using var original = dump.Reader();
        var sent = new MovementMoveRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.Move(
            ref w,
            sent.dwActorID,
            sent.nMapID,
            new Vector3(sent.fPosX, sent.fPosY, sent.fPosZ),
            sent.fYaw,
            sent.fTargetPosX,
            sent.fTargetPosY,
            sent.byRunBit != 0,
            sent.fPitch,
            sent.fMoveSpeed,
            sent.byChangeMotion)));
    }

    [TestMethod]
    public void PingAndKeepAliveAreReadByTheServerAndEqualTheDumps()
    {
        var ping = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.Ping(ref w, 123456789012L)));

        using (var reader = ping.Reader())
        {
            Assert.AreEqual(123456789012L, new SystemPingRequest(reader).TickCount);
            AssertConsumed(reader);
        }

        var keepAlive = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.KeepAlive(ref w, 42)));

        using (var reader = keepAlive.Reader())
        {
            Assert.AreEqual(42L, new SystemKeepAliveRequest(reader).TickCount);
            AssertConsumed(reader);
        }

        var pingDump = Dump.Read(0x01, 0x06);
        using var pingOriginal = pingDump.Reader();

        AssertSameAsDump(pingDump, Wire.Encode((ref FrameWriter w) => Requests.Ping(ref w, new SystemPingRequest(pingOriginal).TickCount)));

        var keepAliveDump = Dump.Read(0x01, 0x05);
        using var keepAliveOriginal = keepAliveDump.Reader();

        AssertSameAsDump(keepAliveDump, Wire.Encode((ref FrameWriter w) => Requests.KeepAlive(ref w, new SystemKeepAliveRequest(keepAliveOriginal).TickCount)));
    }

    [TestMethod]
    public void ChatIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.Chat(ref w, "hello 42")));

        using var reader = frame.Reader();
        var request = new ChatNormalRequest(reader);

        Assert.AreEqual(ChatMessageAppearance.Normal, request.Appearance);
        Assert.AreEqual("hello 42", request.Message);
        AssertConsumed(reader);

        var dump = Dump.Read(0x07, 0x01);
        using var original = dump.Reader();
        var sent = new ChatNormalRequest(original);

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.Chat(ref w, sent.Message)));
    }

    [TestMethod]
    public void ChannelChangeIsReadByTheServerAndEqualsTheDump()
    {
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.ChannelChange(ref w, 7)));

        using var reader = frame.Reader();

        Assert.AreEqual((short)7, new ChannelChangeRequest(reader).Channel);
        AssertConsumed(reader);

        var dump = Dump.Read(0xF1, 0x02);
        using var original = dump.Reader();

        AssertSameAsDump(dump, Wire.Encode((ref FrameWriter w) => Requests.ChannelChange(ref w, new ChannelChangeRequest(original).Channel)));
    }

    [TestMethod]
    public void MazeCompleteCarriesOnlyTheOpcodeAndEqualsTheDump()
    {
        var ours = Wire.Encode((ref FrameWriter w) => Requests.MazeComplete(ref w));

        Assert.AreEqual(2, Wire.Split(ours).Body.Length);

        AssertSameAsDump(Dump.Read(0x11, 0x22), ours);
    }

    [TestMethod]
    public void SkillActiveIsReadByTheServer()
    {
        // No real client's packet of this request was saved, so the server's reader is all there is to check against.
        var frame = Wire.Split(Wire.Encode((ref FrameWriter w) => Requests.SkillActive(ref w, 51641260, new Vector3(1f, 2f, 3f), 45f)));

        using var reader = frame.Reader();
        var request = new SkillActiveSkillRequest(reader);

        Assert.AreEqual(51641260, request.Skill.Skill);
        Assert.AreEqual(new Vector3(1f, 2f, 3f), request.Skill.Position.Position);
        Assert.AreEqual(45f, request.Skill.Position.Angle);
        Assert.AreEqual((short)-1, request.Skill.Position.MotionClass);
        Assert.AreEqual(0, request.Skill.Divergence);
        Assert.AreEqual(0, request.Skill.RandomTrapIndex);
        AssertConsumed(reader);
    }

    [TestMethod]
    public void SeveralFramesGoOutInOneBufferAndEachCarriesItsOwnHeader()
    {
        var bytes = Wire.Encode((ref FrameWriter w) =>
        {
            Requests.Ping(ref w, 1);
            Requests.WorldObject(ref w);
            Requests.Ping(ref w, 2);
        });

        var frames = Wire.SplitAll(bytes);

        Assert.HasCount(3, frames);
        Assert.AreEqual(Opcodes.Ping, frames[0].Opcode);
        Assert.AreEqual(Opcodes.WorldObjectRequest, frames[1].Opcode);
        Assert.AreEqual(Opcodes.Ping, frames[2].Opcode);

        using var reader = frames[2].Reader();

        Assert.AreEqual(2L, new SystemPingRequest(reader).TickCount);
    }

    [TestMethod]
    public void ChatLongerThanTheServerAllowsIsRefused()
    {
        var text = new string('x', Requests.MaxChatLength + 1);

        Assert.Throws<ArgumentException>(() => Wire.Encode((ref FrameWriter w) => Requests.Chat(ref w, text)));
    }

    [TestMethod]
    public void LongestChatFitsTheFrameBuffer()
    {
        var text = new string('x', Requests.MaxChatLength);
        var ours = Wire.Encode((ref FrameWriter w) => Requests.Chat(ref w, text));

        Assert.IsLessThan(Requests.MaxFrameSize, ours.Length);
    }
}
