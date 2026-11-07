using OpenWorker.Hotspot;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Login.Extensions;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;

namespace OpenWorker.GateServer.Gameplay;

public sealed class PersonRegistry(World world, StorageFactory factory, CostumeFactory costumes) : ISessionCloseHandler
{
    private ComponentType[] PersonArchetype { get; } =
    [
        Component.GetComponentType(typeof(ActorComponent)),
        Component.GetComponentType(typeof(AbilityComponent)),
        Component.GetComponentType(typeof(AppearanceComponent)),
        Component.GetComponentType(typeof(FatiguePointsComponent)),
        Component.GetComponentType(typeof(LeagueComponent)),
        Component.GetComponentType(typeof(PersonInfoComponent)),
        Component.GetComponentType(typeof(ShopPrivateComponent)),
        Component.GetComponentType(typeof(RankComponent)),
        Component.GetComponentType(typeof(TitleComponent)),
        Component.GetComponentType(typeof(StorageComponent)),
        Component.GetComponentType(typeof(PersonOptionComponent)),
        Component.GetComponentType(typeof(WorldComponent)),
    ];
    
    internal GatePersonComponent CreatePersonListComponent(IReadOnlyCollection<PersonPersistent> collection)
    {
        // The client places characters by their position in the list, so it must not change between visits.
        var persons = collection
            .OrderBy(e => e.Id)
            .Take(LoginModuleDefines.PersonSlotCount)
            .ToArray();

        var slots = persons
            .Select(CreatePerson)
            .Concat(Enumerable.Repeat(Entity.Null, LoginModuleDefines.PersonSlotCount))
            .Take(LoginModuleDefines.PersonSlotCount)
            .ToArray();

        var last = Array.FindIndex(
            persons,
            e => e.LastSelectedAt is not null && e.LastSelectedAt == persons.Max(p => p.LastSelectedAt));

        return new GatePersonComponent
        {
            SlotList = slots,
            LastIndex = last
        };
    }

    internal void RemovePerson(Entity entity, int person)
    {
        var component = world.Get<GatePersonComponent>(entity);

        var slot = Array.FindIndex(
            component.SlotList,
            e => e != Entity.Null && world.Get<ActorComponent>(e).Identifier == person);

        if (slot < 0)
        {
            return;
        }

        DestroyPerson(component.SlotList[slot]);

        component.SlotList[slot] = Entity.Null;

        if (component.LastIndex == slot)
        {
            world.Set(entity, component with { LastIndex = -1 });
        }
    }

    internal void MarkLastPerson(Entity entity, int person)
    {
        var component = world.Get<GatePersonComponent>(entity);

        var slot = Array.FindIndex(
            component.SlotList,
            e => e != Entity.Null && world.Get<ActorComponent>(e).Identifier == person);

        world.Set(entity, component with { LastIndex = slot });
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (world.Has<GatePersonComponent>(player))
        {
            foreach (var person in world.Get<GatePersonComponent>(player).SlotList ?? [])
            {
                DestroyPerson(person);
            }
        }

        return ValueTask.CompletedTask;
    }

    private void DestroyPerson(Entity person)
    {
        if (person == Entity.Null || !world.IsAlive(person))
        {
            return;
        }

        foreach (var storage in world.Get<StorageComponent>(person).Collection)
        {
            foreach (var item in world.Get<StorageContentComponent>(storage).SlotList)
            {
                if (item != Entity.Null && world.IsAlive(item))
                {
                    world.Destroy(item);
                }
            }

            world.Destroy(storage);
        }

        world.Destroy(person);
    }

    internal int FindFreeSlotIndex(Entity entity)
    {
        var component = world.Get<GatePersonComponent>(entity);
        
        return Array.FindIndex(component.SlotList, e => e == Entity.Null);
    }
    
    internal void CreatePerson(Entity entity, int slotIndex, PersonPersistent persistent)
    {
        var component = world.Get<GatePersonComponent>(entity);
        
        component.SlotList[slotIndex] = CreatePerson(persistent);
        
        world.Set(entity, component with { LastIndex = slotIndex });
    }

    private Entity CreatePerson(PersonPersistent persistent)
    {
        var entity = world.Create(PersonArchetype);
        
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
        
        world.Set(entity, new WorldComponent
        {
            Location = persistent.Location,
            Position = new Vector3(persistent.PositionX, persistent.PositionY, persistent.PositionZ),
            Rotation = persistent.RotationX,
            Jump = persistent.Location * 100 + 1
        });
        
        world.Set(entity, new StorageComponent([CreateGearStorage(persistent.Hero), CreatesEquipStorage(persistent.Outfit)]));

        world.Set(entity, persistent.ToPersonOptionComponent());
        
        return entity;
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
            new StorageItemGradeComponent(0)
        );
        
        return storage;
    }
    
    private Entity CreatesEquipStorage(int outfit)
    {
        var storage = factory.Create(StorageGroup.ShapeEquip);

        costumes.Dress(storage, outfit);

        return storage;
    }
}
