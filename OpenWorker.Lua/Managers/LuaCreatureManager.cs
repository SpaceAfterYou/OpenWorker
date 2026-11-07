using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.World.Responses;

using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Lua.Managers;

public sealed class LuaCreatureManager(World ecs, VBatchFile batch)
{
    private static ComponentType[] Archetype { get; } = [
        ComponentRegistry.Add<ActorComponent>(),
        ComponentRegistry.Add<CreatureComponent>(),
        ComponentRegistry.Add<NonPlayableCreatureComponent>(),
        ComponentRegistry.Add<WorldComponent>(),
    ];

    private const int StartHealth = 250;

    private int Offset { get; set; } = 1;
    private HashSet<int> SpawnedBoxes { get; } = [];
    private HashSet<Entity> Scenery { get; } = [];
    private bool Closed { get; set; }
    
    private List<Entity> Creatures { get; } = [];

    public void Create(Entity player, int box)
    {
        var spawn = batch.EventBox.MonsterSpawns.First(x => x.Id == box);
        Console.WriteLine($"[{nameof(Create)}]: {spawn.LayerBitmask}");
        
        var created = new List<Entity>();

        foreach (var monster in spawn.MonsterList.Where(x => x.Id is not 0))
        {
            switch (monster.Type)
            {
                case MonsterSpawnType.Npc:
                    created.Add(CreateNpc(monster.Id, spawn));
                    break;

                case MonsterSpawnType.Monster:
                    created.Add(CreateMonster(monster.Id, spawn));
                    break;

                case MonsterSpawnType.DestructionObject or MonsterSpawnType.TreasureBox or MonsterSpawnType.Unit:
                    var scenery = CreateMonster(monster.Id, spawn);
                    Scenery.Add(scenery);
                    created.Add(scenery);
                    break;

                default:
                    // One kind of object the server cannot show must not stop the rest of the map.
                    Console.WriteLine($"[{nameof(Create)}]: skipped {monster.Id}, type {monster.Type}");
                    break;
            }
        }

        var session = ecs.Get<ServerSessionComponent>(player);

        switch (spawn.CreationCondition)
        {
            case CreationConditionType.Immediate:
                Show(session, created);
                break;

            case CreationConditionType.WaitSignal:
                _ = ShowLaterAsync(session, created, TimeSpan.FromSeconds(spawn.WaitCreationDelayTime));
                break;
        }
    }

    private void Show(ServerSessionComponent session, List<Entity> created)
    {
        var npcs = created.Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Npc).ToArray();
        var monsters = created.Except(npcs).ToArray();

        if (monsters.Length > 0)
        {
            session.Send(new WorldOtherInfosMonsterResponse
            {
                Monsters = monsters.Select(e => MonsterSnapshotMapper.ToStMonsterInfo(ecs, e)).ToArray()
            });
        }

        if (npcs.Length > 0)
        {
            session.Send(new WorldOtherInfosNpcResponse { List = npcs.Select(ToNpcEntry).ToArray() });
        }
    }

    private WorldNpcListEntry ToNpcEntry(Entity npc)
    {
        var creature = ecs.Get<CreatureComponent>(npc);
        var place = ecs.Get<WorldComponent>(npc);
        var nonPlayable = ecs.Get<NonPlayableCreatureComponent>(npc);

        return new WorldNpcListEntry(
            ecs.Get<ActorComponent>(npc),
            place.Position,
            place.Rotation,
            creature.Health,
            nonPlayable.Waypoint,
            nonPlayable.Sector,
            creature.Level,
            creature.Identifier);
    }

    private void Create(Entity creature, int identifier, MonsterSpawnBox spawn)
    {
        ecs.Set(creature, new CreatureComponent { Identifier = identifier, Health = StartHealth });
        ecs.Set(creature, new NonPlayableCreatureComponent { SpawnBox = spawn.Id, Sector = spawn.Sector, Waypoint = spawn.Waypoint });
        ecs.Set(creature, new WorldComponent { Location = 0, Position = spawn.GetPosition(), Rotation = spawn.Rotation, Jump = 0 });
    }
    
    public Entity CreateMonster(int identifier, MonsterSpawnBox spawn)
    {
        var creature = ecs.Create(Archetype);
        
        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.Monster });
        
        Create(creature, identifier, spawn);
        
        Creatures.Add(creature);
        
        return creature;
    }

    public Entity CreateNpc(int identifier, MonsterSpawnBox spawn)
    {
        var creature = ecs.Create(Archetype);
        
        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.Npc });

        Create(creature, identifier, spawn);
        
        Creatures.Add(creature);
        
        return creature;
    }

    /// <summary>
    /// The maze is over: what it has spawned goes with it.
    /// </summary>
    /// <summary>
    /// Spawns the box the first time a sector asks for it: entering a sector again must not repeat it.
    /// </summary>
    public void SpawnOnce(Entity player, int box)
    {
        if (SpawnedBoxes.Add(box))
        {
            Create(player, box);
        }
    }

    public Entity[] FindMonstersNear(Vector3 position, float radius)
    {
        return Creatures
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster)
            .Where(e => Vector2.Distance(
                new Vector2(position.X, position.Y),
                new Vector2(ecs.Get<WorldComponent>(e).Position.X, ecs.Get<WorldComponent>(e).Position.Y)) <= radius)
            .ToArray();
    }

    public bool HasMonstersInSector(int sector)
    {
        // Objects that can be broken do not hold a sector back.
        return Creatures.Any(e =>
            !Scenery.Contains(e) &&
            ecs.Get<ActorComponent>(e).Type == ActorType.Monster &&
            ecs.Get<NonPlayableCreatureComponent>(e).Sector == sector);
    }

    public void Remove(Entity creature)
    {
        Creatures.Remove(creature);
        Scenery.Remove(creature);

        ecs.Destroy(creature);
    }

    public void Close()
    {
        Closed = true;

        foreach (var creature in Creatures.Where(ecs.IsAlive))
        {
            ecs.Destroy(creature);
        }

        Creatures.Clear();
        Scenery.Clear();
    }

    private async Task ShowLaterAsync(ServerSessionComponent session, List<Entity> created, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay);

            if (!Closed)
            {
                Show(session, created.Where(ecs.IsAlive).ToList());
            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"[{nameof(ShowLaterAsync)}]: {e}");
        }
    }

    public void MoveToPoint(int npc, int waypoint)
    {
        var creature = Creatures.FirstOrDefault(x => ecs.Get<CreatureComponent>(x) == npc);
        
        if (creature.IsAlive() is false)
        {
            Console.WriteLine($"[{nameof(MoveToPoint)}]: creature not found - {npc}");
        }
    }
}