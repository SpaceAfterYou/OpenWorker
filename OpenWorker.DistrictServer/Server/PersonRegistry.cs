using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.League.Components;
using OpenWorker.Hotspot.Modules.Maze.Components;
using OpenWorker.Hotspot.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Skill.Components;

namespace OpenWorker.DistrictServer.Server;

enum eSHAPE_EQUIP_SLOT : uint
{
    E_SHAPE_EQUIP_SLOT_NONE = 0xFFFFFFFF,
    E_SHAPE_EQUIP_SLOT_HAIR_ACC = 0x0,
    E_SHAPE_EQUIP_SLOT_HAIR_CAP = 0x1,
    E_SHAPE_EQUIP_SLOT_FACE_UPPER = 0x2,
    E_SHAPE_EQUIP_SLOT_FACE_LOWER = 0x3,
    E_SHAPE_EQUIP_SLOT_GLOVE = 0x4,
    E_SHAPE_EQUIP_SLOT_UNDERWEAR = 0x5,
    E_SHAPE_EQUIP_SLOT_COSTUME = 0x6,
    E_SHAPE_EQUIP_SLOT_BACK = 0x7,
    E_SHAPE_EQUIP_SLOT_SOCKS = 0x8,
    E_SHAPE_EQUIP_SLOT_SHOES = 0x9,
    E_SHAPE_EQUIP_SLOT_WEAPON_SKIN = 0xA,
    E_SHAPE_EQUIP_SLOT_PANTS = 0xB,
    E_SHAPE_EQUIP_SLOT_TAIL = 0xC,
    E_SHAPE_EQUIP_SLOT_EFFECT = 0xD,
    E_SHAPE_EQUIP_SLOT_MAX = 0xE,
};

enum eSTOAGE_TYPE : int
{
    E_STORAGE_TYPE_COMMON = 0x0,
    E_STORAGE_TYPE_COSTUME = 0x1,
    E_STORAGE_TYPE_CASH = 0x2,
    E_STORAGE_TYPE_MAX = 0x3,
};

public sealed class PersonRegistry(World world, StorageFactory factory, StorageManager storageManager, StorageItemFactory itemFactory)
{
    private ComponentType[] StorageArchetype { get; } =
    [
        Component.GetComponentType(typeof(StorageContentComponent)),
        Component.GetComponentType(typeof(StorageGroupComponent))
    ];
        
    private ComponentType[] ItemArchetype { get; } =
    [
        Component.GetComponentType(typeof(StorageItemComponent)),
        Component.GetComponentType(typeof(StorageItemGradeComponent))
    ];

    internal void PullPerson(Entity entity, PersonPersistent persistent)
    {
        world.Set(entity, new ActorComponent
        {
            Identifier = persistent.Id,
            Type = ActorType.User
        });
        
        world.Set(entity, new AbilityComponent
        {
            Health = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SoulGain = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SoulVapor = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            Stamina = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SuperArmor = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            Speed = new SpeedComponentEntry
            {
                Attack = 100.0f,
                Move = 100.0f
            }
        });
        
        world.Set(entity, new AppearanceComponent
        {
            HairStyle = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultHairStyle,
                Look = persistent.EquippedHairStyle
            },
            HairColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultHairColor,
                Look = persistent.EquippedHairColor
            },
            EyeColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultEyeColor,
                Look = persistent.EquippedEyeColor
            },
            SkinColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultSkinColor,
                Look = persistent.EquippedSkinColor
            }
        });
        
        world.Set(entity, new FatiguePointsComponent
        {
            Common = 100,
            Bonus = 100
        });
        
        world.Set(entity, new LeagueComponent
        {
            Id = 0,
            Name = string.Empty,
            Card = new LeagueComponentCard
            {
                Border = 0,
                Emblem = 0
            }
        });
        
        world.Set(entity, new PersonInfoComponent
        {
            Name = persistent.Name,
            Hero = persistent.Hero
        });
        
        world.Set(entity, new ShopPrivateComponent
        {
            Name = string.Empty,
            Type = 0
        });
        
        world.Set(entity, new RankComponent
        {
            Level = 0,
            Experience = 0
        });

        world.Set(entity, new TitleComponent
        {
            Primary = 0,
            Secondary = 0
        });
        
        world.Set(entity, persistent.ToPersonOptionComponent());
        
        world.Set(entity, new StorageComponent([
            factory.Create(StorageGroup.Common),
            factory.Create(StorageGroup.Costume),
            factory.Create(StorageGroup.Cash),
            
            factory.Create(StorageGroup.CommonStorage),
            factory.Create(StorageGroup.CostumeStorage),
            
            CreateGearStorage(persistent.Hero), 
            CreatesEquipStorage()
        ]));
        
        storageManager.TryAdd(entity, itemFactory.Create(715146322, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(820900502, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(251020601, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(251050901, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(253120701, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(253040901, 1), 1);
        
        storageManager.TryAdd(entity, itemFactory.Create(837000001, 1), 1);
        storageManager.TryAdd(entity, itemFactory.Create(837000001, 3), 3);
        
        storageManager.TryAdd(entity, itemFactory.Create(710105001, 50), 50);
        storageManager.TryAdd(entity, itemFactory.Create(710105002, 50), 50);
        storageManager.TryAdd(entity, itemFactory.Create(710105003, 50), 50);
        storageManager.TryAdd(entity, itemFactory.Create(710105004, 50), 50);
        storageManager.TryAdd(entity, itemFactory.Create(710105005, 50), 50);
        storageManager.TryAdd(entity, itemFactory.Create(710105006, 50), 50);
    }
    
    private Entity CreateGearStorage(Hero hero)
    {
        var storage = factory.Create(StorageGroup.AbilityEquip); 
        
        var content = world.Get<StorageContentComponent>(storage);
        
        var weaponIdentifier = new[]
        {
            /* 0 */ 0,
            /* 1 */ 111000001,
            /* 2 */ 112000001,
            /* 3 */ 113000001,
            /* 4 */ 114000001,
            /* 5 */ 115000001,
            /* 6 */ 116000001
        };
        
        content[AbilityEquipSlot.SoulWeapon] = world.Create(
            new StorageItemComponent(weaponIdentifier[(int)hero]), 
            new StorageItemGradeComponent(9)
        );

        return storage;
    }
    
    private Entity CreatesEquipStorage()
    {
        return factory.Create(StorageGroup.ShapeEquip);
    }
}