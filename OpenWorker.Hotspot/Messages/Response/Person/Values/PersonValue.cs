using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.League.Components;
using OpenWorker.Hotspot.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Maze.Components;
using OpenWorker.Hotspot.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PersonValue
{
    public PersonValue(Entity entity, Entity person)
    {
        var storage = person.Get<StorageComponent>();

        Actor = person.Get<ActorComponent>();

        InfoValue = new PersonInfoValue(
            person.Get<PersonInfoComponent>(),
            person.Get<AppearanceComponent>()
        );

        Level = 25;
        Faction = FactionGroup.None;
        Account = entity.Get<ClaimsComponent>().Account;
        StuffLevel = StuffLevel.GameMaster;
        PvPKillCount = 0;

        var gear = storage.Collection
            .First(x => x.Get<StorageGroupComponent>().Group == StorageGroup.AbilityEquip)
            .Get<StorageContentComponent>();
        
        PrimaryWeapon = new EquipItemValueEntry(gear[0]);
        SecondaryWeapon = new EquipItemValueEntry(gear[1]);

        EquipItems = storage.Collection
            .First(x => x.Get<StorageGroupComponent>().Group == StorageGroup.ShapeEquip)
            .Get<StorageContentComponent>().SlotList
            .Select(x => new EquipItemValueEntry(x))
            .ToArray();

        Title = new TitleValue(person.Get<TitleComponent>());
        League = new LeagueValue(person.Get<LeagueComponent>());
        Ability = new AbilityValue(person.Get<AbilityComponent>());
        PrivateShop = new PrivateShopValue(person.Get<ShopPrivateComponent>());
        FatiguePoints = new FatiguePointsValue(person.Get<FatiguePointsComponent>());
        Rank = new RankValue(person.Get<RankComponent>());
        BattlePose = false;
        Status = 0;
        StatusEffects = [];
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
    public byte Level { get; } = 1;
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
