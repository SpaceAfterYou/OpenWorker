using System.Globalization;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Party.Enums;
using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What a party remembers of a member: enough to show them while they are not online.
/// </summary>
public sealed record PartyMemberCard(int Person, string Name, byte Hero, byte Level)
{
    private const char Separator = '\n';

    public string Encode() => string.Join(
        Separator,
        Name,
        Hero.ToString(CultureInfo.InvariantCulture),
        Level.ToString(CultureInfo.InvariantCulture));

    public static PartyMemberCard Decode(int person, string? value)
    {
        var parts = value?.Split(Separator, 3);

        if (parts is not { Length: 3 } ||
            !byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hero) ||
            !byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
        {
            return new PartyMemberCard(person, string.Empty, 0, 0);
        }

        return new PartyMemberCard(person, parts[0], hero, level);
    }
}

/// <param name="OfflineSince">When the member was last seen leaving, in Unix seconds; null while they are online.</param>
public sealed record PartyMember(PartyMemberCard Card, long? OfflineSince)
{
    public int Person => Card.Person;
}

/// <summary>
/// A party as the book keeps it. The members are in the order of their numbers.
/// </summary>
public sealed record PartyRecord(int Id, int Master, PartyType Type, IReadOnlyList<PartyMember> Members)
{
    public IEnumerable<int> Persons => Members.Select(e => e.Person);

    public bool Has(int person) => Members.Any(e => e.Person == person);

    public bool IsFull => Members.Count >= PartyRules.MaxMembers;

    public PartyView View => new(Id, Master, Persons.ToArray());
}

/// <summary>
/// An invitation to a party that waits for an answer.
/// </summary>
public sealed record PartyInvite(int Inviter, string InviterName);

public enum PartyJoinOutcome
{
    Created,
    Joined,

    /// <summary>
    /// The one who would join is in a party already.
    /// </summary>
    InParty,

    /// <summary>
    /// The master is not the master of their party any more.
    /// </summary>
    NotMaster,

    Full
}

/// <param name="Left">False when the person was in no party.</param>
/// <param name="Before">The party as it was before they left.</param>
/// <param name="Master">The master after they left, the same one unless the master left.</param>
/// <param name="Deleted">The party ended: too few were left.</param>
public sealed record PartyLeave(bool Left, PartyRecord? Before, int Master, bool Deleted);

/// <summary>
/// The parties of every server, kept in Redis, and the invitations that wait for an answer. A party belongs to no server: a
/// change is one script, so two servers that change the same party at once do not lose each other's work. Every key has a
/// lifetime the servers of its online members renew, so the parties of servers that crashed go by themselves.
/// </summary>
/// <remarks>
/// <c>ow:party:{id}</c> is a hash: <c>master</c>, <c>type</c>, one <c>m:{person}</c> per member with what the party shows of
/// them, and <c>off:{person}</c> since when a member is not online. <c>ow:party:of:{person}</c> names the party of a person.
/// </remarks>
public sealed class PartyBook(IConnectionMultiplexer redis, TimeProvider time) : IPartyRoster
{
    private const string PartyPrefix = "ow:party:";
    private const string OfPrefix = "ow:party:of:";
    private const string InvitePrefix = "ow:party:invite:";
    private const string Sequence = "ow:party:seq";

    // The party of a person, if both the pointer and the party are still there; a stale pointer goes.
    private const string PartyOf = """
        local function party_of(key, prefix)
            local id = redis.call('GET', key)
            if id and redis.call('EXISTS', prefix .. id) == 1 then
                return id
            end
            if id then
                redis.call('DEL', key)
            end
            return false
        end
        local function members(key)
            local list = {}
            for _, field in ipairs(redis.call('HKEYS', key)) do
                if string.sub(field, 1, 2) == 'm:' then
                    table.insert(list, tonumber(string.sub(field, 3)))
                end
            end
            return list
        end
        """;

