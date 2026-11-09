using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.League;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Puts what the members bring into the league in the database and tells the members, wherever they are: the new gold and
/// rank with every share, the whole league again when its level rises (the client shows the level, the experience and the
/// room for members only from the whole league), and the level of a member who reached one.
/// </summary>
public sealed partial class LeagueTreasury(
    IDbContextFactory<PersistenceContext> factory,
    CommunityNetwork network,
    LeagueSnapshots snapshots,
    LeagueLevels levels,
    ILogger<LeagueTreasury> logger
) :
    ILeagueTreasury
{
    public void Add(LeagueWealth wealth)
    {
        _ = AddAsync(wealth);
    }

    public void LevelReached(int league, int person, byte level)
    {
        _ = LevelReachedAsync(league, person, level);
    }

    /// <summary>
    /// Adds the share and tells the members. The task ends when they are told.
    /// </summary>
    public async Task AddAsync(LeagueWealth wealth)
    {
        try
        {
            var grown = await factory.GrowAsync(wealth.League, wealth.Person, wealth.Exp, wealth.Gold, levels.Next);

            if (grown is null)
            {
                return;
            }

            var members = await factory.MemberIdsAsync(wealth.League);

            await network.SendAsync(members, LeagueMapper.ToUpdate(grown.League));

            if (grown.League.Rating <= grown.LevelBefore)
            {
                return;
            }

            LogLevelRose(logger, wealth.League, grown.League.Rating);

            foreach (var member in members)
            {
                if (await snapshots.InfoAsync(member, joined: false) is { } info)
                {
                    await network.SendAsync(member, info);
                }
            }
        }

        catch (Exception e)
        {
            LogShareFailed(logger, e, wealth.League, wealth.Person);
        }
    }

    /// <summary>
    /// Shows the other members the level a member reached. The task ends when they are told.
    /// </summary>
    public async Task LevelReachedAsync(int league, int person, byte level)
    {
        try
        {
            var member = await factory.MemberAsync(person);

            if (member is null || member.League != league)
            {
                return;
            }

            var online = await network.FindAsync(person);
            var others = (await factory.MemberIdsAsync(league)).Where(e => e != person);

            await network.SendAsync(others, LeagueSnapshots.Update(member with { Level = level }, online) with { Level = level });
        }

        catch (Exception e)
        {
            LogLevelFailed(logger, e, league, person);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "League {League} reached level {Level}.")]
    private static partial void LogLevelRose(ILogger logger, int league, byte level);

    [LoggerMessage(Level = LogLevel.Error, Message = "The share of person {Person} did not reach league {League}.")]
    private static partial void LogShareFailed(ILogger logger, Exception exception, int league, int person);

    [LoggerMessage(Level = LogLevel.Error, Message = "League {League} was not told the level of person {Person}.")]
    private static partial void LogLevelFailed(ILogger logger, Exception exception, int league, int person);
}
