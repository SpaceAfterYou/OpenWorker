using System.Globalization;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Responses;
using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

public enum PartyMatchingState
{
    /// <summary>
    /// The group takes those who come, until it is full or its time is up.
    /// </summary>
    Collecting = 0,

    /// <summary>
    /// Its people are asked whether they go.
    /// </summary>
    Checking = 1,

    /// <summary>
    /// Everybody goes; the leader is named and the party is about to be made.
    /// </summary>
    Waiting = 2
}

/// <param name="Answer">0 not yet, 1 goes, 2 does not.</param>
public sealed record PartyMatchingMember(PartyMemberCard Card, int Answer);

/// <param name="Deadline">When the state ends, in Unix milliseconds.</param>
/// <param name="Limit">How many the group must have to go, 0 for any number from two.</param>
public sealed record PartyMatchingGroup(int Id, short Maze, PartyMatchingState State, long Deadline, int Limit, int Leader, IReadOnlyList<PartyMatchingMember> Members)
{
    public IEnumerable<int> Persons => Members.Select(e => e.Card.Person);
}

/// <summary>
/// The auto matching: people who want into a maze and have no party wait in a group of that maze. A group that is full, or
/// whose time is up with enough people, asks them whether they go; when all do, it names its leader (the highest level) and
/// becomes a party a moment later. Groups live in Redis, so people of every server meet in them; the server that started a
/// group moves it on. A group whose server stops is forgotten after its lifetime.
/// </summary>
/// <remarks>
/// <c>ow:party:match:{id}</c> is a hash of the group (<c>maze</c>, <c>state</c>, <c>deadline</c>, <c>limit</c>, <c>leader</c>,
/// <c>p:{person}</c> the card, <c>l:{person}</c> the level, <c>c:{person}</c> the answer); <c>ow:party:match-maze:{maze}</c>
/// holds the groups of a maze; <c>ow:party:match-of:{person}</c> names the group of a person.
/// </remarks>
public sealed class PartyMatching(
    CommunityNetwork network,
    PartyNotifier parties,
    IConnectionMultiplexer redis,
    TimeProvider time,
    IEnumerable<IPartyMatchListener>? matched = null
) :
    IPartyClock,
    ICommunityListener
{
    private const string GroupPrefix = "ow:party:match:";
    private const string MazePrefix = "ow:party:match-maze:";
    private const string OfPrefix = "ow:party:match-of:";
    private const string Sequence = "ow:party:match-seq";

    /// <summary>
    /// A group nobody moves on is gone after this.
    /// </summary>
    private static TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(5);

    // KEYS: the pointer of the person, the groups of the maze, the sequence. ARGV: the person, the card, the level, the prefix
    // of groups, the most members, the level gap, the deadline of a new group, its limit, the lifetime in ms, the maze.
    // Returns 1 and the group when the person joined one, 2 when they started one, -1 when they wait in one already.
    private static RedisScript Enter { get; } = new("""
        if redis.call('EXISTS', KEYS[1]) == 1 then
            return {-1}
        end
        local level = tonumber(ARGV[3])
        local ttl = tonumber(ARGV[9])
        for _, id in ipairs(redis.call('SMEMBERS', KEYS[2])) do
            local key = ARGV[4] .. id
            if redis.call('HGET', key, 'state') == '0' then
                local sum, count = 0, 0
                for _, field in ipairs(redis.call('HKEYS', key)) do
                    if string.sub(field, 1, 2) == 'l:' then
                        sum = sum + tonumber(redis.call('HGET', key, field))
                        count = count + 1
                    end
                end
                if count > 0 and count < tonumber(ARGV[5]) and math.abs(level - sum / count) <= tonumber(ARGV[6]) then
                    redis.call('HSET', key, 'p:' .. ARGV[1], ARGV[2], 'l:' .. ARGV[1], ARGV[3])
                    redis.call('SET', KEYS[1], id, 'PX', ttl)
                    return {1, tonumber(id)}
                end
            else
                redis.call('SREM', KEYS[2], id)
            end
        end
        local id = tostring(redis.call('INCR', KEYS[3]))
        local key = ARGV[4] .. id
        redis.call('HSET', key, 'maze', ARGV[10], 'state', '0', 'deadline', ARGV[7], 'limit', ARGV[8], 'leader', '0',
            'p:' .. ARGV[1], ARGV[2], 'l:' .. ARGV[1], ARGV[3])
        redis.call('PEXPIRE', key, ttl)
        redis.call('SADD', KEYS[2], id)
        redis.call('SET', KEYS[1], id, 'PX', ttl)
        return {2, tonumber(id)}
        """);

    // KEYS: the pointer of the person, the group. ARGV: the person. Returns the state the group was in, -1 when the person
    // was in none.
    private static RedisScript Exit { get; } = new("""
        local id = redis.call('GET', KEYS[1])
        redis.call('DEL', KEYS[1])
        if not id or redis.call('HEXISTS', KEYS[2], 'p:' .. ARGV[1]) == 0 then
            return -1
        end
        redis.call('HDEL', KEYS[2], 'p:' .. ARGV[1], 'l:' .. ARGV[1], 'c:' .. ARGV[1])
        return tonumber(redis.call('HGET', KEYS[2], 'state'))
        """);

    // KEYS: the group. ARGV: the state it must be in, the next one, its deadline, the leader. False when it moved on.
    private static RedisScript Advance { get; } = new("""
        if redis.call('HGET', KEYS[1], 'state') ~= ARGV[1] then
            return 0
        end
        redis.call('HSET', KEYS[1], 'state', ARGV[2], 'deadline', ARGV[3], 'leader', ARGV[4])
        return 1
        """);

    private readonly HashSet<int> _owned = [];

    private IDatabase Database => redis.GetDatabase();

    private long Now => time.GetUtcNow().ToUnixTimeMilliseconds();

    /// <summary>
    /// The player waits for a party into the maze; the members of the group they are put in see who came. Returns the code
    /// of the message the client shows when they cannot, or zero.
    /// </summary>
    /// <param name="limit">How many the maze needs, see <see cref="PartyRules.MatchingLimit"/>.</param>
    public async Task<int> EnterAsync(Entity player, short maze, int limit)
    {
        var card = parties.CardOf(player);
        var deadline = Now + (long)PartyRules.MatchingCollect.TotalMilliseconds;

        var result = (int[])(await Enter.EvaluateAsync(
            redis,
            [OfKey(card.Person), MazePrefix + maze.ToString(CultureInfo.InvariantCulture), Sequence],
            [
                card.Person, card.Encode(), (int)card.Level, GroupPrefix, PartyRules.MaxMembers, PartyRules.MatchingLevelGap,
                deadline, limit, (long)Lifetime.TotalMilliseconds, (int)maze
            ]))!;

        if (result[0] < 0)
        {
            return PartyRules.Text.Unexpected;
        }

        if (result[0] == 2)
        {
            _owned.Add(result[1]);
        }

        if (await GetAsync(result[1]) is { } group)
        {
            await TellAsync(group);
        }

        return 0;
    }

    /// <summary>
    /// The person stops waiting. A group that was asking or naming its leader falls apart; one that was collecting goes on
    /// with the others.
    /// </summary>
    public async Task<bool> ExitAsync(int person, PartyMatchingExit reason)
    {
        var id = await Database.StringGetAsync(OfKey(person));

        if (!id.TryParse(out int group))
        {
            return false;
        }

        var state = (int)await Exit.EvaluateAsync(redis, [OfKey(person), GroupKey(group)], [person]);

        if (state < 0)
        {
            return false;
        }

        var rest = await GetAsync(group);

        await network.SendAsync(rest?.Persons.Append(person) ?? [person], new PartyMatchingExitResponse { Character = person, Reason = reason });

        if (rest is not null && (state != (int)PartyMatchingState.Collecting || rest.Members.Count == 0))
        {
            await DissolveAsync(rest, PartyMatchingExit.Check);
        }

        else if (rest is not null)
        {
            await TellAsync(rest);
        }

        return true;
    }

    /// <summary>
    /// The person says whether they go; the server that moves the group on counts the answers.
    /// </summary>
    public async Task CheckAsync(int person, bool goes)
    {
        var id = await Database.StringGetAsync(OfKey(person));

        if (id.TryParse(out int group))
        {
            await Database.HashSetAsync(GroupKey(group), "c:" + person.ToString(CultureInfo.InvariantCulture), goes ? 1 : 2, When.Exists);
        }
    }

    public async Task<PartyMatchingGroup?> OfAsync(int person)
    {
        var id = await Database.StringGetAsync(OfKey(person));

        return id.TryParse(out int group) ? await GetAsync(group) : null;
    }

    /// <summary>
    /// Moves on the groups this server started.
    /// </summary>
    public async Task TickAsync()
    {
        foreach (var id in _owned.ToArray())
        {
            if (await GetAsync(id) is not { } group)
            {
                _owned.Remove(id);
                continue;
            }

            await MoveOnAsync(group);
        }
    }

    private async Task MoveOnAsync(PartyMatchingGroup group)
    {
        var now = Now;
        var count = group.Members.Count;
        var enough = count >= PartyRules.MinMembers && (group.Limit == 0 || count >= group.Limit);

        switch (group.State)
        {
            // A full group needs nobody's answer: they all came for it.
            case PartyMatchingState.Collecting when count >= PartyRules.MaxMembers:
                await WaitAsync(group);
                break;

            case PartyMatchingState.Collecting when now >= group.Deadline && enough:
                if (await AdvanceAsync(group, PartyMatchingState.Checking, now + (long)PartyRules.MatchingCheck.TotalMilliseconds, 0))
                {
                    await network.SendAsync(group.Persons, new PartyMatchingCheckResponse());
                }

                break;

            case PartyMatchingState.Collecting when now >= group.Deadline:
                await DissolveAsync(group, PartyMatchingExit.UserCount);
                break;

            case PartyMatchingState.Checking when group.Members.Any(e => e.Answer == 2) || !enough:
                await DissolveAsync(group, PartyMatchingExit.Check);
                break;

            case PartyMatchingState.Checking when group.Members.All(e => e.Answer == 1):
                await WaitAsync(group);
                break;

            case PartyMatchingState.Checking when now >= group.Deadline:
                await DissolveAsync(group, PartyMatchingExit.Check);
                break;

            case PartyMatchingState.Waiting when now >= group.Deadline:
                await StartAsync(group);
                break;
        }
    }

    private async Task WaitAsync(PartyMatchingGroup group)
    {
        var leader = group.Members
            .OrderByDescending(e => e.Card.Level)
            .ThenBy(e => e.Card.Person)
            .First().Card.Person;

        if (await AdvanceAsync(group, PartyMatchingState.Waiting, Now + (long)PartyRules.MatchingStart.TotalMilliseconds, leader))
        {
            await network.SendAsync(group.Persons, new PartyMatchingWaitResponse { Member = new ActorValue(leader, ActorType.User) });
        }
    }

    /// <summary>
    /// The group becomes a party, its leader the master. One who found a party meanwhile breaks it up.
    /// </summary>
    private async Task StartAsync(PartyMatchingGroup group)
    {
        await ForgetAsync(group);

        var cards = group.Members
            .OrderBy(e => e.Card.Person != group.Leader)
            .Select(e => e.Card)
            .ToArray();

        var party = cards.Length < PartyRules.MinMembers ? 0 : await parties.CreateAsync(cards, PartyType.Auto);

        if (party == 0)
        {
            await network.SendAsync(group.Persons, new PartyMatchingExitResponse { Character = 0, Reason = PartyMatchingExit.Check });

            return;
        }

        // The party goes into the maze it was made for.
        foreach (var listener in matched ?? [])
        {
            await listener.OnMatchedAsync(party, group.Maze, cards.Select(e => e.Person).ToArray());
        }
    }

    private async Task DissolveAsync(PartyMatchingGroup group, PartyMatchingExit reason)
    {
        await ForgetAsync(group);

        // Nobody in particular left: the client takes zero as the whole group.
        await network.SendAsync(group.Persons, new PartyMatchingExitResponse { Character = 0, Reason = reason });
    }

    private async Task ForgetAsync(PartyMatchingGroup group)
    {
        _owned.Remove(group.Id);

        foreach (var person in group.Persons)
        {
            var key = OfKey(person);

            if (await Database.StringGetAsync(key) == group.Id)
            {
                await Database.KeyDeleteAsync(key);
            }
        }

        await Database.KeyDeleteAsync(GroupKey(group.Id));
        await Database.SetRemoveAsync(MazePrefix + group.Maze.ToString(CultureInfo.InvariantCulture), group.Id);
    }

    private async Task<bool> AdvanceAsync(PartyMatchingGroup group, PartyMatchingState next, long deadline, int leader)
    {
        return (int)await Advance.EvaluateAsync(redis, [GroupKey(group.Id)], [(int)group.State, (int)next, deadline, leader]) == 1;
    }

    /// <summary>
    /// Everybody in the group sees who is in it and how long it still collects.
    /// </summary>
    private async Task TellAsync(PartyMatchingGroup group)
    {
        var online = await network.FindManyAsync(group.Persons.ToArray());

        await network.SendAsync(group.Persons, new PartyMatchingEnterResponse
        {
            Identifier = group.Id,
            MemberList = group.Members.Select((e, i) => parties.Info(e.Card, online[i])).Take(PartyRules.MaxMembers).ToArray(),
            RemainTick = (int)Math.Max(0, (group.Deadline - Now) / 1000)
        });
    }

    public async Task<PartyMatchingGroup?> GetAsync(int id)
    {
        var fields = await Database.HashGetAllAsync(GroupKey(id));

        if (fields.Length == 0)
        {
            return null;
        }

        var values = new Dictionary<string, RedisValue>();
        var cards = new Dictionary<int, PartyMemberCard>();
        var answers = new Dictionary<int, int>();

        foreach (var field in fields)
        {
            var name = (string)field.Name!;

            if (name.StartsWith("p:", StringComparison.Ordinal) && int.TryParse(name[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var person))
            {
                cards[person] = PartyMemberCard.Decode(person, field.Value);
            }

            else if (name.StartsWith("c:", StringComparison.Ordinal) && int.TryParse(name[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var answerer))
            {
                answers[answerer] = (int)field.Value;
            }

            else
            {
                values[name] = field.Value;
            }
        }

        return new PartyMatchingGroup(
            id,
            (short)(int)values.GetValueOrDefault("maze"),
            (PartyMatchingState)(int)values.GetValueOrDefault("state"),
            (long)values.GetValueOrDefault("deadline"),
            (int)values.GetValueOrDefault("limit"),
            (int)values.GetValueOrDefault("leader"),
            cards.Values.OrderBy(e => e.Person).Select(e => new PartyMatchingMember(e, answers.GetValueOrDefault(e.Person))).ToArray());
    }

    public ValueTask OnEnteredAsync(Entity player, PresenceRecord person) => ValueTask.CompletedTask;

    /// <summary>
    /// Who goes offline stops waiting.
    /// </summary>
    public async ValueTask OnLeftAsync(Entity player, PresenceRecord person)
    {
        await ExitAsync(person.Person, PartyMatchingExit.Logout);
    }

    private static RedisKey GroupKey(int id) => GroupPrefix + id.ToString(CultureInfo.InvariantCulture);

    private static RedisKey OfKey(int person) => OfPrefix + person.ToString(CultureInfo.InvariantCulture);
}
