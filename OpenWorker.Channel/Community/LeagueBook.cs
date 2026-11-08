using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Persistence;
using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// An invitation to a league that waits for an answer.
/// </summary>
public sealed record LeagueInvite(int League, int Inviter, string InviterName);

/// <summary>
/// Who is in a league, as the database keeps it, and the invitations that have not been answered. An invitation goes away by
/// itself after a minute.
/// </summary>
public sealed class LeagueBook(IDbContextFactory<PersistenceContext> factory, IConnectionMultiplexer redis) : ILeagueRoster
{
    private const string InvitePrefix = "ow:league:invite:";

    public async ValueTask<IReadOnlyList<int>> MembersAsync(int league)
    {
        return await factory.MemberIdsAsync(league);
    }

    /// <summary>
    /// Remembers the invitation of the person. Returns false if they already have one that waits.
    /// </summary>
    public async Task<bool> InviteAsync(int person, LeagueInvite invite)
    {
        var value = string.Join('\n', invite.League.ToString(CultureInfo.InvariantCulture), invite.Inviter.ToString(CultureInfo.InvariantCulture), invite.InviterName);

        return await redis.GetDatabase().StringSetAsync(InvitePrefix + person, value, LeagueRules.InviteLifetime, When.NotExists);
    }

    /// <summary>
    /// Takes the invitation of the person away and returns it.
    /// </summary>
    public async Task<LeagueInvite?> TakeInviteAsync(int person)
    {
        var value = (string?)await redis.GetDatabase().StringGetDeleteAsync(InvitePrefix + person);
        var parts = value?.Split('\n', 3);

        if (parts is not { Length: 3 } ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var league) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var inviter))
        {
            return null;
        }

        return new LeagueInvite(league, inviter, parts[2]);
    }
}
