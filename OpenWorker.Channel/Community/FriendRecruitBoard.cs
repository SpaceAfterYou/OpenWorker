using System.Globalization;
using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

public sealed record FriendRecruit(int Person, string Name, byte Hero, string Memo, byte State, DateTime Since);

/// <summary>
/// Players who say they look for friends. One list for all the servers, and an entry is forgotten after a day.
/// </summary>
public sealed class FriendRecruitBoard(IConnectionMultiplexer redis)
{
    private const string Key = "ow:friend:recruit";
    private const char Separator = '\n';

    public static TimeSpan Lifetime { get; } = TimeSpan.FromDays(1);

    private IDatabase Database => redis.GetDatabase();

    public async Task AddAsync(FriendRecruit recruit)
    {
        var value = string.Join(
            Separator,
            recruit.Name,
            recruit.Hero.ToString(CultureInfo.InvariantCulture),
            recruit.State.ToString(CultureInfo.InvariantCulture),
            recruit.Since.Ticks.ToString(CultureInfo.InvariantCulture),
            recruit.Memo);

        await Database.HashSetAsync(Key, recruit.Person, value);
    }

    public async Task<bool> RemoveAsync(int person)
    {
        return await Database.HashDeleteAsync(Key, person);
    }

    public async Task<bool> ContainsAsync(int person)
    {
        return (await ListAsync()).Any(e => e.Person == person);
    }

    public async Task<IReadOnlyList<FriendRecruit>> ListAsync()
    {
        var all = await Database.HashGetAllAsync(Key);
        var now = DateTime.UtcNow;
        var list = new List<FriendRecruit>();

        foreach (var entry in all)
        {
            var recruit = Decode((int)entry.Name, entry.Value);

            if (recruit is null || now - recruit.Since > Lifetime)
            {
                await Database.HashDeleteAsync(Key, entry.Name);
                continue;
            }

            list.Add(recruit);
        }

        return list.OrderBy(e => e.Since).ToArray();
    }

    private static FriendRecruit? Decode(int person, string? value)
    {
        var parts = value?.Split(Separator, 5);

        if (parts is not { Length: 5 } ||
            !byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hero) ||
            !byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ||
            !long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            return null;
        }

        return new FriendRecruit(person, parts[0], hero, parts[4], state, new DateTime(ticks, DateTimeKind.Utc));
    }
}
