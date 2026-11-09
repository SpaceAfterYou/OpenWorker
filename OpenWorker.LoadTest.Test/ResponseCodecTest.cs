using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.LoadTest.Protocol;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// What the server writes, with the response types of the server, is read by the decoders of the bot.
/// </summary>
[TestClass]
public sealed class ResponseCodecTest
{
    /// <summary>
    /// The body of a frame as the bot sees it after the framing: the opcode, then what the response writes.
    /// </summary>
    private static byte[] BodyOf<T>(T response) where T : IResponseHotspotMessage
    {
        var frame = Wire.ServerFrame((ushort)response.Opcode.Value, response.Write);

        return Wire.Split(frame).Body;
    }

    private static EnterMapResultValue Place(EnterMapType type, ChangeServerType change)
    {
        return new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = 77,
                Account = 5,
                Server = 1,
                Jump = 10301,
                Map = new MapValue { Channel = 3, Location = 10003, Server = 1 },
                Parent = new MapValue(),
                Address = "10.1.2.3",
                Port = 10011,
                World = new WorldValue(10003, new MapValue { Channel = 3, Location = 10003, Server = 1 }, new Vector3(1.5f, -2.5f, 90.25f), 135f),
                Type = type
            },
            ChangeServer = true,
            ChangeType = change,
            Result = 0
        };
    }

    [TestMethod]
    public void OpcodesAreTheOnesOfTheMessagesOfTheServer()
    {
        Assert.AreEqual(Opcodes.LoginResult, Op(new LoginResponse(LoginErrorMessageCode.None)));
        Assert.AreEqual(Opcodes.GateList, Op(new LoginGateListResponse { Values = [] }));
        Assert.AreEqual(Opcodes.GateConnect, Op(new LoginGateConnectResponse { Address = "x" }));
        Assert.AreEqual(Opcodes.EnterGate, Op(new LoginEnterGateResponse()));
        Assert.AreEqual(Opcodes.PersonSelect, Op(new CharacterSelectResponse()));
        Assert.AreEqual(Opcodes.WorldEnter, Op(new WorldEnterResponse()));
        Assert.AreEqual(Opcodes.MoveBroadcast, Op(new MovementMoveBtResponse()));
        Assert.AreEqual(Opcodes.Ping, Op(new SystemPingResponse()));
        Assert.AreEqual(Opcodes.KeepAlive, Op(new SystemKeepAliveResponse()));
        Assert.AreEqual(Opcodes.ChatNormal, Op(new ChatNormalResponse { Message = "x" }));
        Assert.AreEqual(Opcodes.SkillActive, Op(new SkillActiveResponse()));
        Assert.AreEqual(Opcodes.PersonKickOut, Op(new PersonKickOutResponse()));
        Assert.AreEqual(Opcodes.PersonList, Op(new CharacterListResponse { Persons = [], Protection = default }));

        // The answer of the server to a request that failed is the opcode of the answer with the error flag.
        Assert.AreEqual(Opcodes.ErrorOf(Opcodes.PersonList), Op(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ListRes), PersonErrorCode.NameInUse)));
    }

    private static ushort Op(IHotspotMessage message) => (ushort)message.Opcode.Value;

    [TestMethod]
    public void LoginSuccessGivesTheAccountAndTheSession()
    {
        var session = new SessionValue(5, 77);
        var body = BodyOf(new LoginResponse(session, "bot00001", true, "00-11-22-33-44-55", LoginType.NewUser));

        var result = Responses.ReadLogin(body);

        Assert.AreEqual(5, result.Account);
        Assert.AreEqual("bot00001", result.Login);
        Assert.AreEqual(0, result.ErrorCode);
        Assert.AreEqual(session.Key, result.Session);
    }

    [TestMethod]
    public void LoginRefusalGivesTheCode()
    {
        const LoginErrorMessageCode code = LoginErrorMessageCode.InGameAlready;

        var result = Responses.ReadLogin(BodyOf(new LoginResponse(code)));

        Assert.AreEqual((int)code, result.ErrorCode);
        Assert.AreEqual(0L, result.Session);
    }

    [TestMethod]
    public void GateListGivesTheGates()
    {
        var gate = new GateInfo
        {
            Id = 1,
            Name = "Seoul",
            Workload = GateWorkload.Normal,
            Address = "127.0.0.1",
            Port = 10010,
            OnlineCount = 12
        };

        var body = BodyOf(new LoginGateListResponse
        {
            Values =
            [
                new GateDataInfo { Gate = gate, Person = new GatePersonInfo { Count = 2 } },
                new GateDataInfo { Gate = gate with { Id = 2, Name = "Busan", Workload = GateWorkload.Offline, Address = string.Empty, Port = 0 }, Person = new GatePersonInfo { Count = 0 } }
            ]
        });

        var gates = Responses.ReadGateList(body);

        Assert.HasCount(2, gates);
        Assert.AreEqual(new GateEntry(1, 10010, "Seoul", "127.0.0.1", (int)GateWorkload.Normal, 12, 2), gates[0]);
        Assert.AreEqual(new GateEntry(2, 0, "Busan", string.Empty, (int)GateWorkload.Offline, 12, 0), gates[1]);
    }

    [TestMethod]
    public void GateConnectGivesTheRedirect()
    {
        var redirect = Responses.ReadGateConnect(BodyOf(new LoginGateConnectResponse { Address = "192.168.1.20", Port = 10010 }));

        Assert.AreEqual(new Redirect("192.168.1.20", 10010), redirect);
    }

    [TestMethod]
    public void EnterGateGivesTheAccountOrNothing()
    {
        Assert.AreEqual(42, Responses.ReadEnterGate(BodyOf(new LoginEnterGateResponse { Account = 42 })));
        Assert.IsNull(Responses.ReadEnterGate(BodyOf(LoginEnterGateResponse.Error)));
    }

    [TestMethod]
    public void PersonListGivesTheCharactersAndTheLastOne()
    {
        var first = Requests.TemplatePerson("Lt00001");
        var second = Requests.TemplatePerson("Lt00002");

        var body = BodyOf(new CharacterListResponse
        {
            Persons = [first, second],
            LastPerson = 77,
            Protection = new ProtectionStateValue()
        });

        var (persons, last) = Responses.ReadPersonList(body);

        Assert.HasCount(2, persons);
        Assert.AreEqual("Lt00001", persons[0].Name);
        Assert.AreEqual("Lt00002", persons[1].Name);
        Assert.AreEqual(77, last);

        var (none, _) = Responses.ReadPersonList(BodyOf(new CharacterListResponse { Persons = [], Protection = new ProtectionStateValue() }));

        Assert.IsEmpty(none);
    }

    [TestMethod]
    public void SelectedCharacterGivesTheDistrictToGoTo()
    {
        var body = BodyOf(new CharacterSelectResponse { Zone = Place(EnterMapType.Normal, ChangeServerType.EnterDistrict) });

        var place = Responses.ReadEnterMap(body);

        Assert.AreEqual(77, place.Actor);
        Assert.AreEqual(5, place.Account);
        Assert.AreEqual(10301, place.Jump);
        Assert.AreEqual((ulong)new MapValue { Channel = 3, Location = 10003, Server = 1 }, place.Map);
        Assert.AreEqual("10.1.2.3", place.Address);
        Assert.AreEqual(10011, place.Port);
        Assert.AreEqual((short)10003, place.Location);
        Assert.AreEqual(new Vector3(1.5f, -2.5f, 90.25f), place.Position);
        Assert.AreEqual(135f, place.Rotation);
        Assert.AreEqual((byte)EnterMapType.Normal, place.Type);
        Assert.IsTrue(place.ChangeServer);
        Assert.AreEqual(0, place.Result);
    }

    [TestMethod]
    public void EnteredMazeGivesTheMazeToGoTo()
    {
        var place = Responses.ReadEnterMap(BodyOf(new WorldEnterResponse { Map = Place(EnterMapType.Maze, ChangeServerType.EnterMap) }));

        Assert.AreEqual((byte)EnterMapType.Maze, place.Type);
        Assert.AreEqual("10.1.2.3", place.Address);
        Assert.AreEqual(10011, place.Port);
    }

    [TestMethod]
    public void RefusedEntryGivesTheReason()
    {
        var failed = Responses.ReadEnterMap(BodyOf(new WorldEnterResponse { Map = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) }));

        Assert.AreEqual(EnterMapResultValue.ServerUnavailable, failed.Result);
        Assert.IsFalse(failed.ChangeServer);

        var refused = Responses.ReadEnterMap(BodyOf(new CharacterSelectResponse { Zone = EnterMapResultValue.Failed(EnterMapResultValue.CannotEnter) }));

        Assert.AreEqual(EnterMapResultValue.CannotEnter, refused.Result);
    }

    [TestMethod]
    public void KickOutGivesTheReason()
    {
        var kick = Responses.ReadKickOut(BodyOf(new PersonKickOutResponse
        {
            OutReason = PersonKickOutReason.UserWrongWorld,
            Account = 9,
            Message = "gone"
        }));

        Assert.AreEqual(new KickOut((byte)PersonKickOutReason.UserWrongWorld, 9, "gone"), kick);
    }

    [TestMethod]
    public void ErrorMessageGivesTheCodeUnderTheFlaggedOpcode()
    {
        var response = new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ListRes), PersonErrorCode.NameInUse);
        var body = BodyOf(response);

        Assert.AreEqual(Opcodes.ErrorOf(Opcodes.PersonList), BitConverter.ToUInt16(body));
        Assert.AreEqual((ushort)PersonErrorCode.NameInUse, Responses.ReadErrorCode(body));
    }

    private static readonly int[] ExpectedSkills = [51641260, 51641270];

    [TestMethod]
    public void LearnedSkillsAreTheOnesTheDistrictSends()
    {
        var response = default(SkillLoadInfoResponse) with
        {
            Snapshot = default(SkillSnapshotValue) with
            {
                Target = 77,
                SkillPoint = default,
                DeckSlotCount = 4,
                SkillList =
                [
                    default(SkillInfoValue) with { Skill = 51641260 },
                    default(SkillInfoValue) with { Skill = 51641270, Divergence = 1 }
                ],
                DeckSlotList = [],
                DeckBonus = new ushort[OpenWorker.Hotspot.Modules.Skill.SkillModuleDefines.SkillDeckBonusCount]
            }
        };

        CollectionAssert.AreEqual(ExpectedSkills, Responses.ReadLearnedSkills(BodyOf(response)));
    }

    [TestMethod]
    public void MoveBroadcastHasTheSizeAndTheFieldsTheBotReadsAtFixedPlaces()
    {
        var body = BodyOf(new MovementMoveBtResponse
        {
            Actor = new ActorValue(321, ActorType.User),
            Map = new MapValue { Channel = 2, Location = 10003, Server = 1 },
            Pos = new Vector3(10228.605f, 10058.951f, 90.452f),
            Yaw = 90f,
            TargetPosX = 1f,
            TargetPosY = 2f,
            RunBit = 1,
            Pitch = 3f,
            MoveSpeed = 560f
        });

        Assert.HasCount(Responses.MoveBroadcastSize, body);
        Assert.AreEqual(321, Responses.ReadMoveActor(body) & 0x1FFFFFFF);

        var (x, y) = Responses.ReadMovePosition(body);

        Assert.AreEqual(10228.605f, x);
        Assert.AreEqual(10058.951f, y);
    }

    [TestMethod]
    public void PingAndKeepAliveBringTheirNumbersBack()
    {
        Assert.AreEqual(1234567890123L, Responses.ReadPingToken(BodyOf(new SystemPingResponse { KeepAlive = 1234567890123L })));
        Assert.AreEqual(TimeSpan.FromSeconds(30).Ticks, Responses.ReadKeepAliveTicks(BodyOf(new SystemKeepAliveResponse { Time = TimeSpan.FromSeconds(30) })));
    }

    [TestMethod]
    public void SkillResultIsTheErrorCode()
    {
        Assert.AreEqual((short)0, Responses.ReadSkillResult(BodyOf(default(SkillActiveResponse) with { Divergence = 5 })));
        Assert.AreEqual((short)7, Responses.ReadSkillResult(BodyOf(default(SkillActiveResponse) with { ErrorCode = 7 })));
    }

    [TestMethod]
    public void ChatGivesTheSpeakerAndTheNumberOfTheLine()
    {
        var body = BodyOf(new ChatNormalResponse
        {
            Actor = new ActorComponent(321, ActorType.User),
            Appearance = ChatMessageAppearance.Normal,
            Message = "184467"
        });

        Assert.IsTrue(Responses.TryReadChat(body, out var actor, out var number));
        Assert.AreEqual(321, actor & 0x1FFFFFFF);
        Assert.AreEqual(184467L, number);
    }

    [TestMethod]
    public void ChatOfSomebodyElseHasNoNumber()
    {
        var body = BodyOf(new ChatNormalResponse
        {
            Actor = new ActorComponent(1, ActorType.User),
            Appearance = ChatMessageAppearance.Normal,
            Message = "hello"
        });

        Assert.IsFalse(Responses.TryReadChat(body, out _, out _));
        Assert.IsFalse(Responses.TryReadChat(body.AsSpan(0, 5), out _, out _));
    }

    [TestMethod]
    public void CutPacketIsAnErrorOfTheProtocolNotAnIndexOutOfRange()
    {
        var body = BodyOf(new LoginGateConnectResponse { Address = "192.168.1.20", Port = 10010 });

        Assert.Throws<InvalidDataException>(() => Responses.ReadGateConnect(body.AsSpan(0, body.Length - 1)));
        Assert.Throws<InvalidDataException>(() => Responses.ReadGateConnect(body.AsSpan(0, 1)));
    }
}