    // KEYS: the pointer of the member, of the master, the sequence. ARGV: master, member, card of the master, card of the
    // member, the most members, the lifetime in ms, the type, a number kept for the party (0 for a new one), the prefix.
    private static RedisScript Join { get; } = new(PartyOf + "\n" + """
        if party_of(KEYS[1], ARGV[9]) then
            return {-1}
        end
        local ttl = tonumber(ARGV[6])
        local id = party_of(KEYS[2], ARGV[9])
        if not id then
            id = ARGV[8]
            if id == '0' then
                id = tostring(redis.call('INCR', KEYS[3]))
            end
            local key = ARGV[9] .. id
            redis.call('HSET', key, 'master', ARGV[1], 'type', ARGV[7], 'm:' .. ARGV[1], ARGV[3], 'm:' .. ARGV[2], ARGV[4])
            redis.call('PEXPIRE', key, ttl)
            redis.call('SET', KEYS[2], id, 'PX', ttl)
            redis.call('SET', KEYS[1], id, 'PX', ttl)
            return {1, tonumber(id)}
        end
        local key = ARGV[9] .. id
        if redis.call('HGET', key, 'master') ~= ARGV[1] then
            return {-2}
        end
        if #members(key) >= tonumber(ARGV[5]) then
            return {-3}
        end
        redis.call('HSET', key, 'm:' .. ARGV[2], ARGV[4])
        redis.call('SET', KEYS[1], id, 'PX', ttl)
        return {2, tonumber(id)}
        """);

    // KEYS: the sequence, then the pointers of the members, the master first. ARGV: the prefix, the lifetime in ms, the
    // type, the master, then the person and the card of each member: those of KEYS[i] are ARGV[2i + 1] and ARGV[2i + 2].
    private static RedisScript Create { get; } = new(PartyOf + "\n" + """
        for i = 2, #KEYS do
            if party_of(KEYS[i], ARGV[1]) then
                return {-1, tonumber(ARGV[2 * i + 1])}
            end
        end
        local id = tostring(redis.call('INCR', KEYS[1]))
        local key = ARGV[1] .. id
        redis.call('HSET', key, 'master', ARGV[4], 'type', ARGV[3])
        for i = 2, #KEYS do
            redis.call('HSET', key, 'm:' .. ARGV[2 * i + 1], ARGV[2 * i + 2])
            redis.call('SET', KEYS[i], id, 'PX', tonumber(ARGV[2]))
        end
        redis.call('PEXPIRE', key, tonumber(ARGV[2]))
        return {1, tonumber(id)}
        """);

    // KEYS: the pointer of the person. ARGV: the person, the prefix of parties, the prefix of pointers. Returns the outcome
    // (0 in no party, 1 left, 2 the party ended), the party, the master after, then the members before.
    private static RedisScript Leave { get; } = new(PartyOf + "\n" + """
        local id = party_of(KEYS[1], ARGV[2])
        if not id then
            return {0}
        end
        local key = ARGV[2] .. id
        local person = tonumber(ARGV[1])
        if redis.call('HEXISTS', key, 'm:' .. ARGV[1]) == 0 then
            redis.call('DEL', KEYS[1])
            return {0}
        end
        local list = members(key)
        local master = tonumber(redis.call('HGET', key, 'master'))
        redis.call('DEL', KEYS[1])
        if #list <= 2 then
            redis.call('DEL', key)
            for _, member in ipairs(list) do
                if redis.call('GET', ARGV[3] .. member) == id then
                    redis.call('DEL', ARGV[3] .. member)
                end
            end
            return {2, tonumber(id), master, unpack(list)}
        end
        redis.call('HDEL', key, 'm:' .. ARGV[1], 'off:' .. ARGV[1])
        if master == person then
            master = nil
            for _, member in ipairs(list) do
                if member ~= person and (master == nil or member < master) then
                    master = member
                end
            end
            redis.call('HSET', key, 'master', master)
        end
        return {1, tonumber(id), master, unpack(list)}
        """);

    // KEYS: the party. ARGV: the master now, the next one.
    private static RedisScript Hand { get; } = new("""
        if redis.call('HGET', KEYS[1], 'master') ~= ARGV[1] then
            return -1
        end
        if redis.call('HEXISTS', KEYS[1], 'm:' .. ARGV[2]) == 0 then
            return -2
        end
        redis.call('HSET', KEYS[1], 'master', ARGV[2])
        return 1
        """);

