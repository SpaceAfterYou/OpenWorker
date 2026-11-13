using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// How the purchases of a product are counted (<c>tb_CashShop</c>): by the character or by the account, for good or for a
/// day, a week or a month.
/// </summary>
public enum CashLimitKind : byte
{
    None = 0,
    Character = 1,
    Account = 2,
    CharacterDay = 3,
    AccountDay = 4,
    CharacterWeek = 5,
    AccountWeek = 6,
    CharacterMonth = 7,
    AccountMonth = 8
}

/// <summary>
/// The periods the purchases of a limited product are counted in. A day starts at 09:00, a week on Tuesday at 09:00 and a
/// month on its first day at 09:00, by the local time of the server; a count for good has one period that never ends.
/// </summary>
public static class CashLimits
{
    public static readonly TimeSpan DayStart = TimeSpan.FromHours(9);

    /// <summary>
    /// The period of a count for good.
    /// </summary>
    public static readonly DateTime Always = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static bool ByAccount(CashLimitKind kind)
    {
        return kind is CashLimitKind.Account or CashLimitKind.AccountDay or CashLimitKind.AccountWeek or CashLimitKind.AccountMonth;
    }

    /// <summary>
    /// When the period that runs at <paramref name="now"/> started, in UTC.
    /// </summary>
    public static DateTime PeriodOf(CashLimitKind kind, DateTimeOffset now)
    {
        var (day, _) = GameDay.Get(now, DayStart);

        return kind switch
        {
            CashLimitKind.CharacterDay or CashLimitKind.AccountDay => day.UtcDateTime,
            CashLimitKind.CharacterWeek or CashLimitKind.AccountWeek => day.AddDays(-(((int)day.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7)).UtcDateTime,
            CashLimitKind.CharacterMonth or CashLimitKind.AccountMonth => day.AddDays(1 - day.Day).UtcDateTime,
            _ => Always
        };
    }
}
