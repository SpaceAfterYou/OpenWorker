using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Channels;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Messages.Response.World;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Login.Enums;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Skill;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.LoadTest.Protocol;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// A stand-in for the servers, so the whole script of a bot can run without the client data that the real district,
/// gate and maze need. It speaks through the types of the server: requests are read with the request types and
/// answers are written with the response types, so it is as far from the codec of the bot as the real servers are.
/// It does what the servers do that the bot depends on: the auth server signs accounts in, the gate keeps their
/// characters and sends them to the district, the district puts everybody in one channel, broadcasts moves and
/// lines to all of them and answers pings, and the maze answers its player alone.
/// </summary>
internal sealed class FakeWorld : IAsyncDisposable
{
    private enum Role
    {
        Auth,
        Gate,
        District,
        Maze
    }

    private readonly CancellationTokenSource _stop = new();
    private readonly List<TcpListener> _listeners = [];
    private readonly List<Task> _loops = [];
    private readonly ConcurrentDictionary<long, int> _sessions = new();
    private readonly ConcurrentDictionary<int, string> _characters = new();
    private readonly ConcurrentDictionary<Connection, byte> _firstDistrict = new();
    private readonly ConcurrentDictionary<Connection, byte> _secondDistrict = new();
    private readonly Dictionary<Role, Dictionary<ushort, Action<Connection, BinaryReader>>> _handlers = [];

    private long _logins;
    private long _creations;
    private long _moves;
    private long _pings;
    private long _chats;
    private long _channelChanges;
    private readonly ConcurrentDictionary<short, int> _channelsAskedFor = new();
    private long _mazeCompletes;
    private long _skills;
    private long _connections;
    private long _firstEntries;
    private long _secondEntries;

    private FakeWorld(string prefix, string password)
    {
        Prefix = prefix;
        Password = password;

        _handlers[Role.Auth] = new()
        {
            [Op<LoginAuthRequest>()] = OnLogin,
            [Op<LoginGateListRequest>()] = OnGateList,
            [Op<LoginGateConnectRequest>()] = OnGateConnect
        };

        _handlers[Role.Gate] = new()
        {
            [Op<LoginEnterServerRequest>()] = OnEnterGate,
            [Op<PersonListRequest>()] = OnPersonList,
            [Op<PersonCreateRequest>()] = OnPersonCreate,
            [Op<PersonSelectRequest>()] = OnPersonSelect
        };

        _handlers[Role.District] = new()
        {
            [Op<PersonEnterGameServerRequest>()] = OnDistrictEnter,
            [Op<SystemPingRequest>()] = OnPing,
            [Op<SystemKeepAliveRequest>()] = (_, _) => { },
            [Op<WorldObjectRequest>()] = (_, _) => { },
            [Op<MovementMoveRequest>()] = OnMove,
            [Op<ChatNormalRequest>()] = OnChat,
            [Op<ChannelChangeRequest>()] = OnChannelChange,
            [Op<MazeCreateRequest>()] = OnMazeCreate,
            [Op<WorldEnterRequest>()] = OnWorldEnter
        };

        _handlers[Role.Maze] = new()
        {
            [Op<PersonEnterGameServerRequest>()] = OnMazeEnter,
            [Op<MazeCompleteRequest>()] = (_, _) => Interlocked.Increment(ref _mazeCompletes),
            [Op<MovementMoveRequest>()] = OnMazeMove,
            [Op<SkillActiveSkillRequest>()] = OnSkill
        };
    }

    public string Prefix { get; }

    public string Password { get; }

    public int AuthPort { get; private set; }

    public int GatePort { get; private set; }

    public int DistrictPort { get; private set; }

    public int SecondDistrictPort { get; private set; }

    public int MazePort { get; private set; }

    /// <summary>
    /// The location of the second district, which a bot travels to when it is asked to.
    /// </summary>
    public const short SecondLocation = 10021;

    public const short FirstLocation = 10003;

