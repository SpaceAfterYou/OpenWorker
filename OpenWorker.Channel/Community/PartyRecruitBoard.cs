using System.Globalization;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Party.Enums;
using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What the master of a recruitment wrote on it.
/// </summary>
public sealed record PartyRecruitTerms(string Message, short MinLevel, short MaxLevel, byte Purpose, int Place);

/// <summary>
/// Somebody who applied to a recruitment, as the master sees them.
/// </summary>
/// <param name="Until">When the application expires, in Unix seconds.</param>
public sealed record PartyApplicant(PartyMemberCard Card, long Until);

/// <summary>
/// A recruitment of the board. Its number is the number of the party it fills: the party of the master, or the one the first
/// accepted applicant makes with them.
/// </summary>
/// <param name="Until">When the recruitment expires, in Unix seconds.</param>
public sealed record PartyRecruit(int Id, int Master, string MasterName, PartyRecruitTerms Terms, long Until, IReadOnlyList<PartyApplicant> Applicants)
{
    public PartyApplicant? Applicant(int person) => Applicants.FirstOrDefault(e => e.Card.Person == person);
}

/// <summary>
/// The recruitment board of every server, in Redis: recruitments that expire, the applications of each, the penalty of a
/// master who took theirs down. A change that has a limit is one script, so two servers cannot both take the last place.
/// </summary>
/// <remarks>
/// <c>ow:party:recruit:{id}</c> is a hash of the recruitment with one <c>a:{person}</c> per applicant; <c>ow:party:recruits</c>
/// orders the recruitments by their end; <c>ow:party:recruit-of:{master}</c> names the one of a master;
/// <c>ow:party:applied:{person}</c> holds what a person applied to; <c>ow:party:recruit-penalty:{master}</c> is the penalty.
/// </remarks>
public sealed class PartyRecruitBoard(IConnectionMultiplexer redis, TimeProvider time)
{
    private const string RecruitPrefix = "ow:party:recruit:";
    private const string Index = "ow:party:recruits";
    private const string OfPrefix = "ow:party:recruit-of:";
    private const string AppliedPrefix = "ow:party:applied:";
    private const string PenaltyPrefix = "ow:party:recruit-penalty:";
    private const char Separator = '\n';

    // KEYS: the recruitment, what the person applied to. ARGV: the person, the applicant, its level, now, the most applicants,
    // the most applications, the recruitment's number. Returns the outcome (see PartyApplyError) and the applications after.
    // An application whose time is up is taken off first: it holds no place, and its person may apply again.
    private static RedisScript Apply { get; } = new("""
        local ends = tonumber(redis.call('HGET', KEYS[1], 'until') or '0')
        if ends <= tonumber(ARGV[4]) then
            return {1, redis.call('SCARD', KEYS[2])}
        end
        local level = tonumber(ARGV[3])
        if level < tonumber(redis.call('HGET', KEYS[1], 'min')) or level > tonumber(redis.call('HGET', KEYS[1], 'max')) then
            return {7, redis.call('SCARD', KEYS[2])}
        end
        local count = 0
        local fields = redis.call('HGETALL', KEYS[1])
        for i = 1, #fields, 2 do
            if string.sub(fields[i], 1, 2) == 'a:' then
                if (tonumber(string.match(fields[i + 1], '(%d+)$')) or 0) <= tonumber(ARGV[4]) then
                    redis.call('HDEL', KEYS[1], fields[i])
                else
                    count = count + 1
                end
            end
        end
        if redis.call('HEXISTS', KEYS[1], 'a:' .. ARGV[1]) == 1 then
            return {2, redis.call('SCARD', KEYS[2])}
        end
        if count >= tonumber(ARGV[5]) then
            return {6, redis.call('SCARD', KEYS[2])}
        end
        if redis.call('SCARD', KEYS[2]) >= tonumber(ARGV[6]) then
            return {5, redis.call('SCARD', KEYS[2])}
        end
        redis.call('HSET', KEYS[1], 'a:' .. ARGV[1], ARGV[2])
        redis.call('SADD', KEYS[2], ARGV[7])
        return {0, redis.call('SCARD', KEYS[2])}
        """);

    // KEYS: the recruitment, the index, the pointer of its master. ARGV: the number. The first who takes it gets its fields.
    private static RedisScript Take { get; } = new("""
        local fields = redis.call('HGETALL', KEYS[1])
        redis.call('DEL', KEYS[1])
        redis.call('ZREM', KEYS[2], ARGV[1])
        if redis.call('GET', KEYS[3]) == ARGV[1] then
            redis.call('DEL', KEYS[3])
        end
        return fields
        """);

