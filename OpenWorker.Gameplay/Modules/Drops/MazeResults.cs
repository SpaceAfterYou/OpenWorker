using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.League;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// A clear of a maze by one player: its rank (0 is F, 5 is SS), its score and how long it took in seconds.
/// </summary>
public readonly record struct MazeClear(byte Rank, int PlayPoint, int Seconds);

/// <summary>
/// A run of a maze for each player in it: what the player does while it plays (kills, the longest combo, the damage it
/// does, the hits it takes, what it picks up), and at the clear the score, the rank and the rewards of the rank, shown in
/// the window of the results (11/75) and, for a party, in the list of the members (11/76).
/// <list type="bullet">
/// <item>The score and the rank come from the reward tables of the server (<see cref="MazeScoring"/>).</item>
/// <item>The experience and the gold of the maze are given at the share of the rank, and in a party the share of the place
/// of the member in the order of the scores on top; the gold that would pass what a person can hold goes by post.</item>
/// <item>The item of the rank is rolled from the buckets the rank reaches and given at once, or sent by post when it does
/// not fit; the key reward of the window is rolled as well (<see cref="MazeKeyRewards"/>).</item>
/// <item>The second half of the fatigue of the maze is spent at the clear, the first when the player comes in
/// (<see cref="FatigueService"/>); the league of the player hears of both ends (<see cref="LeagueGrowth"/>).</item>
/// </list>
/// The last clear stays known (<see cref="LastClear"/>) for what counts clears by rank, until the next run.
/// The combo is timed on the game clock, and the time of the clear is the played time of the maze, the same for every member.
/// </summary>
public sealed partial class MazeResults(
    World world,
    SimulationClock clock,
    MazeKeyRewards? keys = null,
    LeagueGrowth? league = null,
    MazeScoring? scoring = null,
    ExperienceService? experience = null,
    InventoryService? inventory = null,
    FatigueService? fatigue = null,
    BoosterManager? boosters = null,
    IPostStore? post = null,
    AchievementManager? achievements = null,
    ILogger<MazeResults>? logger = null) : ISessionCloseHandler
{
    private readonly ILogger _logger = logger ?? NullLogger<MazeResults>.Instance;

    private sealed class Tally(int maze)
    {
        public int Maze { get; } = maze;
        public int Kills { get; set; }
        public int Experience { get; set; }
        public long Gold { get; set; }
        public int Ether { get; set; }
        public int Combo { get; set; }
        public int MaxCombo { get; set; }
        public long LastHit { get; set; } = long.MinValue;
        public long Damage { get; set; }
        public int HitsTaken { get; set; }
        public int Deaths { get; set; }
        public int? Health { get; set; }
    }

    private readonly Dictionary<Entity, Tally> _tallies = [];
    private readonly Dictionary<Entity, MazeClear> _cleared = [];

    /// <summary>
    /// The player has come into the maze: its run starts, and the first half of the fatigue of the maze is spent.
    /// </summary>
    public void Start(Entity player, int maze)
    {
        _tallies[player] = new Tally(maze)
        {
            Health = world.TryGet<AbilityComponent>(player, out var ability) ? ability.Health.Current : null
        };
        _cleared.Remove(player);
        keys?.Forget(player);
        fatigue?.SpendHalf(player, maze, entering: true);
        league?.OnMazeEntered(player, maze);
    }

    public void AddKill(Entity player, int experience)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Kills++;
            tally.Experience += experience;
        }
    }

    /// <summary>
    /// Experience the player got from a kill of another member of the party.
    /// </summary>
    public void AddExperience(Entity player, int experience)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Experience += experience;
        }
    }

    public void AddGold(Entity player, int gold)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Gold += gold;
        }
    }

    public void AddEther(Entity player, int ether)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Ether += ether;
        }
    }

    /// <summary>
    /// An attack of the player has hit monsters: the combo goes on when it comes within the window of the last hit and
    /// starts again otherwise, and the damage counts toward the share of the player.
    /// </summary>
    public void OnMonstersHit(Entity player, int targets, long damage)
    {
        if (targets <= 0 || !_tallies.TryGetValue(player, out var tally))
        {
            return;
        }

        var now = (long)clock.Time.TotalMilliseconds;

        tally.Combo = Combat.Combo.Next(tally.Combo, tally.LastHit, targets, now);
        tally.MaxCombo = Math.Max(tally.MaxCombo, tally.Combo);
        tally.LastHit = now;
        tally.Damage += Math.Max(0, damage);

        // Every monster hit counts as a skill used, and the combo counts toward the longest the person has made.
        achievements?.Count(player, AchievementType.SkillUsed, targets);
        achievements?.Count(player, AchievementType.Combo, tally.Combo);
    }

    /// <summary>
    /// The life of the player has changed: less life than before is a hit taken, and none left a death.
    /// </summary>
    public void OnHealthChanged(Entity player)
    {
        if (!_tallies.TryGetValue(player, out var tally) || !world.IsAlive(player) || !world.TryGet<AbilityComponent>(player, out var ability))
        {
            return;
        }

        var health = ability.Health.Current;

        if (tally.Health is { } before && health < before)
        {
            tally.HitsTaken++;

            if (health == 0)
            {
                tally.Deaths++;
                achievements?.Count(player, AchievementType.Death);
            }
        }

        tally.Health = health;
    }

    /// <summary>
    /// The clear the player has just finished, until its next run.
    /// </summary>
    public MazeClear? LastClear(Entity player) => _cleared.TryGetValue(player, out var clear) ? clear : null;

    /// <summary>
    /// The maze is finished by a player alone after <paramref name="played"/> of game time: shows the player how it went.
    /// Only once for a maze.
    /// </summary>
    public void Complete(Entity player, TimeSpan played)
    {
        Complete([player], played);
    }

    /// <summary>
    /// The maze is finished by everybody in it: each member is scored, the members are put in the order of their scores,
    /// and each gets its rewards and its window; the members of a party also get the list of all of them with their ranks.
    /// Only once for a maze. <paramref name="played"/> is the played time of the maze, which every member is scored and shown;
    /// <paramref name="hidden"/> is the item of the hidden event of the maze when it was met, which every member gets.
    /// </summary>
    public void Complete(IReadOnlyList<Entity> members, TimeSpan played, HiddenEventReward? hidden = null)
    {
        var runs = new List<(Entity Player, Tally Tally)>(members.Count);

        foreach (var member in members.Distinct())
        {
            if (world.IsAlive(member) && world.Has<ServerSessionComponent>(member) && _tallies.Remove(member, out var tally))
            {
                runs.Add((member, tally));
            }
        }

        if (runs.Count == 0)
        {
            return;
        }

        var milliseconds = Math.Max(0L, (long)played.TotalMilliseconds);
        var seconds = (int)Math.Min(milliseconds / 1000, ushort.MaxValue);
        var damage = runs.Sum(e => e.Tally.Damage);
        var scored = runs.Select(run => (run.Player, run.Tally, Score: Score(run.Player, run.Tally, runs.Count, damage, milliseconds))).ToList();

        // The order of the scores, highest first; equal scores keep the order the members came in.
        var ordered = scored.OrderByDescending(e => e.Score.PlayPoint).ToList();
        var list = new List<MazeRewardPartyMember>(ordered.Count);

        for (var place = 1; place <= ordered.Count; place++)
        {
            var (player, tally, score) = ordered[place - 1];

            Reward(player, tally, score, seconds, ordered.Count, place, hidden);

            list.Add(new MazeRewardPartyMember(
                (byte)place,
                score.Rank,
                world.TryGet<ActorComponent>(player, out var actor) ? actor.Identifier : 0,
                world.TryGet<PersonInfoComponent>(player, out var info) ? info.Name : string.Empty,
                (int)score.PlayPoint));
        }

        // The list of the members is the party's; a player alone has none.
        if (ordered.Count > 1)
        {
            var party = new MazeRewardPartyInfoResponse { Members = list };

            foreach (var (player, _, _) in ordered)
            {
                if (world.IsAlive(player))
                {
                    world.Get<ServerSessionComponent>(player).Send(party);
                }
            }
        }
    }

    private MazeScore Score(Entity player, Tally tally, int partySize, long damage, long milliseconds)
    {
        if (scoring is null)
        {
            return new MazeScore(0f, 0, MazeScoring.LowestRank, 1f, 1f);
        }

        var hero = world.TryGet<PersonInfoComponent>(player, out var info) ? (int)info.Hero : 0;
        var level = world.TryGet<LevelComponent>(player, out var rank) ? rank.Level : 0;
        var share = damage > 0 ? (int)(tally.Damage * 100f / damage) : 0;

        return scoring.Score(new MazeRun(tally.Maze, hero, level, tally.MaxCombo, tally.Kills, share, tally.HitsTaken, partySize, milliseconds));
    }

    private void Reward(Entity player, Tally tally, MazeScore score, int seconds, int members, int place, HiddenEventReward? hidden)
    {
        var (partyExperience, partyGold) = scoring?.PartyShare(members, place) ?? (0f, 0f);
        var (mazeExperience, mazeGold) = scoring is not null && scoring.TryGetRule(tally.Maze, out var rule) ? (rule.RewardExperience, rule.RewardGold) : (0u, 0u);

        // A booster of the rewards of mazes adds its share of each, shown apart in the window.
        var expRate = boosters?.GetRate(player, BoosterEffect.MazeRewardIncExp) ?? 0f;
        var goldRate = boosters?.GetRate(player, BoosterEffect.MazeRewardIncMoney) ?? 0f;

        var exp = mazeExperience * (score.ExperienceShare + partyExperience);
        var specialExp = exp > 0f && expRate > 0f ? (int)(exp * expRate) : 0;
        var playExp = (int)Math.Ceiling(exp);

        var gold = (int)(mazeGold * (score.GoldShare + partyGold));
        var specialGold = gold > 0 && goldRate > 0f ? (int)(gold * goldRate) : 0;

        experience?.Add(player, playExp + specialExp);
        tally.Experience += playExp + specialExp;

        var mail = new List<PostItemPersistent>();
        long mailGold = 0;

        if (gold + specialGold > 0 && inventory is not null)
        {
            // Gold of a clear that the purse would not hold goes by post.
            if (!inventory.CanReceive(player, ShopCurrency.Gold, gold + specialGold))
            {
                mailGold = gold + specialGold;
            }
            else
            {
                inventory.TryAddGold(player, gold + specialGold);
            }

            tally.Gold += gold + specialGold;
        }

        var item = keys?.RollForRank(tally.Maze, score.Rank);

        if (item is { } given && inventory is not null && !inventory.TryGive(player, given.Item, given.Count))
        {
            mail.Add(new PostItemPersistent { Item = given.Item, Count = given.Count, Endurance = 100 });
        }

        // The item of a met hidden event goes by post too when it does not fit, and the window says so.
        var eventItemByPost = false;

        if (hidden is { } paid && inventory is not null && !inventory.TryGive(player, paid.Item, paid.Count))
        {
            mail.Add(new PostItemPersistent { Item = paid.Item, Count = paid.Count, Endurance = 100 });
            eventItemByPost = true;
        }

        var offer = keys?.Roll(player, tally.Maze);

        fatigue?.SpendHalf(player, tally.Maze, entering: false);
        league?.OnMazeFinished(player, tally.Maze, gold + specialGold);

        _cleared[player] = new MazeClear(score.Rank, (int)score.PlayPoint, seconds);

        LogScored(_logger, tally.Maze, score.Rank, score.PlayPoint, place, members, playExp + specialExp, gold + specialGold);

        world.Get<ServerSessionComponent>(player).Send(new MazeRewardInfoResponse
        {
            Rank = score.Rank,
            PlayPoint = (int)score.PlayPoint,
            ClearPoint = score.ClearPoint,
            ClearTime = (ushort)seconds,
            TotalCombo = tally.MaxCombo,
            PlayExp = playExp,
            PlayGold = gold,
            SpecialExp = specialExp,
            SpecialGold = specialGold,
            TotalExp = tally.Experience,
            TotalMoney = (int)Math.Min(tally.Gold, int.MaxValue),
            TotalEther = tally.Ether,
            BaseItem = item?.Item ?? 0,
            BaseItemCount = item?.Count ?? 0,
            CashItem = offer?.Item ?? 0,
            CashItemCount = offer?.Count ?? 0,
            EventItem = hidden?.Item ?? 0,
            EventItemCount = hidden?.Count ?? 0,
            EventItemByPost = (byte)(eventItemByPost ? 1 : 0)
        });

        if ((mail.Count > 0 || mailGold > 0) && post is not null && world.TryGet<ClaimsComponent>(player, out var claims))
        {
            _ = MailAsync(player, claims.Account, mailGold, mail);
        }
    }

    /// <summary>
    /// What does not fit goes as a letter of the game to the account of the player, who is told of it while still here.
    /// </summary>
    private async Task MailAsync(Entity player, int account, long gold, List<PostItemPersistent> items)
    {
        try
        {
            var letter = await post!.SendToAccountAsync(new PostAccountDraft(
                account,
                "SoulWorker",
                "Maze",
                "The reward of a maze that did not fit.",
                gold,
                items,
                GameLetters.AccountKind,
                GameLetters.MazeRewardKept));

            var (_, unread) = await post.CountUnreadAsync(0, account);

            if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
            {
                world.Get<ServerSessionComponent>(player).Send(new PostAccountRecvResponse
                {
                    PostCount = (short)Math.Min(unread, short.MaxValue),
                    Value = PostLetters.Account(letter)
                });
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogMailFailed(_logger, e, account, gold, items.Count);
        }
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        _tallies.Remove(player);
        _cleared.Remove(player);
        keys?.Forget(player);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Maze {Maze} cleared at rank {Rank} with {Score} points, place {Place} of {Members}: {Experience} experience, {Gold} gold")]
    private static partial void LogScored(ILogger logger, int maze, byte rank, float score, int place, int members, int experience, int gold);

    [LoggerMessage(Level = LogLevel.Error, Message = "The reward of a maze could not be sent to account {Account} by post: {Gold} gold, {Items} items")]
    private static partial void LogMailFailed(ILogger logger, Exception exception, int account, long gold, int items);
}
