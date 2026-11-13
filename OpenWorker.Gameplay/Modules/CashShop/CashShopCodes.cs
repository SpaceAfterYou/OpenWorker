namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// The error codes of the cash shop, as the client words them.
/// </summary>
public static class CashShopCodes
{
    /// <summary>Not a product the shop sells, or a wrong list.</summary>
    public const int WrongProduct = 52309;

    /// <summary>The balance is less than the price.</summary>
    public const int NotEnough = 52310;

    /// <summary>The product is not for sale, or not as a present.</summary>
    public const int NotSold = 52312;

    /// <summary>Nobody has the name a present is for.</summary>
    public const int NoReceiver = 52313;

    /// <summary>A present could not be sent.</summary>
    public const int GiftFailed = 52314;

    /// <summary>The purchase failed; try again later.</summary>
    public const int Failed = 52317;

    /// <summary>A purchase of the player is still being made.</summary>
    public const int Busy = 52318;

    /// <summary>The item is for another class than the buyer's.</summary>
    public const int OtherClass = 52319;

    /// <summary>The item is for another class than the receiver's.</summary>
    public const int ReceiverClass = 52320;

    /// <summary>The sale of the product has not begun or is over.</summary>
    public const int OutOfPeriod = 52322;

    /// <summary>The product may not be bought more in its period.</summary>
    public const int LimitReached = 52323;

    /// <summary>A gacha item to open is missing from the bags, or fewer of it than asked.</summary>
    public const int CountMismatch = 52014;

    /// <summary>The bags have not enough free places.</summary>
    public const int NoRoom = 52010;

    /// <summary>Not a gacha item, or a wrong count of it.</summary>
    public const int NotGacha = 52011;

    /// <summary>The gacha may not be opened today; the original's code, whose text speaks of the bank.</summary>
    public const int GachaClosed = 52043;

    /// <summary>The draw of a gacha failed.</summary>
    public const int GachaFailed = 59100;
}