    // KEYS: the party. ARGV: the person, the field to set or 'online', its value. Only a member is marked.
    private static RedisScript Mark { get; } = new("""
        if redis.call('HEXISTS', KEYS[1], 'm:' .. ARGV[1]) == 0 then
            return 0
        end
        if ARGV[2] == 'online' then
            redis.call('HDEL', KEYS[1], 'off:' .. ARGV[1])
            redis.call('HSET', KEYS[1], 'm:' .. ARGV[1], ARGV[3])
        else
            redis.call('HSETNX', KEYS[1], 'off:' .. ARGV[1], ARGV[3])
        end
        return 1
        """);

    private IDatabase Database => redis.GetDatabase();

    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    #region Roster

    public async ValueTask<IReadOnlyList<int>> MembersAsync(int person)
    {
        return (await OfAsync(person))?.Persons.ToArray() ?? [];
    }

    public async ValueTask<PartyView?> PartyAsync(int person)
    {
        return (await OfAsync(person))?.View;
    }

    #endregion

    /// <summary>
    /// The party of the person, or null when they are alone.
    /// </summary>
    public async Task<PartyRecord?> OfAsync(int person)
    {
        var id = await Database.StringGetAsync(OfKey(person));

        if (!id.TryParse(out int party))
        {
            return null;
        }

        var record = await GetAsync(party);

        return record is not null && record.Has(person) ? record : null;
    }

    public async Task<PartyRecord?> GetAsync(int party)
    {
        var fields = await Database.HashGetAllAsync(PartyKey(party));

        if (fields.Length == 0)
        {
            return null;
        }

        var master = 0;
        var type = PartyType.Base;
        var cards = new Dictionary<int, PartyMemberCard>();
        var offline = new Dictionary<int, long>();

        foreach (var field in fields)
        {
            var name = (string)field.Name!;

            if (name == "master")
            {
                field.Value.TryParse(out master);
            }

            else if (name == "type" && field.Value.TryParse(out int value))
            {
                type = (PartyType)value;
            }

            else if (name.StartsWith("m:", StringComparison.Ordinal) && TryNumber(name[2..], out var person))
            {
                cards[person] = PartyMemberCard.Decode(person, field.Value);
            }

            else if (name.StartsWith("off:", StringComparison.Ordinal) && TryNumber(name[4..], out var away) && field.Value.TryParse(out long since))
            {
                offline[away] = since;
            }
        }

        var members = cards.Values
            .OrderBy(e => e.Person)
            .Select(e => new PartyMember(e, offline.TryGetValue(e.Person, out var since) ? since : null))
            .ToArray();

        return members.Length == 0 ? null : new PartyRecord(party, master, type, members);
    }

    /// <summary>
    /// Puts the member into the party of the master, or makes one of the two when the master has none. A party made for a
    /// recruitment keeps the number the recruitment was shown under.
    /// </summary>
    public async Task<(PartyJoinOutcome Outcome, int Party)> JoinAsync(PartyMemberCard master, PartyMemberCard member, int keep = 0)
    {
        var result = (int[])(await Join.EvaluateAsync(
            redis,
            [OfKey(member.Person), OfKey(master.Person), Sequence],
            [
                master.Person, member.Person, master.Encode(), member.Encode(), PartyRules.MaxMembers,
                (long)PartyRules.PartyLifetime.TotalMilliseconds, (int)PartyType.Base, keep, PartyPrefix
            ]))!;

        return result[0] switch
        {
            1 => (PartyJoinOutcome.Created, result[1]),
            2 => (PartyJoinOutcome.Joined, result[1]),
            -1 => (PartyJoinOutcome.InParty, 0),
            -2 => (PartyJoinOutcome.NotMaster, 0),
            _ => (PartyJoinOutcome.Full, 0)
        };
    }

    /// <summary>
    /// Makes a party of people who are all alone, the first of them its master. Returns 0 and nothing changes when one of
    /// them is in a party already.
    /// </summary>
    public async Task<int> CreateAsync(IReadOnlyList<PartyMemberCard> members, PartyType type)
    {
        var keys = new List<RedisKey> { Sequence };
        var values = new List<RedisValue> { PartyPrefix, (long)PartyRules.PartyLifetime.TotalMilliseconds, (int)type, members[0].Person };

        foreach (var member in members)
        {
            keys.Add(OfKey(member.Person));
            values.Add(member.Person);
            values.Add(member.Encode());
        }

        var result = (int[])(await Create.EvaluateAsync(redis, keys.ToArray(), values.ToArray()))!;

        return result[0] == 1 ? result[1] : 0;
    }

