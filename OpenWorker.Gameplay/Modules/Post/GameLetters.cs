namespace OpenWorker.Gameplay.Modules.Post;

/// <summary>
/// The kinds of the letters of the game and the rows of the client's table of system letters (tb_SystemMail) they name. For
/// a letter of either kind the client writes the sender, title and text from the row, whatever the letter carries, except
/// for a row of main type 4: then it shows the letter's own, with the row's icon.
/// </summary>
public static class GameLetters
{
    /// <summary>A letter of the game in the box of a person.</summary>
    public const byte PersonKind = 1;

    /// <summary>A letter of the game in the box of an account.</summary>
    public const byte AccountKind = 4;

    /// <summary>
    /// Row (4, 1): the client shows the title and text the server wrote, with the system icon. For a letter no row of the
    /// client speaks of.
    /// </summary>
    public const byte OwnText = 9;

    /// <summary>Row (2, 1): an item bought at the exchange; the text names the item attached.</summary>
    public const byte ExchangeBought = 5;

    /// <summary>Row (2, 2): an item sold at the exchange; the title is the item, the text the count.</summary>
    public const byte ExchangeSold = 6;

    /// <summary>Row (2, 3): a listing its seller took back while it was on sale.</summary>
    public const byte ExchangeTakenBack = 7;

    /// <summary>Row (5, 1): the reward of a maze kept because the inventory had no room.</summary>
    public const byte MazeRewardKept = 11;

    /// <summary>Row (5, 2): the reward of a daily mission.</summary>
    public const byte DailyMissionReward = 12;

    /// <summary>Row (2, 4): a listing whose time ran out, taken back by its seller.</summary>
    public const byte ExchangeEnded = 16;

    /// <summary>Row (2, 5): an item sold at the exchange without a commission; title and text as for a sale.</summary>
    public const byte ExchangeSoldWithoutCommission = 18;
}