    private IDatabase Database => redis.GetDatabase();

    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    /// <summary>
    /// Puts up the recruitment of the master, or renews the one they have: its terms change, its applicants stay, and it
    /// lives <see cref="PartyRules.RecruitLifetime"/> from now.
    /// </summary>
    public async Task<PartyRecruit> PutAsync(int id, int master, string masterName, PartyRecruitTerms terms)
    {
        var until = Now + (long)PartyRules.RecruitLifetime.TotalSeconds;
        var key = RecruitKey(id);

        await Database.HashSetAsync(key,
        [
            new("master", master),
            new("name", masterName),
            new("msg", terms.Message),
            new("min", terms.MinLevel),
            new("max", terms.MaxLevel),
            new("purpose", (int)terms.Purpose),
            new("place", terms.Place),
            new("until", until)
        ]);

        await Task.WhenAll(
            Database.KeyExpireAsync(key, PartyRules.RecruitLifetime + PartyRules.ApplicationLifetime),
            Database.SortedSetAddAsync(Index, id, until),
            Database.StringSetAsync(OfKey(master), id, PartyRules.RecruitLifetime));

        return (await GetAsync(id))!;
    }

    /// <summary>
    /// The recruitment, while it lasts.
    /// </summary>
    public async Task<PartyRecruit?> GetAsync(int id)
    {
        var record = Decode(id, await Database.HashGetAllAsync(RecruitKey(id)));

        return record is not null && record.Until > Now ? record : null;
    }

    /// <summary>
    /// The recruitment the master has up, while it lasts.
    /// </summary>
    public async Task<PartyRecruit?> OfAsync(int master)
    {
        var id = await Database.StringGetAsync(OfKey(master));

        return id.TryParse(out int recruit) && await GetAsync(recruit) is { } record && record.Master == master ? record : null;
    }

    /// <summary>
    /// Every recruitment that lasts, the oldest first.
    /// </summary>
    public async Task<IReadOnlyList<PartyRecruit>> ListAsync()
    {
        var ids = await Database.SortedSetRangeByScoreAsync(Index, Now + 1, double.PositiveInfinity);
        var list = new List<PartyRecruit>(ids.Length);

        foreach (var id in ids)
        {
            if (id.TryParse(out int recruit) && await GetAsync(recruit) is { } record)
            {
                list.Add(record);
            }
        }

        return list;
    }

    /// <summary>
    /// The person applies to the recruitment. Returns how it went and how many applications the person has now.
    /// </summary>
    public async Task<(PartyApplyError Outcome, int Applications)> ApplyAsync(int id, PartyMemberCard applicant)
    {
        await PruneAsync(applicant.Person);

        var until = Now + (long)PartyRules.ApplicationLifetime.TotalSeconds;
        var value = string.Join(Separator, applicant.Encode(), until.ToString(CultureInfo.InvariantCulture));

        var result = (int[])(await Apply.EvaluateAsync(
            redis,
            [RecruitKey(id), AppliedKey(applicant.Person)],
            [applicant.Person, value, (int)applicant.Level, Now, PartyRules.MaxApplicants, PartyRules.MaxApplications, id]))!;

        if (result[0] == 0)
        {
            await Database.KeyExpireAsync(AppliedKey(applicant.Person), PartyRules.ApplicationLifetime);
        }

        return ((PartyApplyError)result[0], result[1]);
    }

    /// <summary>
    /// Takes the application of the person off the recruitment. False when there was none.
    /// </summary>
    public async Task<bool> WithdrawAsync(int id, int person)
    {
        var removed = await Database.HashDeleteAsync(RecruitKey(id), "a:" + person.ToString(CultureInfo.InvariantCulture));

        await Database.SetRemoveAsync(AppliedKey(person), id);

        return removed;
    }

    /// <summary>
    /// The recruitments the person has applied to and that still last.
    /// </summary>
    public async Task<IReadOnlyList<PartyRecruit>> AppliedAsync(int person)
    {
        var list = new List<PartyRecruit>();

        foreach (var id in await Database.SetMembersAsync(AppliedKey(person)))
        {
            if (id.TryParse(out int recruit) && await GetAsync(recruit) is { } record && record.Applicant(person) is not null)
            {
                list.Add(record);
            }
        }

        return list;
    }

    /// <summary>
    /// Takes the recruitment down and returns it as it was; null when it was gone already, or another server took it first.
    /// </summary>
    public async Task<PartyRecruit?> TakeAsync(int id)
    {
        // The pointer of the master is read before the recruitment goes, so that it can go with it.
        var master = await Database.HashGetAsync(RecruitKey(id), "master");
        var fields = (RedisValue[])(await Take.EvaluateAsync(redis, [RecruitKey(id), Index, OfPrefix + (string?)master], [id]))!;

        return fields.Length == 0 ? null : Decode(id, ToEntries(fields));
    }

    /// <summary>
    /// Takes down every recruitment whose time is up and returns them, each to one server only.
    /// </summary>
    public async Task<IReadOnlyList<PartyRecruit>> ExpireAsync()
    {
        var ids = await Database.SortedSetRangeByScoreAsync(Index, double.NegativeInfinity, Now);
        var gone = new List<PartyRecruit>();

        foreach (var id in ids)
        {
            if (!id.TryParse(out int recruit))
            {
                await Database.SortedSetRemoveAsync(Index, id);
                continue;
            }

            if (await TakeAsync(recruit) is { } record)
            {
                gone.Add(record);
            }
        }

        return gone;
    }

