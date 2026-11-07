using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Attendance.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Events.Responses;

namespace OpenWorker.Gameplay.Modules.Attendance;

/// <summary>
/// A person who comes on a new day gets the reward of that day of the table, and one who stays in the game gets
/// a reward after each wait. Both go to the person by themselves; the requests of the client only ask to look again.
/// </summary>
public sealed class AttendanceManager(
    World world,
    PersonStateStore store,
    AttendanceCatalog catalog,
    IRewardGranter rewards,
    ServerContent contents,
    TimeProvider time,
    IConfiguration configuration,
    ILogger<AttendanceManager> logger) : IMiscTick
{
    private static readonly QueryDescription Everyone = new QueryDescription().WithAll<AttendanceComponent>();

    /// <summary>
    /// The longest time that is counted between two looks, so a pause of the server does not count as played.
    /// </summary>
    private static readonly TimeSpan LongestStep = TimeSpan.FromMinutes(2);

    private TimeSpan Reset => TimeSpan.FromHours(configuration.GetValue("Attendance:ResetHour", AttendanceRules.DefaultResetTime.TotalHours));

    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<AttendanceBook>(person, PersonStateKind.Attendance, cancellationToken) ?? new AttendanceBook();

        world.Set(player, new AttendanceComponent { Person = person, Book = book, LastTick = time.GetLocalNow() });

        await RefreshAsync(player, cancellationToken);
    }

    /// <summary>
    /// Looks whether the person has come on a new day, and tells the client about the attendance the first time.
    /// </summary>
    public async ValueTask RefreshAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component) || !contents[ServerContentIndex.Attendance])
        {
            return;
        }

        var book = component.Book!;
        var session = world.Get<ServerSessionComponent>(player);

        var before = (book.Count, book.LastDay);
        var advance = AttendanceRules.Advance(book, catalog, time.GetLocalNow(), Reset);

        if (!advance.HasTable)
        {
            return;
        }

        var changed = advance.Attended || advance.PlayTimeReset;

        if (advance.Attended)
        {
            var reward = catalog.GetReward(book.Table, advance.Day);

            if (!reward.IsEmpty && !await rewards.TryGrantAsync(player, reward, RewardSource.Attendance, cancellationToken))
            {
                // It is tried again the next time the person is looked at.
                AttendanceRules.Undo(book, before.LastDay);
                changed = advance.PlayTimeReset;
                advance = advance with { Attended = false };
            }
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        if (!component.Sent)
        {
            world.Set(player, world.Get<AttendanceComponent>(player) with { Sent = true });

            session.Send(new EventAttendanceLoadResponse { Info = book.ToInfo(), Time = book.ToPlayTime() });
        }

        else
        {
            if (advance.Attended)
            {
                session.Send(new EventAttendanceRewardResponse { Info = book.ToInfo() });
            }

            if (advance.PlayTimeReset)
            {
                session.Send(new EventAttendancePlayTimeInitResponse());
            }
        }

        if (changed)
        {
            await SaveAsync(player, book, cancellationToken);
        }
    }

    /// <summary>
    /// Counts the time played since the last look and gives the rewards that are due.
    /// </summary>
    public async ValueTask CountPlayTimeAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component) || !contents[ServerContentIndex.Attendance])
        {
            return;
        }

        var book = component.Book!;
        var now = time.GetLocalNow();
        var elapsed = now - component.LastTick;

        world.Set(player, component with { LastTick = now });

        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        var reached = AttendanceRules.AddPlayTime(book, catalog, (int)Math.Min(elapsed.TotalSeconds, LongestStep.TotalSeconds));

        if (reached.Count == 0)
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(player);

        foreach (var step in reached)
        {
            var reward = new Reward([new RewardItem(catalog.PlayTime[step].Item, 1)]);

            if (!await rewards.TryGrantAsync(player, reward, RewardSource.PlayTime, cancellationToken))
            {
                // The time stays full, so the reward is tried again at the next look.
                book.PlayStep = step;
                book.PlaySeconds = catalog.PlayTime[step].Seconds;
                break;
            }
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        session.Send(new EventAttendancePlayTimeRewardResponse { Time = book.ToPlayTime() });

        await SaveAsync(player, book, cancellationToken);
    }

    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        world.Query(in Everyone, (Entity entity, ref AttendanceComponent component) =>
        {
            if (component.Book is not null)
            {
                players.Add(entity);
            }
        });

        foreach (var player in players)
        {
            try
            {
                await RefreshAsync(player, cancellationToken);
                await CountPlayTimeAsync(player, cancellationToken);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Attendance of a person failed.");
            }
        }
    }

    private bool TryGet(Entity player, out AttendanceComponent component)
    {
        if (world.IsAlive(player) && world.TryGet(player, out component) && component.Book is not null)
        {
            return true;
        }

        component = default;
        return false;
    }

    private async ValueTask SaveAsync(Entity player, AttendanceBook book, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var person = world.Get<AttendanceComponent>(player).Person;

        await book.Gate.WaitAsync(cancellationToken);

        try
        {
            await store.SaveAsync(person, PersonStateKind.Attendance, book, cancellationToken);
        }

        finally
        {
            book.Gate.Release();
        }
    }
}
