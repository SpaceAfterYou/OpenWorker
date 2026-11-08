using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Numerics;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Net;
using OpenWorker.LoadTest.Protocol;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// One player. It signs in, gets to a character, enters a district, and then walks in a circle and talks until the
/// run ends; a bot that is chosen to fight leaves the district for a maze after a few seconds and uses a skill
/// there. The script runs as one task that sleeps between the actions, so thousands of bots need no thread each.
/// What the server sends is read by the read loop of the connection, which calls <see cref="IFrameSink.OnFrame" />.
/// </summary>
internal sealed class Bot : IFrameSink, IDisposable
{
    /// <summary>
    /// The number a ping carries while the bot waits to be let into a district. Pings after that carry their count.
    /// </summary>
    private const long EntryPing = -1;

    /// <summary>
    /// How many pings, lines and skills the bot remembers the time of. More than a few can hardly be in flight.
    /// </summary>
    private const int Ring = 64;

    private const int ActorMask = 0x1FFFFFFF;

    private const long NoKeepAlive = long.MinValue;

    /// <summary>
    /// A latency longer than this is not a latency but a mismatch: the number of a move came round after a lap
    /// of the circle, or the readings of two clocks were taken apart.
    /// </summary>
    private const long ImplausibleMicroseconds = 120_000_000;

    private readonly RunContext _context;
    private readonly RunOptions _options;
    private readonly RunStats _stats;
    private readonly Random _random;
    private readonly int _index;
    private readonly string _username;
    private readonly string _macAddress;

    // The moves, pings, lines and skills the bot sent, by the number they carry, with the time they went out.
    private readonly long[] _moveSentAt = new long[MoveTrack.Points];
    private readonly long[] _pingSentAt = new long[Ring];
    private readonly long[] _chatSentAt = new long[Ring];
    private readonly long[] _skillSentAt = new long[Ring];

    private long _moveCount;
    private long _pingCount;
    private long _chatCount;
    private long _skillSent;
    private long _skillAnswered;

    // Who the bot is and where.
    private int _account;
    private long _session;
    private short _gate;
    private Redirect _gateAddress = new(string.Empty, 0);
    private int _actor;
    private ulong _map;
    private int _location;
    private float _centerX;
    private float _centerY;
    private float _height;
    private float _heading;
    private int[] _skills = [];

    // Set once, before the first move leaves, and read by the read loops of the bots that hear it.
    private volatile bool _trackReady;
    private volatile bool _inMaze;

    // Whether the bot has left the district it was in for a step that takes it somewhere else.
    private bool _leftDistrict;

    private BotConnection? _connection;
    private CancellationTokenSource _lost = new();
    private Pending? _pending;
    private Exception? _closeError;
    private long _keepAliveTicks = NoKeepAlive;

    public Bot(int index, RunContext context)
    {
        _context = context;
        _options = context.Options;
        _stats = context.Stats;
        _random = new Random(unchecked((index * 7919) + 17));
        _index = index;

        var number = _options.AccountOffset + index;

        _username = AccountNames.Format(_options.AccountPrefix, number);
        _macAddress = AccountNames.MacAddress(number);

        IsMazeBot = RunContext.GoesToMaze(index, _options.MazeShare);
        Metrics = new BotMetrics();

        _stats.Register(Metrics);

        // Counted here, not when the script starts on a thread of the pool, so the fleet never sees a bot it has
        // made as not started.
        _stats.BotStarted();
    }

    public BotMetrics Metrics { get; }

    public bool IsMazeBot { get; }

    public void Dispose()
    {
        _lost.Dispose();
    }

    /// <summary>
    /// Runs the script until the run ends, the server drops the bot, or a step fails. Never throws for a failure
    /// of the bot: it is counted, and the other bots go on.
    /// </summary>
    public async Task RunAsync(CancellationToken stopping)
    {
        var entered = false;
        var lost = false;

        try
        {
            await LoginAsync(stopping);
            await ChooseGateAsync(stopping);

            var zone = await EnterGateAsync(stopping);

            await EnterDistrictAsync(zone, stopping);

            entered = true;
            _stats.BotEntered();

            await PlayAsync(stopping);
        }

        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // The run is over.
        }

