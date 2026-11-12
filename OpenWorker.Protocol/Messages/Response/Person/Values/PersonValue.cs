using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PersonValue : IWireReadable<PersonValue>, IWireWritable
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
        this = WireBridge.Read<PersonValue>(reader);
    }

    /// <summary>
    /// A character as the client sends it to be made, and as the bot of the load test reads it from the list: in the order
    /// of the wire, the names counted in bytes, the worn items by their identifiers alone.
    /// </summary>
    public static PersonValue Read(ref PacketReader reader)
    {
        var actor = ActorValue.Read(ref reader);
        var info = PersonInfoValue.Read(ref reader);
        var level = reader.ReadByte();
        var faction = (FactionGroup)reader.ReadByte();
        var account = reader.ReadInt32();
        var stuffLevel = (StuffLevel)reader.ReadByte();
        var pvpKillCount = reader.ReadInt32();
        var primaryWeapon = EquipItemValueEntry.Read(ref reader);
        var secondaryWeapon = EquipItemValueEntry.Read(ref reader);

        var equipItems = new EquipItemValueEntry[ItemModuleDefines.EquippedItemCount];

        for (var index = 0; index < equipItems.Length; index++)
        {
            equipItems[index] = new EquipItemValueEntry(reader.ReadInt32());
        }

        var title = TitleValue.Read(ref reader);
        var league = LeagueValue.Read(ref reader);
        var ability = AbilityValue.Read(ref reader);
        var privateShop = PrivateShopValue.Read(ref reader);
        var fatiguePoints = FatiguePointsValue.Read(ref reader);
        var rank = RankValue.Read(ref reader);
        var battlePose = reader.ReadBoolean();
        var status = reader.ReadInt32();

        var statusEffects = new StatusEffectValueEntry[reader.ReadLength(WireLength.Byte, StatusEffectSize)];

        for (var index = 0; index < statusEffects.Length; index++)
        {
            statusEffects[index] = StatusEffectValueEntry.Read(ref reader);
        }

        return new PersonValue(actor, info, account, level, faction, stuffLevel, pvpKillCount, primaryWeapon, secondaryWeapon,
            equipItems, title, league, ability, privateShop, fatiguePoints, rank, battlePose, status, statusEffects);
    }

    /// <summary>
    /// The bytes of a status effect: id, time, count, owner.
    /// </summary>
    private const int StatusEffectSize = sizeof(int) + sizeof(float) + sizeof(byte) + sizeof(int);

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

    /// <summary>
    /// In the order the client reads, which is not the one of the properties, and the worn items by their identifiers alone:
    /// not declarative, so written by hand.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Actor);
        InfoValue.Write(ref writer);
        writer.WriteByte(Level);
        writer.WriteByte((byte)Faction);
        writer.WriteInt32(Account);
        writer.WriteByte((byte)StuffLevel);
        writer.WriteInt32(PvPKillCount);
        PrimaryWeapon.Write(ref writer);
        SecondaryWeapon.Write(ref writer);

        // The worn items without their upgrades, one for every slot.
        foreach (var item in EquipItems ?? [])
        {
            writer.WriteInt32(item.Id);
        }

        Title.Write(ref writer);
        League.Write(ref writer);
        Ability.Write(ref writer);
        PrivateShop.Write(ref writer);
        FatiguePoints.Write(ref writer);
        Rank.Write(ref writer);
        writer.WriteBoolean(BattlePose);
        writer.WriteInt32(Status);
        writer.WriteLength(WireLength.Byte, StatusEffects?.Length ?? 0);

        foreach (var effect in StatusEffects ?? [])
        {
            effect.Write(ref writer);
        }
    }
}