    public async Task<PartyLeave> LeaveAsync(int person)
    {
        var result = (int[])(await Leave.EvaluateAsync(redis, [OfKey(person)], [person, PartyPrefix, OfPrefix]))!;

        if (result[0] == 0)
        {
            return new PartyLeave(false, null, 0, false);
        }

        // What the party was is read back from the answer: the cards are gone with a deleted party.
        var members = result[3..].Order().Select(e => new PartyMember(new PartyMemberCard(e, string.Empty, 0, 0), null)).ToArray();
        var before = new PartyRecord(result[1], result[2], PartyType.Base, members);

        return new PartyLeave(true, before, result[2], result[0] == 2);
    }

    /// <summary>
    /// Makes the other member the master. False when the one who asks is not the master, or the other not a member.
    /// </summary>
    public async Task<bool> HandOverAsync(int party, int master, int next)
    {
        return (int)await Hand.EvaluateAsync(redis, [PartyKey(party)], [master, next]) == 1;
    }

    /// <summary>
    /// The member is online again, as the card says.
    /// </summary>
    public async Task MarkOnlineAsync(int party, PartyMemberCard card)
    {
        await Mark.EvaluateAsync(redis, [PartyKey(party)], [card.Person, "online", card.Encode()]);
    }

    /// <summary>
    /// The member is not online since now, unless that was noticed earlier.
    /// </summary>
    public async Task MarkOfflineAsync(int party, int person)
    {
        await Mark.EvaluateAsync(redis, [PartyKey(party)], [person, "offline", Now]);
    }

    /// <summary>
    /// Keeps the parties of the persons, and their pointers, from expiring.
    /// </summary>
    public async Task RenewAsync(IReadOnlyCollection<int> persons)
    {
        if (persons.Count == 0)
        {
            return;
        }

        var ids = await Database.StringGetAsync(persons.Select(OfKey).ToArray());
        var renewals = new List<Task>();

        foreach (var (person, id) in persons.Zip(ids))
        {
            if (id.TryParse(out int party))
            {
                renewals.Add(Database.KeyExpireAsync(OfKey(person), PartyRules.PartyLifetime));
                renewals.Add(Database.KeyExpireAsync(PartyKey(party), PartyRules.PartyLifetime));
            }
        }

        await Task.WhenAll(renewals);
    }

    /// <summary>
    /// A number no party has, for a party that does not exist yet.
    /// </summary>
    public async Task<int> ReserveAsync()
    {
        return (int)await Database.StringIncrementAsync(Sequence);
    }

    #region Invitations

    /// <summary>
    /// Remembers the invitation of the person for a minute. Returns the one that waits already instead, if there is one.
    /// </summary>
    public async Task<PartyInvite?> InviteAsync(int person, PartyInvite invite)
    {
        var value = string.Join('\n', invite.Inviter.ToString(CultureInfo.InvariantCulture), invite.InviterName);

        if (await Database.StringSetAsync(InviteKey(person), value, PartyRules.InviteLifetime, When.NotExists))
        {
            return null;
        }

        return DecodeInvite(await Database.StringGetAsync(InviteKey(person))) ?? invite;
    }

    /// <summary>
    /// Takes the invitation of the person away and returns it.
    /// </summary>
    public async Task<PartyInvite?> TakeInviteAsync(int person)
    {
        return DecodeInvite(await Database.StringGetDeleteAsync(InviteKey(person)));
    }

    private static PartyInvite? DecodeInvite(RedisValue value)
    {
        var parts = ((string?)value)?.Split('\n', 2);

        return parts is { Length: 2 } && TryNumber(parts[0], out var inviter) ? new PartyInvite(inviter, parts[1]) : null;
    }

    #endregion

    private static bool TryNumber(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

    private static RedisKey PartyKey(int party) => PartyPrefix + party.ToString(CultureInfo.InvariantCulture);

    private static RedisKey OfKey(int person) => OfPrefix + person.ToString(CultureInfo.InvariantCulture);

    private static RedisKey InviteKey(int person) => InvitePrefix + person.ToString(CultureInfo.InvariantCulture);
}
