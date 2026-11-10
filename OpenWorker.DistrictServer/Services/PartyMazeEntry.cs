using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using Redis.OM.Searching;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// Where a party maze is kept and how a member is sent into it: the record of the party in Redis and the reservation of
/// the maze server, which <see cref="PartyMazeEntry"/> leaves to this so that its rules can be tried without them.
/// </summary>
public interface IPartyMazeRoutes
{
    bool IsMaze(short location);

    /// <summary>
    /// The maze the party plays now, if its maze server still runs.
    /// </summary>
    ValueTask<PartyMazeCache?> FindAsync(int party);

    /// <summary>
    /// Picks the maze server and a new instance for the party, and records them.
    /// </summary>
    ValueTask<PartyMazeCache> OpenAsync(int party, short location, int members);

    /// <summary>
    /// Writes the reservation of the member for the instance and answers the client with the address. False when the
    /// location has no start to enter by.
    /// </summary>
    ValueTask<bool> SendAsync(Entity player, short location, int jump, PartyMazeCache maze);
}

public sealed class PartyMazeRoutes(
    WorldManager manager,
    IRedisCollection<PartyMazeCache> parties,
    IRedisCollection<MazeCache> servers
) : IPartyMazeRoutes
{
    public bool IsMaze(short location) => manager.IsMaze(location);

    public async ValueTask<PartyMazeCache?> FindAsync(int party)
    {
        if (await parties.FindByIdAsync(party.ToString(System.Globalization.CultureInfo.InvariantCulture)) is not { } record)
        {
            return null;
        }

        // A maze server that stopped without saying so has no record any more, and neither has its maze.
        return record.Server != Guid.Empty && await servers.FindByIdAsync(record.Server.ToString()) is not null ? record : null;
    }

    public async ValueTask<PartyMazeCache> OpenAsync(int party, short location, int members)
    {
        var (server, host, port) = await manager.PickMazeServerAsync();

        var record = new PartyMazeCache
        {
            Party = party,
            Location = location,
            Instance = Guid.NewGuid(),
            Server = server,
            Host = host,
            Port = port,
            Members = (byte)Math.Clamp(members, 0, byte.MaxValue)
        };

        await parties.InsertAsync(record, PartyMazeCache.Lifetime);

        return record;
    }

    public ValueTask<bool> SendAsync(Entity player, short location, int jump, PartyMazeCache maze)
    {
        return manager.SendToMazeAsync(player, location, jump, (maze.Host, maze.Port), maze.Instance, maze.Party, maze.Members);
    }
}

