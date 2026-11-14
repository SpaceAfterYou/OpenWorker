using System.Globalization;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Post.Responses;

namespace OpenWorker.Gameplay.Modules.Ranking;

/// <summary>
/// The periods of the time attack ranking. A period ends every Monday at midnight of the server; then each place of a map
/// that the reward table names gets its reward by a letter of the game to its account, and the list of the map starts
/// empty. The client tells its players both: the raid ranking starts anew with a new season, and its rewards come by mail.
/// A period ends once whichever server ends it; a server that was down at the end ends it when it starts.
/// </summary>
public sealed partial class TimeAttackPeriods(
    World world,
    IRankingStore store,
    TimeAttackRewards rewards,
    InventoryService inventory,
    IPostStore? post = null,
    TimeProvider? time = null,
    ILogger<TimeAttackPeriods>? logger = null) : IMiscTick
{
    private const string Sender = "SoulWorker";

    /// <summary>The most items one letter carries.</summary>
    private const int ItemsPerLetter = 5;

    private static readonly QueryDescription Accounts = new QueryDescription().WithAll<ClaimsComponent, ServerSessionComponent>();

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly ILogger _logger = logger ?? NullLogger<TimeAttackPeriods>.Instance;

    // The last end every map has been through, so that a tick asks the database once per period.
    private DateTimeOffset _ended = DateTimeOffset.MinValue;
    private bool _running;

    /// <summary>
    /// The end of the period that ended last before <paramref name="now"/>: midnight of the last Monday, in the time of the
    /// server.
    /// </summary>
    public static DateTimeOffset LastEnd(DateTimeOffset now)
    {
        var day = new DateTimeOffset(now.Date, now.Offset);

        return day.AddDays(-(((int)day.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7));
    }

    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var end = LastEnd(_time.GetLocalNow());

        if (end == _ended || _running)
        {
            return;
        }

        if (await EndAsync(end) is not null)
        {
            _ended = end;
        }
    }

    /// <summary>
    /// Ends the period of every map now. The letters it wrote; null when another end was running or did not finish.
    /// </summary>
    public ValueTask<int?> EndNowAsync()
    {
        return _running ? ValueTask.FromResult<int?>(null) : EndAsync(_time.GetUtcNow());
    }

    private async ValueTask<int?> EndAsync(DateTimeOffset end)
    {
        _running = true;

        try
        {
            var letters = 0;
            var done = true;

            foreach (var map in rewards.Maps)
            {
                try
                {
                    letters += await EndMapAsync(map, end.UtcDateTime) ?? 0;
                }

                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // The next tick tries the map again.
                    LogEndFailed(e, map, end);
                    done = false;
                }
            }

            return done ? letters : null;
        }

        finally
        {
            _running = false;
        }
    }

    /// <summary>
    /// Ends the period of one map: the letters it wrote, or null when it had ended already or had nobody in it.
    /// </summary>
    private async ValueTask<int?> EndMapAsync(int map, DateTime end)
    {
        var table = rewards.Of(map);
        var places = await store.PeriodPlacesAsync(RankingKind.TimeAttack, map, end, table.Count == 0 ? 0 : table[^1].Place);

        if (places is null or { Count: 0 })
        {
            return null;
        }

        var parcels = new List<(RankingPlace Place, List<(int Item, int Count)> Stacks)>();
        var unknown = new SortedSet<int>();

        foreach (var place in places)
        {
            var reward = table.FirstOrDefault(e => e.Place == place.Place);

            if (reward.Item == 0 || reward.Count <= 0)
            {
                continue;
            }

            var item = RankingRewardStandIns.Resolve(reward.Item, inventory.Catalog);

            if (item == 0)
            {
                unknown.Add(reward.Item);

                continue;
            }

            var limit = Math.Max(1, (int)inventory.Catalog.StackLimit(item));
            var stacks = new List<(int Item, int Count)>();

            for (var left = reward.Count; left > 0; left -= limit)
            {
                stacks.Add((item, Math.Min(left, limit)));
            }

            parcels.Add((place, stacks));
        }

        if (unknown.Count > 0)
        {
            LogUnknownItems(map, string.Join(", ", unknown));
        }

        var all = parcels.SelectMany(e => e.Stacks).ToList();

        await inventory.EnsureSerialsAsync(all.Select(e => (e.Item, 1)));

        if (!inventory.CanMake(all.Count))
        {
            throw new ItemSerialsExhaustedException();
        }

        var drafts = new List<PostAccountDraft>();

        foreach (var (place, stacks) in parcels)
        {
            var title = "[System] Raid Ranking Reward";
            var body = string.Create(CultureInfo.InvariantCulture,
                $"The raid ranking season has ended. Your reward for place {place.Place} on map {map} is attached. A new season has begun.");

            foreach (var chunk in stacks.Chunk(ItemsPerLetter))
            {
                var items = chunk.Select(e => PostLetters.Attach(inventory.Create(e.Item, e.Count), (short)e.Count)).ToList();

                drafts.Add(new PostAccountDraft(place.Account, Sender, title, body, 0, items, GameLetters.AccountKind, GameLetters.OwnText));
            }
        }

        var written = await store.EndPeriodAsync(RankingKind.TimeAttack, map, end, drafts);

        if (written is null)
        {
            return null;
        }

        LogEnded(map, new DateTimeOffset(end, TimeSpan.Zero), places.Count, written.Count);

        await TellAsync(written);

        return written.Count;
    }

    /// <summary>
    /// Shows the new letters to the players of this server whose accounts they went to.
    /// </summary>
    private async ValueTask TellAsync(IReadOnlyList<PostPersistent> letters)
    {
        if (post is null || letters.Count == 0)
        {
            return;
        }

        var present = new Dictionary<int, Entity>();

        world.Query(in Accounts, (Entity entity, ref ClaimsComponent claims) => present.TryAdd(claims.Account, entity));

        foreach (var letter in letters)
        {
            if (letter.ReceiverAccountId is not { } account || !present.TryGetValue(account, out var player))
            {
                continue;
            }

            int unread;

            try
            {
                (_, unread) = await post.CountUnreadAsync(0, account);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The letter is in; the player sees it when the post is opened.
                LogNotTold(e, account);

                continue;
            }

            if (world.IsAlive(player) && world.TryGet(player, out ServerSessionComponent session))
            {
                session.Send(new PostAccountRecvResponse
                {
                    PostCount = (short)Math.Min(unread, short.MaxValue),
                    Value = PostLetters.Account(letter)
                });
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The time attack period of map {Map} ended at {End}: {Places} places, {Letters} letters")]
    private partial void LogEnded(int map, DateTimeOffset end, int places, int letters);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The time attack rewards of map {Map} name items the item table lacks, given to nobody: {Items}")]
    private partial void LogUnknownItems(int map, string items);

    [LoggerMessage(Level = LogLevel.Error, Message = "The time attack period of map {Map} that ends at {End} did not end; it is tried again")]
    private partial void LogEndFailed(Exception exception, int map, DateTimeOffset end);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} was not told of its ranking reward")]
    private partial void LogNotTold(Exception exception, int account);
}
