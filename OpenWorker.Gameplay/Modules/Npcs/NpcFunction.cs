namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// What an NPC of a city does for a player, as the three function columns of its row of <c>tb_NPC</c> name it. The number
/// next to a function (its id) is the shop group for a shop, the recipe group for production, the transport row for a
/// transport.
/// </summary>
public enum NpcFunction : byte
{
    /// <summary>
    /// Any NPC: the service asks for no function, only for the NPC itself and the distance.
    /// </summary>
    None = 0,

    Shop = 1,
    Bank = 2,

    /// <summary>
    /// Upgrades and evolutions of items.
    /// </summary>
    Upgrade = 3,

    Post = 4,
    Auction = 5,
    Production = 6,
    League = 7,

    /// <summary>
    /// Carries the player along a path of the city for gold.
    /// </summary>
    Transport = 8,

    Socket = 9,
    Warlord = 10,
    AkashicMix = 11,
    Repair = 12,
    Helper = 13,
    Exchange = 14,
    Gacha = 15,
    Recycle = 16,
    MyRoom = 17,
    Renovate = 18,
    Dye = 19,
    CashMileage = 20
}
