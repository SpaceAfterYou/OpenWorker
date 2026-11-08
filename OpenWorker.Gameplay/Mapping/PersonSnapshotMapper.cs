using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using GameplayWorld = OpenWorker.Gameplay.Messages.Response.Person;
using HotspotWorld = OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Gameplay.Mapping;

public static class PersonSnapshotMapper
{
    public static CharacterInfoResponse CreateCharacterInfoResponse(World world, Entity player)
    {
        return new CharacterInfoResponse
        {
            Person = CreatePersonValue(world, player, player),
            World = CreateWorldValue(world, player),
            Gate = CreateCharacterInfoGatePayload(world, player)
        };
    }

    public static PersonValue CreatePersonValue(World world, Entity accountEntity, Entity personEntity)
    {
        var inventory = world.Get<InventoryComponent>(personEntity).Value;
        var actor = (ActorValue)world.Get<ActorComponent>(personEntity);
        var personInfo = world.Get<PersonInfoComponent>(personEntity);
        var appearance = world.Get<AppearanceComponent>(personEntity);

        var infoValue = new PersonInfoValue(
            personInfo.Name,
            personInfo.Hero,
            new AppearanceValue(
                appearance.HairStyle.Shape,
                appearance.HairColor.Shape,
                appearance.EyeColor.Shape,
                appearance.SkinColor.Shape),
            new AppearanceValue(
                appearance.HairStyle.Look,
                appearance.HairColor.Look,
                appearance.EyeColor.Look,
                appearance.SkinColor.Look));

        var account = world.Get<ClaimsComponent>(accountEntity).Account;

        var gear = inventory?.Find(StorageGroup.AbilityEquip);
        var shape = inventory?.Find(StorageGroup.ShapeEquip);

        static EquipItemValueEntry ItemEntry(ItemInstance? item) =>
            item is null ? new EquipItemValueEntry(-1, 0) : new EquipItemValueEntry(item.Item, item.Upgrade);

        var shapeItems = Enumerable
            .Range(0, ItemModuleDefines.EquippedItemCount)
            .Select(i => ItemEntry(shape is not null && i < shape.Size ? shape.Slots[i] : null))
            .ToArray();

        var league = world.Get<LeagueComponent>(personEntity);

        return new PersonValue(
            actor,
            infoValue,
            account,
            world.Get<LevelComponent>(personEntity).Level,
            FactionGroup.None,
            StuffLevel.GameMaster,
            0,
            ItemEntry(gear?.Slots[(int)AbilityEquipSlot.SoulWeapon]),
            ItemEntry(gear?.Slots[(int)AbilityEquipSlot.SubWeapon]),
            shapeItems,
            new TitleValue(world.Get<TitleComponent>(personEntity).Primary, world.Get<TitleComponent>(personEntity).Secondary),
            new LeagueValue(league.Id, league.Name, new PersonLeagueCardValueEntry(league.Card.Emblem, league.Card.Border)),
            FromAbility(world.Get<AbilityComponent>(personEntity)),
            new PrivateShopValue(world.Get<ShopPrivateComponent>(personEntity).Type, world.Get<ShopPrivateComponent>(personEntity).Name),
            new FatiguePointsValue(world.Get<FatiguePointsComponent>(personEntity).Common, world.Get<FatiguePointsComponent>(personEntity).Bonus),
            new RankValue(world.Get<RankComponent>(personEntity).Level, world.Get<RankComponent>(personEntity).Experience),
            false,
            0,
            []);
    }

    public static HotspotWorld.WorldValue CreateWorldValue(World world, Entity personEntity)
    {
        var w = world.Get<GameplayWorld.WorldComponent>(personEntity);
        return new HotspotWorld.WorldValue(w.Location, w.Map, w.Position, w.Rotation);
    }

    public static CharacterInfoGatePayload CreateCharacterInfoGatePayload(World world, Entity player)
    {
        var currency = world.Get<CurrencyComponent>(player);
        var worldComponent = world.Get<GameplayWorld.WorldComponent>(player);
        var worldType = worldComponent.Location < 20_000 ? WorldType.District : WorldType.Maze;

        return new CharacterInfoGatePayload
        {
            PlaceholderInt = 0,
            Gold = currency.Gold,
            CommonStep = byte.MinValue,
            ConsumeStep = byte.MinValue,
            CostumeStep = byte.MinValue,
            CardStep = byte.MinValue,
            UserDb = uint.MinValue,
            SyncUser = uint.MinValue,
            BattlePoint = currency.BattlePoint,
            Ether = currency.Ether,
            FriendPoint = 17_000,
            AccountId = "",
            NetCafe = false,
            ClassScene = false,
            WorldType = worldType,
            UsePvpDistrict = false
        };
    }

    public static CharacterListResponse CreateCharacterListResponse(World world, Entity gateEntity)
    {
        var c = world.Get<GatePersonComponent>(gateEntity);

        var persons = c.SlotList
            .Where(e => Entity.Null != e)
            .Select(p => CreatePersonValue(world, gateEntity, p))
            .ToList();

        var prot = new ProtectionStateValue
        {
            SecondPassword = world.Get<SecondPasswordComponent>(gateEntity).State,
            TradePassword = world.Get<TradePasswordComponent>(gateEntity).Has
                ? PasswordProtectionState.UnAuthenticated
                : PasswordProtectionState.None
        };

        return new CharacterListResponse
        {
            Persons = persons,
            LastPerson = c.HasLast && c.Last != Entity.Null ? world.Get<ActorComponent>(c.Last).Identifier : 0,
            Protection = prot
        };
    }

    private static AbilityValue FromAbility(AbilityComponent component)
    {
        return new AbilityValue(
            new AbilityValueEntry(component.Health.Current, component.Health.Max),
            new AbilityValueEntry(component.SoulGain.Current, component.SoulGain.Max),
            new AbilityValueEntry(component.SoulVapor.Current, component.SoulVapor.Max),
            new AbilityValueEntry(component.Stamina.Current, component.Stamina.Max),
            new AbilityValueEntry(component.SuperArmor.Current, component.SuperArmor.Max),
            new SpeedValueEntry(component.Speed.Move, component.Speed.Attack));
    }
}
