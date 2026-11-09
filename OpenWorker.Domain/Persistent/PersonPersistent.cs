using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Persistent;

public sealed class PersonPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required string Name { get; set; } = string.Empty;

    public required Hero Hero { get; init; }
    
    public required short DefaultHairStyle { get; set; }
    public required short DefaultHairColor { get; set; }
    public required short DefaultEyeColor { get; set; }
    public required short DefaultSkinColor { get; set; }

    public required short EquippedHairStyle { get; set; }
    public required short EquippedHairColor { get; set; }
    public required short EquippedEyeColor { get; set; }
    public required short EquippedSkinColor { get; set; }
    
    public required short Location { get; set; }
    
    public required float PositionX { get; set; }
    public required float PositionY { get; set; }
    public required float PositionZ { get; set; }
    
    public required float RotationX { get; set; }

    public int TitlePrimary { get; set; }
    public int TitleSecondary { get; set; }
    public int TitleAbilityPrimary { get; set; }
    public int TitleAbilitySecondary { get; set; }

    /// <summary>
    /// The titles the person has earned.
    /// </summary>
    public int[] TitleList { get; set; } = [];

    public byte CommunityState { get; set; }
    public string CommunityComment { get; set; } = string.Empty;
    public string CommunityMemo { get; set; } = string.Empty;
    
    public int[] GestureList { get; set; } = [];

    /// <summary>
    /// The options of the client as they were kept before they moved to the account (<see cref="AccountPersistent.OptionList"/>).
    /// Nothing reads it any more; the migration that moved them copied it, and a later one can drop the column.
    /// </summary>
    public byte[] OptionList { get; set; } = [];

    public int Outfit { get; set; }
    public DateTime? LastSelectedAt { get; set; }

    /// <summary>
    /// The character has not played the prologue yet: the gate sends it to the first tutorial maze instead of its
    /// district, and leaving the tutorial clears it, once. Characters made before there was a prologue count as having
    /// played it.
    /// </summary>
    public bool ProloguePending { get; set; }

    /// <summary>
    /// When the person last left a server of the game, for the friends who look at the list.
    /// </summary>
    public DateTime? LastLogoutAt { get; set; }

    /// <summary>
    /// The fatigue of the person: what entering a maze spends, up to 200, filled up each game day.
    /// </summary>
    public required short FatiguePointCommon { get; set; }

    /// <summary>
    /// The fatigue saved up from the days before, up to 400, spent before <see cref="FatiguePointCommon"/>.
    /// </summary>
    public short FatiguePointBonus { get; set; }

    /// <summary>
    /// When the fatigue was last filled up for a new game day; null for a person that has not been filled yet.
    /// </summary>
    public DateTime? FatigueRefilledAt { get; set; }
    
    public byte Level { get; set; } = 1;
    public int Experience { get; set; }

    public long Gold { get; set; }
    public long BattlePoint { get; set; }
    public long Ether { get; set; }

    /// <summary>
    /// The whole wallet, indexed by the currency of the shop (cash, tokens, mileages). Gold, battle points and ether
    /// are also kept in their own columns, which win when both are there, so rows written before the wallet keep them.
    /// </summary>
    public long[] Currencies { get; set; } = [];

    /// <summary>
    /// How far each storage has been extended, indexed by the storage group.
    /// </summary>
    public byte[] StorageGrades { get; set; } = [];

    public int[] QuickSlotCards { get; set; } = [];
    public int[] QuickSlotItems { get; set; } = [];

    public List<ItemPersistent> Items { get; init; } = [];
    
    public required AccountPersistent Account { get; set; }
    public required GatePersistent Gate { get; set; }

    /// <summary>
    /// After leaving a league, or being thrown out, a person cannot join another until then.
    /// </summary>
    public DateTime? LeagueJoinBlockedUntil { get; set; }

    /// <summary>
    /// After disbanding a league its master cannot found another until then.
    /// </summary>
    public DateTime? LeagueCreateBlockedUntil { get; set; }
}