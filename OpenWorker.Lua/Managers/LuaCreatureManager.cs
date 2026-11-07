using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.World.Responses;

using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Lua.Managers;

public sealed class LuaCreatureManager(World ecs, VBatchFile batch, CombatTables tables)
{
    private static ComponentType[] Archetype { get; } = [
        ComponentRegistry.Add<ActorComponent>(),
        ComponentRegistry.Add<CreatureComponent>(),
        ComponentRegistry.Add<NonPlayableCreatureComponent>(),
        ComponentRegistry.Add<WorldComponent>(),
        ComponentRegistry.Add<MonsterCombatComponent>(),
    ];

    private const int NpcHealth = 250;

    private int Offset { get; set; } = 1;
    private HashSet<int> SpawnedBoxes { get; } = [];
    private HashSet<Entity> Scenery { get; } = [];
    private bool Closed { get; set; }
    
    private List<Entity> Creatures { get; } = [];
    private Dictionary<int, SectorKillProgress> KillProgress { get; } = [];

    public void Create(Entity player, int box)
    {
        var spawn = batch.EventBox.MonsterSpawns.First(x => x.Id == box);

        SpawnedBoxes.Add(box);

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
        ecs.Set(creature, new CreatureComponent { Identifier = identifier, Health = NpcHealth });
        ecs.Set(creature, new NonPlayableCreatureComponent { SpawnBox = spawn.Id, Sector = spawn.Sector, Waypoint = spawn.Waypoint });
        ecs.Set(creature, new WorldComponent { Location = 0, Position = spawn.GetPosition(), Rotation = spawn.Rotation, Jump = 0 });
    }
    
    public Entity CreateMonster(int identifier, MonsterSpawnBox spawn)
    {
        var creature = ecs.Create(Archetype);
        
        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.Monster });
        
        Create(creature, identifier, spawn);
        
        var combat = tables.GetMonster(identifier);

        ecs.Set(creature, combat);
        ecs.Set(creature, ecs.Get<CreatureComponent>(creature) with { Level = combat.Stats.Level, Health = (int)combat.Stats.MaxHealth });

        if (combat.HoldsSector)
        {
            GetKillProgress(spawn.Sector).Added();
        }

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

    public Entity[] FindMonsters(Func<Vector3, MonsterCombatComponent, bool> touches)
    {
        return Creatures
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster)
            .Where(e => touches(ecs.Get<WorldComponent>(e).Position, ecs.Get<MonsterCombatComponent>(e)))
            .ToArray();
    }

    public Entity? FindMonster(int identifier)
    {
        foreach (var creature in Creatures)
        {
            var actor = ecs.Get<ActorComponent>(creature);

            if (actor.Type == ActorType.Monster && actor.Identifier == identifier)
            {
                return creature;
            }
        }

        return null;
    }

    public FlightTarget[] GetFlightTargets()
    {
        return Creatures
            .Where(e => ecs.Get<ActorComponent>(e).Type == ActorType.Monster)
            .Select(e => new FlightTarget(ecs.Get<ActorComponent>(e).Identifier, ecs.Get<WorldComponent>(e).Position, ecs.Get<MonsterCombatComponent>(e).BodyRadius, ecs.Get<MonsterCombatComponent>(e).BodyHeight))
            .ToArray();
    }

    public SectorKillProgress GetKillProgress(int sector)
    {
        if (!KillProgress.TryGetValue(sector, out var progress))
        {
            var box = batch.EventBox.Sectors.FirstOrDefault(e => e.Id == sector);

            progress = box is null ? new SectorKillProgress(ClearType.None, 0, []) : new SectorKillProgress(box);

            KillProgress.Add(sector, progress);
        }

        return progress;
    }

    /// <summary>
    /// Boxes of a sector that have not appeared yet and whose monsters have to die with the rest.
    /// </summary>
    public int CountBoxesToCome(int sector)
    {
        return batch.EventBox.MonsterSpawns.Count(e => e.Sector == sector && !SpawnedBoxes.Contains(e.Id) && HoldsSector(e));
    }

    private bool HoldsSector(MonsterSpawnBox box)
    {
        return box.MonsterList
            .TakeWhile(e => e.Id is not 0)
            .All(e => e.Type is not MonsterSpawnType.Npc && tables.HoldsSector(e.Id));
    }

    public void Remove(Entity creature)
    {
        if (ecs.Get<ActorComponent>(creature).Type == ActorType.Monster && ecs.Get<MonsterCombatComponent>(creature).HoldsSector)
        {
            GetKillProgress(ecs.Get<NonPlayableCreatureComponent>(creature).Sector).Removed();
        }

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