/// <summary>
/// A party enters a maze together. The master takes the portal; every member is asked (12/47) while the master waits; when
/// all have said yes, one instance is opened on one maze server for the party and every member is sent into it. A member
/// who says no, the master who calls it off, or a member who leaves the district ends the question for all (11/49). A
/// member who takes the portal of the maze the party already plays is sent into it where it stands, from any district and
/// channel, without a question. A player who is not in a party, or alone in one, enters as before.
/// </summary>
public sealed partial class PartyMazeEntry(
    World world,
    IPartyRoster roster,
    CommunityNetwork community,
    IPartyMazeRoutes routes,
    ILogger<PartyMazeEntry> logger,
    MazeAdmission? admission = null,
    TimeProvider? time = null
) : ISessionCloseHandler, IPartyMatchListener, ICommunityModule
{
    /// <summary>
    /// How long a question stays open. The client of a member counts a time down in its question and answers when it is over,
    /// so a question that is still open after this much is one the client never answered; the original server holds it until
    /// somebody ends it.
    /// </summary>
    public static TimeSpan QuestionLifetime { get; } = TimeSpan.FromSeconds(45);

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <summary>
    /// The event that has the servers of the members of a party the auto matching made send them into its maze.
    /// </summary>
    private const string MatchedEvent = "party.maze.matched";

    /// <summary>
    /// Only the master of a party opens a maze for it.
    /// </summary>
    public const ushort NotMaster = 55004;

    /// <summary>
    /// A member is not in the game.
    /// </summary>
    public const ushort MemberOffline = 55040;

    /// <summary>
    /// A member is in the game, but not in this district.
    /// </summary>
    public const ushort MemberAway = 55041;

    /// <summary>
    /// A member is in this district, but on another channel.
    /// </summary>
    public const ushort MemberOnOtherChannel = 55047;

    /// <summary>
    /// The maze cannot be entered.
    /// </summary>
    public const ushort CannotEnter = MazeAdmission.CannotEnter;

    /// <summary>
    /// A question that is open: who was asked, and who has not said yes yet.
    /// </summary>
    private sealed class Question(int party, int master, short location, int jump, MessageOpcode request, long asked)
    {
        public long Asked { get; } = asked;
        public int Party { get; } = party;
        public int Master { get; } = master;
        public short Location { get; } = location;
        public int Jump { get; } = jump;
        public MessageOpcode Request { get; } = request;
        public List<(int Person, Entity Player)> Members { get; } = [];
        public HashSet<int> Waiting { get; } = [];
    }

    private readonly Dictionary<int, Question> _byPerson = [];

    /// <summary>
    /// Whether a question about entering a maze waits for the answer of the person.
    /// </summary>
    public bool IsAsked(int person) => _byPerson.ContainsKey(person);

    /// <summary>
    /// The player takes a way into a location. True when the party takes care of it: the player is in a party of more than
    /// one and the location is a maze. False otherwise, and the caller enters as for a player alone.
    /// </summary>
    public async ValueTask<bool> TryEnterAsync(Entity player, short location, int jump, MessageOpcode request, int portal = 0)
    {
        if (!routes.IsMaze(location) || !world.Has<ActorComponent>(player))
        {
            return false;
        }

        var person = world.Get<ActorComponent>(player).Identifier;
        var party = await roster.PartyAsync(person);

        if (party is null || party.Members.Count < 2)
        {
            return false;
        }

        if (!world.IsAlive(player))
        {
            return true;
        }

        // Whoever takes the portal, the master or a member, passes what a maze asks of everybody before the party is looked at.
        if (admission?.CheckEntrant(player, location, inParty: true) is { } unfit)
        {
            Refuse(player, request, unfit);

            return true;
        }

        // The party plays this maze already: the member goes where it stands.
        var running = await routes.FindAsync(party.Id);

        if (!world.IsAlive(player))
        {
            return true;
        }

        if (running is not null && running.Location == location)
        {
            if (!await routes.SendAsync(player, location, jump, running) && world.IsAlive(player))
            {
                Refuse(player, request, CannotEnter);
            }

            return true;
        }

        if (party.Master != person)
        {
            Refuse(player, request, NotMaster);

            return true;
        }

        var question = new Question(party.Id, person, location, jump, request, _time.GetTimestamp());

        foreach (var member in party.Members)
        {
            if (member == person)
            {
                question.Members.Add((member, player));
                continue;
            }

            if (community.TryGet(member, out var other))
            {
                if (!SameChannel(player, other))
                {
                    Refuse(player, request, MemberOnOtherChannel);

                    return true;
                }

                question.Members.Add((member, other));
                question.Waiting.Add(member);

                continue;
            }

            var presence = await community.FindAsync(member);

            if (world.IsAlive(player))
            {
                Refuse(player, request, presence is null ? MemberOffline : MemberAway);
            }

            return true;
        }

        // The maze must take a party, and every member must be able to enter it, before anybody is asked.
        if (admission?.CheckParty(player, question.Members.Select(e => e.Player).ToArray(), location, portal, out var item) is { } refused)
        {
            Refuse(player, request, refused, item);

            return true;
        }

        // A new question of the master replaces the one that was open.
        if (_byPerson.TryGetValue(person, out var open))
        {
            Forget(open);
        }

        foreach (var (member, _) in question.Members)
        {
            if (_byPerson.TryGetValue(member, out var other))
            {
                Cancel(other, member);
            }

            _byPerson[member] = question;
        }

        LogAsked(party.Id, location, question.Members.Count);

        var ask = new MazeEnterPartyAskResponse { Maze = location, Accepted = false };

        foreach (var (_, member) in question.Members)
        {
            Send(member, ask);
        }

        return true;
    }

    /// <summary>
    /// A member answers the question, or the master calls it off. The last yes sends the party in.
    /// </summary>
    public async ValueTask AnswerAsync(Entity player, MazeEnterPartyAnswerRequest answer)
    {
        if (!world.Has<ActorComponent>(player))
        {
            return;
        }

        var person = world.Get<ActorComponent>(player).Identifier;

        // An answer to no question, or to the question of another maze, changes nothing.
        if (!_byPerson.TryGetValue(person, out var question) || answer.Maze != question.Location)
        {
            return;
        }

        if (person == question.Master || !answer.Agree)
        {
            Cancel(question, person);

            return;
        }

        question.Waiting.Remove(person);

        if (question.Waiting.Count > 0)
        {
            return;
        }

        Forget(question);

        await LaunchAsync(question);
    }

    /// <summary>
    /// Everybody said yes: one instance for the party, and every member who is still here goes into it.
    /// </summary>
    private async ValueTask LaunchAsync(Question question)
    {
        var maze = await routes.OpenAsync(question.Party, question.Location, question.Members.Count);

        LogLaunched(question.Party, question.Location, maze.Instance);

        foreach (var (_, member) in question.Members)
        {
            if (world.IsAlive(member) && !await routes.SendAsync(member, question.Location, question.Jump, maze) && world.IsAlive(member))
            {
                Refuse(member, question.Request, CannotEnter);
            }
        }
    }

    /// <summary>
    /// The auto matching made a party for the maze, and everybody in it said they go: one instance for it, and every member
    /// is sent in by the server they are on.
    /// </summary>
    public async ValueTask OnMatchedAsync(int party, short maze, IReadOnlyList<int> persons)
    {
        if (!routes.IsMaze(maze))
        {
            return;
        }

        var record = await routes.OpenAsync(party, maze, persons.Count);

        LogLaunched(party, maze, record.Instance);

        await community.PublishAsync(persons, MatchedEvent, BitConverter.GetBytes(party));
    }

    public void Attach(CommunityNetwork network)
    {
        network.On(MatchedEvent, OnMatchedHereAsync);
    }

    /// <summary>
    /// The members of a matched party who are on this server go into its maze.
    /// </summary>
    private async ValueTask OnMatchedHereAsync(CommunityEvent matched)
    {
        if (matched.Body.Length < sizeof(int) || await routes.FindAsync(BitConverter.ToInt32(matched.Body.Span)) is not { } record)
        {
            return;
        }

        foreach (var person in matched.Persons)
        {
            if (community.TryGet(person, out var player) && !await routes.SendAsync(player, record.Location, 0, record) && world.IsAlive(player))
            {
                LogMatchedNotSent(person, record.Location);
            }
        }
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (world.Has<ActorComponent>(player) &&
            world.Get<ActorComponent>(player).Identifier is var person &&
            _byPerson.TryGetValue(person, out var question))
        {
            Cancel(question, person);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Ends the questions that have been open for too long. The member who has not answered is the one the others are told
    /// ended it, as if they had said no.
    /// </summary>
    public void ExpireQuestions()
    {
        foreach (var question in _byPerson.Values.Distinct().ToArray())
        {
            if (_time.GetElapsedTime(question.Asked) < QuestionLifetime)
            {
                continue;
            }

            var silent = question.Members.Select(e => e.Person).FirstOrDefault(question.Waiting.Contains);

            Cancel(question, silent != 0 ? silent : question.Master);
        }
    }

    /// <summary>
    /// The question is over for everybody: they are told who ended it.
    /// </summary>
    private void Cancel(Question question, int actor)
    {
        Forget(question);

        var cancel = new MazeCancelEnterPartyResponse { Actor = actor };

        foreach (var (_, member) in question.Members)
        {
            Send(member, cancel);
        }
    }

    private void Forget(Question question)
    {
        foreach (var (member, _) in question.Members)
        {
            if (_byPerson.TryGetValue(member, out var current) && ReferenceEquals(current, question))
            {
                _byPerson.Remove(member);
            }
        }
    }

    private bool SameChannel(Entity player, Entity other)
    {
        return world.TryGet(player, out ChannelMemberComponent mine) &&
               world.TryGet(other, out ChannelMemberComponent theirs) &&
               mine.Index == theirs.Index;
    }

    private void Send<T>(Entity player, T message) where T : struct, Hotspot.Messages.Abstractions.IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    private void Refuse(Entity player, MessageOpcode request, ushort code, int item = 0)
    {
        Send(player, new ErrorMessageResponse(request, code, item > 0 ? (uint)item : null));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Party {Party} is asked to enter maze {Maze}, {Members} members")]
    private partial void LogAsked(int party, short maze, int members);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Person {Person} of a matched party could not be sent into maze {Maze}")]
    private partial void LogMatchedNotSent(int person, short maze);

    [LoggerMessage(Level = LogLevel.Information, Message = "Party {Party} enters maze {Maze} as instance {Instance}")]
    private partial void LogLaunched(int party, short maze, Guid instance);
}