        catch (BotFailure failure)
        {
            // A step counted a failure of its own. A bot that was in the world has lost its place in it.
            lost = entered;

            if (entered && !failure.Counted)
            {
                _stats.Error("session", failure.Kind, failure.Message);
            }
        }

        catch (Exception e)
        {
            lost = entered;

            _stats.Error("bot", "unexpected", e.ToString());
        }

        finally
        {
            await DisposeConnectionAsync();

            var actor = Volatile.Read(ref _actor);

            if (actor != 0)
            {
                _context.Unregister(actor, this);
            }

            if (entered)
            {
                if (_inMaze)
                {
                    _stats.BotInMaze(-1);
                }

                _stats.BotLeft(lost);
            }

            else
            {
                _stats.BotFailedToEnter();
            }

            Dispose();
        }
    }

    #region Getting in

    private Task LoginAsync(CancellationToken stopping)
    {
        return StageAsync(BotStage.Login, async () =>
        {
            await ConnectAsync(BotStage.Login, _options.AuthHost, _options.AuthPort, stopping);

            var reply = await CallAsync(BotStage.Login, Opcodes.LoginResult, (ref w) => Requests.Login(ref w, _username, _options.Password, _macAddress), stopping);
            var result = Responses.ReadLogin(reply.Body);

            if (result.ErrorCode != 0)
            {
                var name = (LoginErrorMessageCode)result.ErrorCode;

                throw new BotFailure(BotStage.Login, "refused-" + name, $"The server refused the login of {_username}: {name} ({result.ErrorCode}).");
            }

            _account = result.Account;
            _session = result.Session;
        }, stopping);
    }

    private Task ChooseGateAsync(CancellationToken stopping)
    {
        return StageAsync(BotStage.GateList, async () =>
        {
            var reply = await CallAsync(BotStage.GateList, Opcodes.GateList, (ref w) => Requests.GateList(ref w, _account), stopping);
            var gates = Responses.ReadGateList(reply.Body);

            var gate = gates.FirstOrDefault(e => _options.Gate == 0 ? e.Workload != 0 : e.Id == _options.Gate);

            if (gate is null)
            {
                var wanted = _options.Gate == 0 ? "online" : "number " + _options.Gate.ToString(CultureInfo.InvariantCulture);

                throw new BotFailure(BotStage.GateList, "no-gate", $"The server has {gates.Length} gates and none of them is {wanted}.");
            }

            reply = await CallAsync(BotStage.GateList, Opcodes.GateConnect, (ref w) => Requests.GateConnect(ref w, gate.Id), stopping);

            var redirect = Responses.ReadGateConnect(reply.Body);

            if (redirect.Port <= 0)
            {
                throw new BotFailure(BotStage.GateList, "no-address", $"The server gave no address for the gate {gate.Id}.");
            }

            _gate = gate.Id;
            _gateAddress = redirect;

            // A client leaves the auth server for the gate.
            await DisposeConnectionAsync();
        }, stopping);
    }

    private async Task<EnterMapResult> EnterGateAsync(CancellationToken stopping)
    {
        await StageAsync(BotStage.GateEnter, async () =>
        {
            await ConnectAsync(BotStage.GateEnter, _gateAddress.Address, _gateAddress.Port, stopping);

            var reply = await CallAsync(BotStage.GateEnter, Opcodes.EnterGate, (ref w) => Requests.EnterGate(ref w, _account, _gate, _session), stopping);

            if (Responses.ReadEnterGate(reply.Body) is null)
            {
                throw new BotFailure(BotStage.GateEnter, "refused", $"The gate {_gate} did not let {_username} in.");
            }
        }, stopping);

        return await StageAsync(BotStage.Character, async () =>
        {
            var reply = await CallAsync(BotStage.Character, Opcodes.PersonList, (ref w) => Requests.PersonList(ref w, _session), stopping);
            var (persons, last) = Responses.ReadPersonList(reply.Body);

            if (persons.Length == 0)
            {
                // The answer to a creation is the list again, with the new character in it.
                reply = await CallAsync(BotStage.Character, Opcodes.PersonList, (ref w) => Requests.PersonCreate(ref w, _username), stopping);
                (persons, last) = Responses.ReadPersonList(reply.Body);
            }

            if (persons.Length == 0)
            {
                throw new BotFailure(BotStage.Character, "no-character", $"The account {_username} has no character after the creation.");
            }

            var person = persons.FirstOrDefault(e => e.Actor == last) ?? persons[0];

            reply = await CallAsync(BotStage.Character, Opcodes.PersonSelect, (ref w) => Requests.PersonSelect(ref w, person.Actor, _account, true), stopping);

            var zone = Responses.ReadEnterMap(reply.Body);

            if (zone.Result != 0 || !zone.ChangeServer || zone.Port <= 0)
            {
                throw new BotFailure(BotStage.Character, "refused-" + zone.Result.ToString(CultureInfo.InvariantCulture), $"The gate did not let the character {person.Name} into a district (result {zone.Result}).");
            }

            // The gate has nothing more to say to a bot that is on its way to the district.
            await DisposeConnectionAsync();

            return zone;
        }, stopping);
    }

    /// <summary>
    /// Enters the district the gate chose. The entry is done when the server has handled the whole request of it:
    /// the ping that follows the request on the same connection is answered only then, because the server handles
    /// the packets of a connection one after another.
    /// </summary>
    private Task EnterDistrictAsync(EnterMapResult zone, CancellationToken stopping)
    {
        return StageAsync(BotStage.DistrictEnter, async () =>
        {
            await EnterAsync(BotStage.DistrictEnter, zone, true, stopping);

            var channel = ChannelToAskFor();

            if (channel > 0)
            {
                await SendAsync(_connection!, (ref w) => Requests.ChannelChange(ref w, channel));
            }
        }, stopping);
    }

    /// <summary>
    /// The channel to ask for after entering a district, or 0 to stay. With <c>--channel-fill</c> the first channel
    /// is the one the district puts a player into, so the bots meant for it do not ask.
    /// </summary>
    private short ChannelToAskFor()
    {
        if (_options.Channel > 0)
        {
            return (short)_options.Channel;
        }

        if (_options.ChannelFill <= 0)
        {
            return 0;
        }

        var channel = 1 + (_index / _options.ChannelFill);

        return channel > 1 ? (short)Math.Min(channel, short.MaxValue) : (short)0;
    }

    /// <summary>
    /// Connects to the district of a zone and enters it, the way the client does: the request to enter, the ask for
    /// who is around, and a ping behind them that tells when the server is through with both.
    /// </summary>
    private async Task EnterAsync(BotStage stage, EnterMapResult zone, bool firstConnect, CancellationToken stopping)
    {
        TakePlace(zone);

        await ConnectAsync(stage, zone.Address, zone.Port, stopping);

        await CallAsync(
            stage,
            Opcodes.Ping,
            (ref w) =>
            {
                Requests.PersonEnter(ref w, _account, _actor, _map, firstConnect, _session);
                Requests.WorldObject(ref w);
                Requests.Ping(ref w, EntryPing);
            },
            stopping);
    }

    /// <summary>
    /// Remembers where the character is and registers the bot under its actor, so the others can find it when they
    /// hear it move.
    /// </summary>
    private void TakePlace(EnterMapResult zone)
    {
        // The offset keeps the circles of two bots from lying on each other.
        _centerX = zone.Position.X + (float)((_random.NextDouble() * 20d) - 10d);
        _centerY = zone.Position.Y + (float)((_random.NextDouble() * 20d) - 10d);
        _height = zone.Position.Z;
        _heading = zone.Rotation;
        _map = zone.Map;
        _location = zone.Location;

        Volatile.Write(ref _actor, zone.Actor & ActorMask);

        _context.Register(_actor, this);

        _trackReady = true;
    }

    #endregion Getting in

    #region Playing

    private async Task PlayAsync(CancellationToken stopping)
    {
        if (_options.District > 0 && _options.District != _location)
        {
            await TravelAsync(stopping);
        }

        if (IsMazeBot)
        {
            // The bot plays in the district for a while, then asks for the maze.
            var delay = _options.MazeDelay + (_options.MazeDelay * _random.NextDouble());

            await LoopAsync(Stopwatch.GetTimestamp() + (long)(delay.TotalSeconds * Stopwatch.Frequency), stopping);

            await TryEnterMazeAsync(stopping);
        }

        await LoopAsync(long.MaxValue, stopping);
    }

    /// <summary>
    /// Goes to the district the run asked for: asks to enter it, follows the server to its address and enters it
    /// there. A refusal leaves the bot where it was.
    /// </summary>
    private async Task TravelAsync(CancellationToken stopping)
    {
        try
        {
            await StageAsync(BotStage.DistrictTravel, async () =>
            {
                var reply = await CallAsync(BotStage.DistrictTravel, Opcodes.WorldEnter, (ref w) => Requests.WorldEnter(ref w, (short)_options.District, 0, 0), stopping);
                var zone = Responses.ReadEnterMap(reply.Body);

                if (zone.Result != 0 || !zone.ChangeServer || zone.Port <= 0 || zone.Type != (byte)EnterMapType.EnterDistrict)
                {
                    throw new BotFailure(BotStage.DistrictTravel, "refused-" + zone.Result.ToString(CultureInfo.InvariantCulture), $"The district {_options.District} did not take the bot (result {zone.Result}).");
                }

                _leftDistrict = true;

                await DisposeConnectionAsync();

                await EnterAsync(BotStage.DistrictTravel, zone, false, stopping);
            }, stopping);
        }

        catch (BotFailure) when (!_leftDistrict)
        {
            // The bot is still in the district it entered, and plays there.
        }

        finally
        {
            _leftDistrict = false;
        }
    }

    /// <summary>
    /// Asks the district for a maze and goes there. A refusal leaves the bot in the district; a failure after the
    /// bot has left it ends the bot.
    /// </summary>
    private async Task TryEnterMazeAsync(CancellationToken stopping)
    {
        try
        {
            await StageAsync(BotStage.MazeEnter, async () =>
            {
                var reply = await CallAsync(BotStage.MazeEnter, Opcodes.WorldEnter, (ref w) => Requests.MazeCreate(ref w, _options.MazeLocation, 0), stopping);
                var zone = Responses.ReadEnterMap(reply.Body);

                if (zone.Result != 0 || !zone.ChangeServer || zone.Port <= 0 || zone.Type != (byte)EnterMapType.Maze)
                {
                    throw new BotFailure(BotStage.MazeEnter, "refused-" + zone.Result.ToString(CultureInfo.InvariantCulture), $"The maze {_options.MazeLocation} did not take the bot (result {zone.Result}).");
                }

                _leftDistrict = true;

                await DisposeConnectionAsync();

                _inMaze = true;
                _stats.BotInMaze(1);

                // The place in the maze is where the server put the character, and the circle goes round it.
                _centerX = zone.Position.X;
                _centerY = zone.Position.Y;
                _height = zone.Position.Z;
                _heading = zone.Rotation;
                _map = zone.Map;

                await ConnectAsync(BotStage.MazeEnter, zone.Address, zone.Port, stopping);

                // A maze does not answer pings, so the entry is done when the character is described to the client.
                await CallAsync(BotStage.MazeEnter, Opcodes.PersonInfo, (ref w) => Requests.PersonEnter(ref w, _account, _actor, _map, false, _session), stopping);

                await SendAsync(_connection!, (ref w) => Requests.MazeComplete(ref w));
            }, stopping);
        }

        catch (BotFailure) when (!_leftDistrict)
        {
            // The maze refused the bot, which plays on in the district.
        }

        finally
        {
            _leftDistrict = false;
        }
    }

    /// <summary>
    /// The script of the steady state: moves, lines, skills and the answers a server asks for, each at its own
    /// rate. The bot sleeps until the next action is due. Returns at <paramref name="until" /> (a reading of
    /// <see cref="Stopwatch" />) or throws when the run ends or the connection does.
    /// </summary>
    private async Task LoopAsync(long until, CancellationToken stopping)
    {
        var connection = _connection!;

        using var alive = CancellationTokenSource.CreateLinkedTokenSource(stopping, _lost.Token);

        var cancel = alive.Token;
        var frequency = Stopwatch.Frequency;

        var moveEvery = _options.MoveRate > 0 ? (long)(frequency / _options.MoveRate) : 0;
        var chatEvery = !_inMaze && _options.ChatRate > 0 ? (long)(frequency / _options.ChatRate) : 0;
        var skillEvery = _inMaze && _options.SkillRate > 0 ? (long)(frequency / _options.SkillRate) : 0;

        var now = Stopwatch.GetTimestamp();

        // Every bot starts at a different point of its period, so the moves of a thousand bots do not arrive together.
        var nextMove = moveEvery > 0 ? now + (long)(_random.NextDouble() * moveEvery) : long.MaxValue;
        var nextChat = chatEvery > 0 ? now + (long)(_random.NextDouble() * chatEvery) : long.MaxValue;
        var nextSkill = skillEvery > 0 ? now + (long)(_random.NextDouble() * skillEvery) : long.MaxValue;

        // A server that asks for a keep-alive is answered at the next wake-up at the latest.
        var check = frequency / 4;

        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var due = Math.Min(Math.Min(nextMove, nextChat), Math.Min(nextSkill, now + check));
                var wait = Math.Min(due, until) - Stopwatch.GetTimestamp();

                if (wait > 0)
                {
                    await Task.Delay(Stopwatch.GetElapsedTime(0, wait), cancel);
                }

                now = Stopwatch.GetTimestamp();

                if (now >= until)
                {
                    return;
                }

                var keepAlive = Interlocked.Exchange(ref _keepAliveTicks, NoKeepAlive);

                if (keepAlive != NoKeepAlive)
                {
                    await SendAsync(connection, (ref w) => Requests.KeepAlive(ref w, keepAlive));
                }

                if (now >= nextMove)
                {
                    await SendMoveAsync(connection, nextMove);

                    nextMove = Advance(nextMove, moveEvery, now);
                }

                if (now >= nextChat)
                {
                    await SendChatAsync(connection);

                    nextChat = Advance(nextChat, chatEvery, now);
                }

                if (now >= nextSkill)
                {
                    await SendSkillAsync(connection);

                    nextSkill = Advance(nextSkill, skillEvery, now);
                }
            }
        }

        catch (Exception e) when (e is SocketException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            // The end of the run is not a failure.
            stopping.ThrowIfCancellationRequested();

            throw Lost(e);
        }

        stopping.ThrowIfCancellationRequested();

        // Neither the end of the run nor the end of the time asked for, so the connection ended.
        throw Lost(null);
    }

    private BotFailure Lost(Exception? thrown)
    {
        var stage = _inMaze ? BotStage.MazeEnter : BotStage.DistrictEnter;
        var error = Volatile.Read(ref _closeError) ?? thrown;

        return error is null || (thrown is OperationCanceledException && Volatile.Read(ref _closeError) is null)
            ? new BotFailure(stage, "closed-by-server", "The server closed the connection.")
            : new BotFailure(stage, "disconnected", "The connection broke: " + error.Message, error);
    }

    /// <summary>
    /// The time the next action is due after the one that was due at <paramref name="due" />. A bot that has
    /// fallen more than a period behind skips what it missed instead of sending it all at once.
    /// </summary>
    internal static long Advance(long due, long every, long now)
    {
        if (every == 0)
        {
            return long.MaxValue;
        }

        var next = due + every;

        return next < now - every ? now + every : next;
    }

    private async ValueTask SendMoveAsync(BotConnection connection, long dueAt)
    {
        var count = _moveCount++;
        var index = (int)(count % MoveTrack.Points);

        var (x, y) = MoveTrack.PointAt(_centerX, _centerY, index);
        var (targetX, targetY) = MoveTrack.PointAt(_centerX, _centerY, index + 1);

        // A ping that follows the move measures how long the server takes to get through it. A maze does not answer one.
        var probe = !_inMaze && _options.ProbeEvery > 0 && count % _options.ProbeEvery == 0;
        var pingNumber = _pingCount;

        var buffer = ArrayPool<byte>.Shared.Rent(Requests.MaxFrameSize * 2);

        try
        {
            var length = BuildMove(buffer, x, y, targetX, targetY, MoveTrack.HeadingAt(index), probe, pingNumber);

            var sentAt = Stopwatch.GetTimestamp();

            if (_stats.Measuring)
            {
                Metrics.SendLag.RecordTicks(dueAt, sentAt);
                Metrics.MovesSentWhileMeasured++;
            }

            Volatile.Write(ref _moveSentAt[index], sentAt);

            if (probe)
            {
                Volatile.Write(ref _pingSentAt[pingNumber % Ring], sentAt);

                _pingCount++;
                Metrics.PingsSent++;
            }

            Metrics.MovesSent++;

            await connection.SendAsync(buffer.AsMemory(0, length));
        }

        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private int BuildMove(byte[] buffer, float x, float y, float targetX, float targetY, float heading, bool probe, long pingNumber)
    {
        var writer = new FrameWriter(buffer);

        Requests.Move(ref writer, _actor, _map, new Vector3(x, y, _height), heading, targetX, targetY, true, 0f, 560f, 0);

        if (probe)
        {
            Requests.Ping(ref writer, pingNumber);
        }

        return writer.Length;
    }

    private async ValueTask SendChatAsync(BotConnection connection)
    {
        var number = _chatCount++;

        Volatile.Write(ref _chatSentAt[number % Ring], Stopwatch.GetTimestamp());

        Metrics.ChatsSent++;

        await SendAsync(connection, (ref w) => Requests.Chat(ref w, number.ToString(CultureInfo.InvariantCulture)));
    }

    private async ValueTask SendSkillAsync(BotConnection connection)
    {
        var skill = _options.Skill != 0 ? _options.Skill : _skills.FirstOrDefault();

        if (skill == 0)
        {
            return;
        }

        var sent = _skillSent;

        Volatile.Write(ref _skillSentAt[sent % Ring], Stopwatch.GetTimestamp());
        Volatile.Write(ref _skillSent, sent + 1);

        Metrics.SkillsSent++;

        var position = new Vector3(_centerX, _centerY, _height);

        await SendAsync(connection, (ref w) => Requests.SkillActive(ref w, skill, position, _heading));
    }

    #endregion Playing

    #region Reading

    void IFrameSink.OnFrame(ReadOnlySpan<byte> frame, long receivedAt)
    {
        Metrics.FramesReceived++;

        if (frame.Length < sizeof(ushort))
        {
            return;
        }

        var opcode = BinaryPrimitives.ReadUInt16LittleEndian(frame);

        // The broadcast of a move is by far what a bot hears most.
        if (opcode == Opcodes.MoveBroadcast)
        {
            OnMove(frame, receivedAt);

            return;
        }

        var pending = Volatile.Read(ref _pending);

        if (pending is not null && pending.Accepts(opcode))
        {
            pending.Source.TrySetResult(new Reply(opcode, frame.ToArray()));

            return;
        }

        if (opcode == Opcodes.Ping)
        {
            OnPong(frame, receivedAt);
        }

        else if (opcode == Opcodes.ChatNormal)
        {
            OnChat(frame, receivedAt);
        }

        else if (opcode == Opcodes.SkillActive)
        {
            OnSkill(receivedAt);
        }

        else if (opcode == Opcodes.KeepAlive)
        {
            Volatile.Write(ref _keepAliveTicks, Responses.ReadKeepAliveTicks(frame));
        }

        else if (opcode == Opcodes.SkillLoadInfo)
        {
            _skills = Responses.ReadLearnedSkills(frame);
        }

        else if (opcode == Opcodes.PersonKickOut)
        {
            // Told outside any call: the server is about to close the connection, and the close is what ends the bot.
            var kick = Responses.ReadKickOut(frame);

            _stats.Error("session", "kicked-" + (PersonKickOutReason)kick.Reason, kick.Message);
        }
    }

    void IFrameSink.OnClosed(Exception? error)
    {
        Volatile.Write(ref _closeError, error);

        Volatile.Read(ref _pending)?.Source.TrySetException(new ConnectionLost(error));

        try
        {
            _lost.Cancel();
        }

        catch (ObjectDisposedException)
        {
            // The bot ended first.
        }
    }

    private void OnMove(ReadOnlySpan<byte> frame, long receivedAt)
    {
        if (!_stats.Measuring)
        {
            return;
        }

        if (frame.Length < Responses.MoveBroadcastSize)
        {
            Metrics.Unmatched++;

            return;
        }

        var actor = Responses.ReadMoveActor(frame) & ActorMask;
        var own = actor == Volatile.Read(ref _actor);
        var sender = own ? this : _context.Find(actor);

        // The monsters of a maze move with the same broadcast, and the bot does not measure them.
        if (sender is null && _inMaze)
        {
            return;
        }

        if (sender is null || !sender._trackReady)
        {
            Metrics.Unmatched++;

            return;
        }

        var (x, y) = Responses.ReadMovePosition(frame);
        var index = MoveTrack.IndexOf(sender._centerX, sender._centerY, x, y);

        if (index < 0)
        {
            Metrics.Unmatched++;

            return;
        }

        var sentAt = Volatile.Read(ref sender._moveSentAt[index]);
        var microseconds = sentAt == 0 ? -1 : LatencyHistogram.ToMicroseconds(sentAt, receivedAt);

        if (microseconds is < 0 or > ImplausibleMicroseconds)
        {
            Metrics.Unmatched++;

            return;
        }

        if (!own)
        {
            Metrics.MoveDelivery.Record(microseconds);
        }

        else if (_inMaze)
        {
            Metrics.MazeMoveEcho.Record(microseconds);
        }

        else
        {
            Metrics.MoveEcho.Record(microseconds);
        }
    }

    private void OnPong(ReadOnlySpan<byte> frame, long receivedAt)
    {
        var number = Responses.ReadPingToken(frame);

        if (number < 0 || !_stats.Measuring)
        {
            return;
        }

        var sentAt = Volatile.Read(ref _pingSentAt[number % Ring]);

        if (sentAt == 0)
        {
            Metrics.Unmatched++;

            return;
        }

        Metrics.Ping.RecordTicks(sentAt, receivedAt);
    }

    private void OnChat(ReadOnlySpan<byte> frame, long receivedAt)
    {
        if (!_stats.Measuring || !Responses.TryReadChat(frame, out var speaker, out var number))
        {
            return;
        }

        var actor = speaker & ActorMask;
        var own = actor == Volatile.Read(ref _actor);
        var sender = own ? this : _context.Find(actor);

        if (sender is null)
        {
            return;
        }

        var sentAt = Volatile.Read(ref sender._chatSentAt[number % Ring]);

        if (sentAt == 0)
        {
            Metrics.Unmatched++;

            return;
        }

        (own ? Metrics.ChatEcho : Metrics.ChatDelivery).RecordTicks(sentAt, receivedAt);
    }

    private void OnSkill(long receivedAt)
    {
        // The answers come in the order the skills went out.
        var answered = _skillAnswered;

        if (answered >= Volatile.Read(ref _skillSent))
        {
            return;
        }

        _skillAnswered = answered + 1;
        Metrics.SkillsAnswered++;

        if (_stats.Measuring)
        {
            Metrics.SkillResponse.RecordTicks(Volatile.Read(ref _skillSentAt[answered % Ring]), receivedAt);
        }
    }

    #endregion Reading

    #region Plumbing

    /// <summary>
    /// A step the bot waits on the answer of the server for. It takes the answers the request can get: the one
    /// asked for, the error form of it, and the notice that the server throws the bot out.
    /// </summary>
    private sealed class Pending(ushort expected)
    {
        public TaskCompletionSource<Reply> Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Accepts(ushort opcode)
        {
            return opcode == expected || opcode == Opcodes.ErrorOf(expected) || opcode == Opcodes.PersonKickOut;
        }
    }

    private readonly record struct Reply(ushort Opcode, byte[] Body);

    /// <summary>
    /// The connection ended while the bot waited for an answer.
    /// </summary>
    private sealed class ConnectionLost(Exception? error) : Exception("The connection ended before the answer came.", error);

    private async Task ConnectAsync(BotStage stage, string address, int port, CancellationToken stopping)
    {
        var host = _options.RedirectHost ?? address;

        // A fresh set of flags for the new connection. The last one has ended: every step that
        // opens a connection first waits for the read loop of the old one to finish.
        Volatile.Write(ref _closeError, null);

        _lost.Dispose();
        _lost = new CancellationTokenSource();

        try
        {
            _connection = await BotConnection.ConnectAsync(host, port, this, _options.StageTimeout, stopping);
        }

        catch (Exception e) when (e is SocketException or TimeoutException)
        {
            throw BotFailure.Connecting(stage, host, port, e);
        }
    }

    private async Task DisposeConnectionAsync()
    {
        var connection = Interlocked.Exchange(ref _connection, null);

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private async Task<Reply> CallAsync(BotStage stage, ushort expected, Encoder request, CancellationToken stopping)
    {
        var pending = new Pending(expected);

        if (Interlocked.CompareExchange(ref _pending, pending, null) is not null)
        {
            throw new InvalidOperationException("A request is already waiting for its answer.");
        }

        try
        {
            await SendAsync(_connection!, request);

            var reply = await pending.Source.Task.WaitAsync(_options.StageTimeout, stopping);

            if (reply.Opcode == Opcodes.PersonKickOut)
            {
                var kick = Responses.ReadKickOut(reply.Body);
                var reason = (PersonKickOutReason)kick.Reason;

                throw new BotFailure(stage, "kicked-" + reason, $"The server threw the bot out: {reason}. {kick.Message}".TrimEnd());
            }

            if (reply.Opcode != expected)
            {
                var code = Responses.ReadErrorCode(reply.Body);

                throw new BotFailure(stage, "error-" + ErrorName(code), $"The server answered with the error {ErrorName(code)} ({code.ToString(CultureInfo.InvariantCulture)}).");
            }

            return reply;
        }

        catch (TimeoutException)
        {
            throw new BotFailure(stage, "timeout", $"No answer within {Durations.Format(_options.StageTimeout)}.");
        }

        finally
        {
            Volatile.Write(ref _pending, null);
        }
    }

    private static string ErrorName(ushort code)
    {
        return Enum.IsDefined((PersonErrorCode)code)
            ? ((PersonErrorCode)code).ToString()
            : code.ToString(CultureInfo.InvariantCulture);
    }

    private static async ValueTask SendAsync(BotConnection connection, Encoder request)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Requests.MaxFrameSize * 2);

        try
        {
            var length = Build(buffer, request);

            await connection.SendAsync(buffer.AsMemory(0, length));
        }

        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int Build(byte[] buffer, Encoder request)
    {
        var writer = new FrameWriter(buffer);

        request(ref writer);

        return writer.Length;
    }

    /// <summary>
    /// Runs a step of the script and counts it. A failure is counted under the step and thrown on as a
    /// <see cref="BotFailure" />; the end of the run is thrown on as it is.
    /// </summary>
    private async Task StageAsync(BotStage stage, Func<Task> work, CancellationToken stopping)
    {
        await StageAsync(
            stage,
            async () =>
            {
                await work();

                return 0;
            },
            stopping);
    }

    private async Task<T> StageAsync<T>(BotStage stage, Func<Task<T>> work, CancellationToken stopping)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            var result = await work();

            _stats.Stage(stage).Succeed(started);

            return result;
        }

        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            throw;
        }

        catch (Exception e)
        {
            var failure = e as BotFailure ?? Classify(stage, e);

            failure.Counted = true;

            _stats.Stage(stage).Fail();
            _stats.Error(BotStages.NameOf(stage), failure.Kind, failure.Message);

            if (ReferenceEquals(failure, e))
            {
                throw;
            }

            throw failure;
        }
    }

    private static BotFailure Classify(BotStage stage, Exception e)
    {
        return e switch
        {
            ConnectionLost lost => new BotFailure(stage, "closed-by-server", "The server closed the connection" + (lost.InnerException is null ? "." : ": " + lost.InnerException.Message), e),
            OperationCanceledException => new BotFailure(stage, "closed-by-server", "The connection was closed during the step.", e),
            SocketException socket => new BotFailure(stage, "io-" + socket.SocketErrorCode, "The connection broke: " + socket.Message, e),
            IOException => new BotFailure(stage, "io-error", "The connection broke: " + e.Message, e),
            InvalidDataException => new BotFailure(stage, "protocol", "The server sent what the bot cannot read: " + e.Message, e),
            _ => new BotFailure(stage, "unexpected", e.ToString(), e)
        };
    }

    #endregion Plumbing
}