    public const int LearnedSkill = 51641260;

    /// <summary>
    /// The actor of the monster that walks in the maze, which is not a bot.
    /// </summary>
    public const int MonsterActor = 7_000_001;

    /// <summary>
    /// Makes the district answer a request for a maze with a refusal.
    /// </summary>
    public bool RefuseMaze { get; set; }

    /// <summary>
    /// Makes the district close the connection of everybody who enters, as it does for a connection without a reservation.
    /// </summary>
    public bool TurnAwayFromDistrict { get; set; }

    /// <summary>
    /// How long the district waits before it answers a ping, so the latency of a test is not zero.
    /// </summary>
    public TimeSpan PongDelay { get; set; }

    /// <summary>
    /// Closes the connection of everybody in the districts, as a server does that stops.
    /// </summary>
    public void DropPlayers()
    {
        foreach (var connection in _firstDistrict.Keys.Concat(_secondDistrict.Keys))
        {
            connection.Complete();
        }
    }

    public long Logins => Interlocked.Read(ref _logins);

    public long Creations => Interlocked.Read(ref _creations);

    public long Moves => Interlocked.Read(ref _moves);

    public long Pings => Interlocked.Read(ref _pings);

    public long Chats => Interlocked.Read(ref _chats);

    public long ChannelChanges => Interlocked.Read(ref _channelChanges);

    /// <summary>
    /// How many bots asked for each channel.
    /// </summary>
    public IReadOnlyDictionary<short, int> ChannelsAskedFor => _channelsAskedFor;

    public long MazeCompletes => Interlocked.Read(ref _mazeCompletes);

    public long Skills => Interlocked.Read(ref _skills);

    /// <summary>
    /// How many entered the first district, and the second, over the life of the world.
    /// </summary>
    public long EntriesToFirstDistrict => Interlocked.Read(ref _firstEntries);

    public long EntriesToSecondDistrict => Interlocked.Read(ref _secondEntries);

    public static FakeWorld Start(string prefix = "lt", string password = "secret")
    {
        var world = new FakeWorld(prefix, password);

        world.AuthPort = world.Listen(Role.Auth, null);
        world.GatePort = world.Listen(Role.Gate, null);
        world.DistrictPort = world.Listen(Role.District, first: true);
        world.SecondDistrictPort = world.Listen(Role.District, first: false);
        world.MazePort = world.Listen(Role.Maze, null);

        return world;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();

        foreach (var listener in _listeners)
        {
            listener.Stop();
        }

        try
        {
            await Task.WhenAll(_loops).WaitAsync(TimeSpan.FromSeconds(5));
        }

        catch (Exception e) when (e is TimeoutException or OperationCanceledException or IOException or SocketException)
        {
            // The connections are gone with the process.
        }

        _stop.Dispose();
    }

    private int Listen(Role role, bool? first)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start(1024);

        _listeners.Add(listener);

        var group = role == Role.District ? (first == true ? _firstDistrict : _secondDistrict) : null;

        lock (_loops)
        {
            _loops.Add(Task.Run(() => AcceptAsync(listener, role, group)));
        }

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task AcceptAsync(TcpListener listener, Role role, ConcurrentDictionary<Connection, byte>? group)
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(_stop.Token);

                client.NoDelay = true;

                var connection = new Connection(client, role, group);

                Interlocked.Increment(ref _connections);