    /// <summary>
    /// How long the master still waits before they may put up a recruitment again.
    /// </summary>
    public async Task<TimeSpan> PenaltyAsync(int master)
    {
        return await Database.KeyTimeToLiveAsync(PenaltyKey(master)) ?? TimeSpan.Zero;
    }

    public Task PenalizeAsync(int master)
    {
        return Database.StringSetAsync(PenaltyKey(master), 1, PartyRules.RecruitPenalty);
    }

    private async Task PruneAsync(int person)
    {
        foreach (var id in await Database.SetMembersAsync(AppliedKey(person)))
        {
            if (!id.TryParse(out int recruit) || await GetAsync(recruit) is not { } record || record.Applicant(person) is not { } applicant || applicant.Until <= Now)
            {
                await Database.SetRemoveAsync(AppliedKey(person), id);
            }
        }
    }

    private PartyRecruit? Decode(int id, HashEntry[] fields)
    {
        if (fields.Length == 0)
        {
            return null;
        }

        var values = new Dictionary<string, RedisValue>();
        var applicants = new List<PartyApplicant>();
        var now = Now;

        foreach (var field in fields)
        {
            var name = (string)field.Name!;

            if (name.StartsWith("a:", StringComparison.Ordinal))
            {
                if (int.TryParse(name[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var person) &&
                    DecodeApplicant(person, field.Value) is { } applicant && applicant.Until > now)
                {
                    applicants.Add(applicant);
                }
            }

            else
            {
                values[name] = field.Value;
            }
        }

        if (!values.TryGetValue("master", out var master) || !master.TryParse(out int masterId))
        {
            return null;
        }

        var terms = new PartyRecruitTerms(
            (string?)values.GetValueOrDefault("msg") ?? string.Empty,
            (short)(int)values.GetValueOrDefault("min"),
            (short)(int)values.GetValueOrDefault("max"),
            (byte)(int)values.GetValueOrDefault("purpose"),
            (int)values.GetValueOrDefault("place"));

        return new PartyRecruit(
            id,
            masterId,
            (string?)values.GetValueOrDefault("name") ?? string.Empty,
            terms,
            (long)values.GetValueOrDefault("until"),
            applicants.OrderBy(e => e.Until).ToArray());
    }

    private static PartyApplicant? DecodeApplicant(int person, RedisValue value)
    {
        var text = (string?)value;
        var cut = text?.LastIndexOf(Separator) ?? -1;

        if (text is null || cut < 0 || !long.TryParse(text[(cut + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var until))
        {
            return null;
        }

        return new PartyApplicant(PartyMemberCard.Decode(person, text[..cut]), until);
    }

    private static HashEntry[] ToEntries(RedisValue[] flat)
    {
        var entries = new HashEntry[flat.Length / 2];

        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = new HashEntry(flat[i * 2], flat[i * 2 + 1]);
        }

        return entries;
    }

    private static RedisKey RecruitKey(int id) => RecruitPrefix + id.ToString(CultureInfo.InvariantCulture);

    private static RedisKey OfKey(int master) => OfPrefix + master.ToString(CultureInfo.InvariantCulture);

    private static RedisKey AppliedKey(int person) => AppliedPrefix + person.ToString(CultureInfo.InvariantCulture);

    private static RedisKey PenaltyKey(int master) => PenaltyPrefix + master.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Somebody who says they look for a party.
/// </summary>
/// <param name="Until">When the entry expires, in Unix seconds.</param>
public sealed record PartyAwaiter(PartyMemberCard Card, long Until);

/// <summary>
/// The people who look for a party, one list for all the servers (<c>ow:party:awaiters</c>, a hash by person). An entry is
/// forgotten after <see cref="PartyRules.AwaiterLifetime"/> or when its person joins a party.
/// </summary>
public sealed class PartyAwaiterBoard(IConnectionMultiplexer redis, TimeProvider time)
{
    private const string Key = "ow:party:awaiters";

    private IDatabase Database => redis.GetDatabase();

    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    public async Task<PartyAwaiter> AddAsync(PartyMemberCard card)
    {
        var until = Now + (long)PartyRules.AwaiterLifetime.TotalSeconds;

        await Database.HashSetAsync(Key, card.Person, string.Join('\n', card.Encode(), until.ToString(CultureInfo.InvariantCulture)));

        return new PartyAwaiter(card, until);
    }

    public async Task<bool> RemoveAsync(int person)
    {
        return await Database.HashDeleteAsync(Key, person);
    }

    public async Task<IReadOnlyList<PartyAwaiter>> ListAsync()
    {
        var list = new List<PartyAwaiter>();
        var now = Now;

        foreach (var entry in await Database.HashGetAllAsync(Key))
        {
            var text = (string?)entry.Value;
            var cut = text?.LastIndexOf('\n') ?? -1;

            if (!entry.Name.TryParse(out int person) || text is null || cut < 0 ||
                !long.TryParse(text[(cut + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var until) || until <= now)
            {
                await Database.HashDeleteAsync(Key, entry.Name);
                continue;
            }

            list.Add(new PartyAwaiter(PartyMemberCard.Decode(person, text[..cut]), until));
        }

        return list.OrderBy(e => e.Until).ToArray();
    }
}
