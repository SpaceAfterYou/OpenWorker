using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PersonValue
{
    public PersonValue(
        ActorValue actor,
        PersonInfoValue infoValue,
        int account,
        byte level,
        FactionGroup faction,
        StuffLevel stuffLevel,
        int pvpKillCount,
        EquipItemValueEntry primaryWeapon,
        EquipItemValueEntry secondaryWeapon,
        EquipItemValueEntry[] equipItems,
        TitleValue title,
        LeagueValue league,
        AbilityValue ability,
        PrivateShopValue privateShop,
        FatiguePointsValue fatiguePoints,
        RankValue rank,
        bool battlePose,
        int status,
        StatusEffectValueEntry[] statusEffects)
    {
        Actor = actor;
        InfoValue = infoValue;
        Account = account;
        Level = level;
        Faction = faction;
        StuffLevel = stuffLevel;
        PvPKillCount = pvpKillCount;
        PrimaryWeapon = primaryWeapon;
        SecondaryWeapon = secondaryWeapon;
        EquipItems = equipItems;
        Title = title;
        League = league;
        Ability = ability;
        PrivateShop = privateShop;
        FatiguePoints = fatiguePoints;
        Rank = rank;
        BattlePose = battlePose;
        Status = status;
        StatusEffects = statusEffects;
    }

    public PersonValue(BinaryReader reader)
    {
        Actor = new ActorValue(reader);
        InfoValue = new PersonInfoValue(reader);
        Level = reader.ReadByte();
        Faction = reader.ReadFactionGroup();
        Account = reader.ReadInt32();
        StuffLevel = reader.ReadStuffLevel();
        PvPKillCount = reader.ReadInt32();
        PrimaryWeapon = new EquipItemValueEntry(reader);
        SecondaryWeapon = new EquipItemValueEntry(reader);
        EquipItems = reader.ReadEquippedItems();
        Title = new TitleValue(reader);
        League = new LeagueValue(reader);
        Ability = new AbilityValue(reader);
        PrivateShop = new PrivateShopValue(reader);
        FatiguePoints = new FatiguePointsValue(reader);
        Rank = new RankValue(reader);
        BattlePose = reader.ReadBoolean();
        Status = reader.ReadInt32();

        var statusEffectCount = reader.ReadByte();

        StatusEffects = Enumerable
            .Range(0, statusEffectCount)
            .Select(_ => new StatusEffectValueEntry(reader))
            .ToArray();
    }

    public ActorValue Actor { get; }
    public PersonInfoValue InfoValue { get; }
    public int Account { get; }
    public byte Level { get; }
    public FactionGroup Faction { get; }
    public StuffLevel StuffLevel { get; }
    public int PvPKillCount { get; }
    public EquipItemValueEntry PrimaryWeapon { get; }
    public EquipItemValueEntry SecondaryWeapon { get; }
    public EquipItemValueEntry[] EquipItems { get; }
    public TitleValue Title { get; }
    public LeagueValue League { get; }
    public AbilityValue Ability { get; }
    public PrivateShopValue PrivateShop { get; }
    public FatiguePointsValue FatiguePoints { get; }
    public RankValue Rank { get; }
    public bool BattlePose { get; }
    public int Status { get; }
    public StatusEffectValueEntry[] StatusEffects { get; }
}