                lock (_loops)
                {
                    _loops.Add(Task.Run(() => ServeAsync(connection)));
                }
            }
        }

        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // Stopping.
        }
    }

    private async Task ServeAsync(Connection connection)
    {
        var writer = Task.Run(connection.WriteLoopAsync);

        try
        {
            var stream = connection.Stream;
            var head = new byte[MessageHeader.HeaderSize];

            while (!_stop.IsCancellationRequested)
            {
                await stream.ReadExactlyAsync(head, _stop.Token);

                var header = new MessageHeader(head);
                var body = new byte[header.ContentLength];

                await stream.ReadExactlyAsync(body, _stop.Token);

                MessageEncryption.Exchange(body.AsSpan(), header.Version);

                Handle(connection, body);
            }
        }

        catch (Exception e) when (e is EndOfStreamException or IOException or OperationCanceledException or ObjectDisposedException or InvalidDataException)
        {
            // The client went away.
        }

        finally
        {
            connection.Group?.TryRemove(connection, out _);
            connection.Complete();

            await writer;
        }
    }

    private void Handle(Connection connection, byte[] body)
    {
        if (body.Length < sizeof(ushort))
        {
            return;
        }

        var opcode = BinaryPrimitives.ReadUInt16LittleEndian(body);

        if (_handlers[connection.Role].TryGetValue(opcode, out var handler))
        {
            using var reader = new BinaryReader(new MemoryStream(body) { Position = sizeof(ushort) });

            handler(connection, reader);
        }
    }

    private static ushort Op<T>() where T : struct, IHotspotMessage
    {
        return (ushort)default(T).Opcode.Value;
    }

    #region The auth server

    private void OnLogin(Connection connection, BinaryReader reader)
    {
        var request = new LoginAuthRequest(reader);

        if (!request.Username.StartsWith(Prefix, StringComparison.Ordinal) ||
            request.Password != Password ||
            !int.TryParse(request.Username[Prefix.Length..], out var number))
        {
            connection.Send(new LoginResponse(LoginErrorMessageCode.WrongUsernameOrPassword));

            return;
        }

        Interlocked.Increment(ref _logins);

        // Account 0 means no account to the servers, so the numbers start from one.
        var account = number + 1;
        var session = new SessionValue(account);

        _sessions[session.Key] = account;
        connection.Account = account;

        connection.Send(new LoginResponse(session, request.Username, true, request.MacAddress, LoginType.NewUser));
    }

    private void OnGateList(Connection connection, BinaryReader reader)
    {
        var request = new LoginGateListRequest(reader);

        if (request.Account != connection.Account)
        {
            return;
        }

        var gate = new GateInfo
        {
            Id = 1,
            Name = "Test",
            Workload = GateWorkload.Normal,
            Address = "127.0.0.1",
            Port = (short)GatePort,
            OnlineCount = 0
        };

        connection.Send(new LoginGateListResponse
        {
            Values = [new GateDataInfo { Gate = gate, Person = new GatePersonInfo { Count = (byte)(_characters.ContainsKey(request.Account) ? 1 : 0) } }]
        });

        connection.Send(new LoginOptionLoadResponse { PersonOptions = new byte[64], Contents = new ServerContent([]) });
    }

    private void OnGateConnect(Connection connection, BinaryReader reader)
    {
        _ = new LoginGateConnectRequest(reader);

        connection.Send(new LoginGateConnectResponse { Address = "127.0.0.1", Port = (short)GatePort });
    }

    #endregion The auth server

    #region The gate

    private void OnEnterGate(Connection connection, BinaryReader reader)
    {
        var request = new LoginEnterServerRequest(reader);

        if (!_sessions.TryGetValue(request.Session.Key, out var account) || account != request.Account)
        {
            connection.Send(LoginEnterGateResponse.Error);

            return;
        }

        connection.Account = account;
        connection.Session = request.Session.Key;

        connection.Send(new LoginEnterGateResponse { Account = account });
        connection.Send(new WorldCurrentDateClientMessage());
    }

    private void OnPersonList(Connection connection, BinaryReader reader)
    {
        _ = new PersonListRequest(reader);

        SendList(connection);
    }

    private void OnPersonCreate(Connection connection, BinaryReader reader)
    {
        var request = new PersonCreateRequest(reader);

        if (connection.Account == 0 || request.Person.InfoValue.Hero != Requests.Template.Hero)
        {
            connection.Send(new ErrorMessageResponse(MessageOpcode.From(GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes.GroupOpcode.Character, GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes.CharacterOpcode.ListRes), PersonErrorCode.CreateFailed));

            return;
        }

        Interlocked.Increment(ref _creations);

        _characters[connection.Account] = request.Person.InfoValue.Name;

        SendList(connection);
    }

    private void SendList(Connection connection)
    {
        var persons = _characters.TryGetValue(connection.Account, out var name)
            ? new[] { Person(ActorOf(connection.Account), name) }
            : [];

        connection.Send(new CharacterListResponse
        {
            Persons = persons,
            LastPerson = persons.Length == 0 ? 0 : ActorOf(connection.Account),
            Protection = new ProtectionStateValue()
        });
    }

    private void OnPersonSelect(Connection connection, BinaryReader reader)
    {
        var request = new PersonSelectRequest(reader);

        if (request.Actor.Identifier != ActorOf(connection.Account) || !_characters.ContainsKey(connection.Account))
        {
            connection.Send(new CharacterSelectResponse { Zone = EnterMapResultValue.Failed(EnterMapResultValue.CannotEnter) });

            return;
        }

        connection.Send(new CharacterSelectResponse
        {
            Zone = Place(connection, FirstLocation, DistrictPort, EnterMapType.Normal, ChangeServerType.EnterDistrict, new Vector3(10228.605f, 10058.951f, 90.452f))
        });
    }

    #endregion The gate

    #region The district

    private void OnDistrictEnter(Connection connection, BinaryReader reader)
    {
        var request = new PersonEnterGameServerRequest(reader);

        if (TurnAwayFromDistrict ||
            !_sessions.TryGetValue(request.Session.Key, out var account) ||
            account != request.Account ||
            request.Actor.Identifier != ActorOf(account))
        {
            connection.Complete();

            return;
        }

        connection.Account = account;
        connection.Actor = request.Actor.Identifier;
        connection.Session = request.Session.Key;
        connection.Group?.TryAdd(connection, 0);

        Interlocked.Increment(ref ReferenceEquals(connection.Group, _firstDistrict) ? ref _firstEntries : ref _secondEntries);

        connection.Send(new CharacterInfoResponse
        {
            Person = Person(connection.Actor, _characters[account]),
            World = new WorldValue(FirstLocation, new MapValue { Channel = 1, Location = FirstLocation, Server = 1 }, new Vector3(10228.605f, 10058.951f, 90.452f), 90f),
            Gate = new CharacterInfoGatePayload { AccountId = "test" }
        });

        connection.Send(default(SkillLoadInfoResponse) with
        {
            Snapshot = default(SkillSnapshotValue) with
            {
                Target = connection.Actor,
                SkillPoint = default,
                DeckSlotCount = 4,
                SkillList = [default(SkillInfoValue) with { Skill = LearnedSkill }],
                DeckSlotList = [],
                DeckBonus = new ushort[SkillModuleDefines.SkillDeckBonusCount]
            }
        });
    }

    private void OnPing(Connection connection, BinaryReader reader)
    {
        var request = new SystemPingRequest(reader);

        Interlocked.Increment(ref _pings);

        if (PongDelay > TimeSpan.Zero)
        {
            _ = Task.Delay(PongDelay).ContinueWith(_ => connection.Send(new SystemPingResponse { KeepAlive = request.TickCount }), TaskScheduler.Default);

            return;
        }

        connection.Send(new SystemPingResponse { KeepAlive = request.TickCount });
    }

    private void OnMove(Connection connection, BinaryReader reader)
    {
        var request = new MovementMoveRequest(reader);

        if (connection.Actor == 0 || request.dwActorID.Identifier != connection.Actor || connection.Group is null)
        {
            return;
        }

        Interlocked.Increment(ref _moves);

        var broadcast = new MovementMoveBtResponse
        {
            Actor = request.dwActorID,
            Map = request.nMapID,
            Pos = new Vector3(request.fPosX, request.fPosY, request.fPosZ),
            Yaw = request.fYaw,
            TargetPosX = request.fTargetPosX,
            TargetPosY = request.fTargetPosY,
            RunBit = request.byRunBit,
            Pitch = request.fPitch,
            MoveSpeed = request.fMoveSpeed,
            ChangeMotion = request.byChangeMotion
        };

        // Serialized once and put in every queue, which is what the real server will do after its second phase.
        var frame = Wire.ServerFrame(broadcast);

        foreach (var member in connection.Group.Keys)
        {
            member.Enqueue(frame);
        }
    }

    private void OnChat(Connection connection, BinaryReader reader)
    {
        var request = new ChatNormalRequest(reader);

        if (connection.Actor == 0 || connection.Group is null)
        {
            return;
        }

        Interlocked.Increment(ref _chats);

        var response = new ChatNormalResponse
        {
            Actor = new ActorComponent(connection.Actor, ActorType.User),
            Appearance = request.Appearance,
            Message = request.Message
        };

        var frame = Wire.ServerFrame(response);

        foreach (var member in connection.Group.Keys)
        {
            member.Enqueue(frame);
        }
    }

    private void OnChannelChange(Connection connection, BinaryReader reader)
    {
        var request = new ChannelChangeRequest(reader);

        Interlocked.Increment(ref _channelChanges);

        _channelsAskedFor.AddOrUpdate(request.Channel, 1, (_, count) => count + 1);
    }

    private void OnMazeCreate(Connection connection, BinaryReader reader)
    {
        var request = new MazeCreateRequest(reader);

        if (RefuseMaze)
        {
            connection.Send(new WorldEnterResponse { Map = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) });

            return;
        }

        connection.Send(new WorldEnterResponse
        {
            Map = Place(connection, request.Location, MazePort, EnterMapType.Maze, ChangeServerType.EnterMap, new Vector3(100f, 200f, 5f))
        });
    }

    private void OnWorldEnter(Connection connection, BinaryReader reader)
    {
        var request = new WorldEnterRequest(reader);

        if (request.Location != SecondLocation)
        {
            connection.Send(new WorldEnterResponse { Map = EnterMapResultValue.Failed(EnterMapResultValue.ServerUnavailable) });

            return;
        }

        connection.Send(new WorldEnterResponse
        {
            Map = Place(connection, SecondLocation, SecondDistrictPort, EnterMapType.EnterDistrict, ChangeServerType.EnterDistrict, new Vector3(20000f, 20000f, 50f))
        });
    }

    #endregion The district

    #region The maze

    private void OnMazeEnter(Connection connection, BinaryReader reader)
    {
        var request = new PersonEnterGameServerRequest(reader);

        if (!_sessions.TryGetValue(request.Session.Key, out var account) || account != request.Account)
        {
            connection.Complete();

            return;
        }

        connection.Account = account;
        connection.Actor = request.Actor.Identifier;

        // A client that comes from another server is answered with the place it is in, as the servers do.
        connection.Send(new PersonEnterGameServerResponse2
        {
            World = new WorldValue(21311, new MapValue { Location = 21311, Server = 1 }, new Vector3(100f, 200f, 5f), 0f)
        });
    }

    private void OnMazeMove(Connection connection, BinaryReader reader)
    {
        var request = new MovementMoveRequest(reader);

        if (connection.Actor == 0 || request.dwActorID.Identifier != connection.Actor)
        {
            return;
        }

        Interlocked.Increment(ref _moves);

        // A maze tells a move to its player alone.
        connection.Send(new MovementMoveBtResponse
        {
            Actor = request.dwActorID,
            Map = request.nMapID,
            Pos = new Vector3(request.fPosX, request.fPosY, request.fPosZ),
            Yaw = request.fYaw,
            TargetPosX = request.fTargetPosX,
            TargetPosY = request.fTargetPosY,
            RunBit = request.byRunBit,
            Pitch = request.fPitch,
            MoveSpeed = request.fMoveSpeed,
            ChangeMotion = request.byChangeMotion
        });

        // And the monsters move with the same broadcast, as the real maze sends it (MonsterBody). The maze numbers its
        // actors on its own, so a monster can have the number a district gave to a bot: the second one does.
        var other = _firstDistrict.Keys.FirstOrDefault(e => e.Actor != 0 && e.Actor != connection.Actor)?.Actor ?? MonsterActor;

        foreach (var monster in (int[])[MonsterActor, other])
        {
            connection.Send(new MovementMoveBtResponse
            {
                Actor = new ActorValue(monster, ActorType.Monster),
                Map = request.nMapID,
                Pos = new Vector3(request.fPosX + 300f, request.fPosY - 300f, request.fPosZ),
                MoveSpeed = request.fMoveSpeed
            });
        }
    }

    private void OnSkill(Connection connection, BinaryReader reader)
    {
        var request = new SkillActiveSkillRequest(reader);

        Interlocked.Increment(ref _skills);

        connection.Send(default(SkillActiveResponse) with { Divergence = request.Skill.Divergence });
    }

    #endregion The maze

    private static int ActorOf(int account) => 100_000 + account;

    private static PersonValue Person(int actor, string name)
    {
        var template = Requests.TemplatePerson(name);

        return new PersonValue(
            actor,
            template.InfoValue,
            template.Account,
            template.Level,
            template.Faction,
            template.StuffLevel,
            template.PvPKillCount,
            template.PrimaryWeapon,
            template.SecondaryWeapon,
            template.EquipItems,
            template.Title,
            template.League,
            template.Ability,
            template.PrivateShop,
            template.FatiguePoints,
            template.Rank,
            template.BattlePose,
            template.Status,
            template.StatusEffects);
    }

    private static EnterMapResultValue Place(Connection connection, short location, int port, EnterMapType type, ChangeServerType change, Vector3 position)
    {
        var map = new MapValue { Channel = 1, Location = location, Server = 1 };

        return new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = ActorOf(connection.Account),
                Account = connection.Account,
                Server = 1,
                Jump = location * 100 + 1,
                Map = map,
                Parent = new MapValue(),
                Address = "127.0.0.1",
                Port = (short)port,
                World = new WorldValue(location, map, position, 90f),
                Type = type
            },
            ChangeServer = true,
            ChangeType = change,
            Result = 0
        };
    }

    /// <summary>
    /// One client of the fake servers, with a queue and a task of its own to write it, like the session of the real ones.
    /// </summary>
    private sealed class Connection
    {
        private readonly TcpClient _client;
        private readonly System.Threading.Channels.Channel<byte[]> _queue = System.Threading.Channels.Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });

        public Connection(TcpClient client, Role role, ConcurrentDictionary<Connection, byte>? group)
        {
            _client = client;
            Role = role;
            Group = group;
        }

        public Role Role { get; }

        public ConcurrentDictionary<Connection, byte>? Group { get; }

        public NetworkStream Stream => _client.GetStream();

        public int Account { get; set; }

        public int Actor { get; set; }

        public long Session { get; set; }

        public void Send<T>(T message) where T : IResponseHotspotMessage
        {
            Enqueue(Wire.ServerFrame(message));
        }

        public void Enqueue(byte[] frame)
        {
            _queue.Writer.TryWrite(frame);
        }

        /// <summary>
        /// Closes the connection once what is queued has been sent.
        /// </summary>
        public void Complete()
        {
            _queue.Writer.TryComplete();
        }

        public async Task WriteLoopAsync()
        {
            try
            {
                await foreach (var frame in _queue.Reader.ReadAllAsync())
                {
                    await _client.GetStream().WriteAsync(frame);
                }
            }

            catch (Exception e) when (e is IOException or ObjectDisposedException or InvalidOperationException)
            {
                // The client went away.
            }

            _client.Close();
        }
    }
}
