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
    public byte[] OptionList { get; set; } = [];

    public int Outfit { get; set; }
    public DateTime? LastSelectedAt { get; set; }

    /// <summary>
    /// When the person last left a server of the game, for the friends who look at the list.
    /// </summary>
    public DateTime? LastLogoutAt { get; set; }

    public required short FatiguePointCommon { get; set; }
    public short FatiguePointBonus { get; set; }
    
    public byte Level { get; set; } = 1;
    public int Experience { get; set; }

    public long Gold { get; set; }
    public long BattlePoint { get; set; }
    public long Ether { get; set; }

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