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

namespace OpenWorker.Lua.Managers;

public sealed class LuaCreatureManager(World ecs, VBatchFile batch)
{
    private static ComponentType[] Archetype { get; } = [
        ComponentRegistry.Add<ActorComponent>(),
        ComponentRegistry.Add<CreatureComponent>(),
        ComponentRegistry.Add<NonPlayableCreatureComponent>(),
        ComponentRegistry.Add<WorldComponent>(),
    ];

    private int Offset { get; set; } = 1;
    
    private List<Entity> Creatures { get; } = [];

    public void Create(Entity player, int box)
    {
        var spawn = batch.EventBox.MonsterSpawns.First(x => x.Id == box);
        Console.WriteLine($"[{nameof(Create)}]: {spawn.LayerBitmask}");
        
        var monsters = spawn.MonsterList
            .Where(x => x.Id is not 0)
            .Select(monster =>
            {
                Console.WriteLine($"[{nameof(Create)}]: spawn - {monster.Id} / type - {monster.Type} / chance - {monster.Chance}");

                return monster.Type switch
                {
                    MonsterSpawnType.Npc => CreateNpc(monster.Id, spawn),
                    MonsterSpawnType.Monster => CreateMonster(monster.Id, spawn),
                    _ => throw new NotImplementedException($"[{nameof(Create)}]: unknown type - {monster.Type}")
                };
            })
            .ToArray();
        
        var session = ecs.Get<ServerSessionComponent>(player);

        switch (spawn.CreationCondition)
        {
            case CreationConditionType.Immediate:
                session.Send(new WorldOtherInfosMonsterResponse
                {
                    Monsters = monsters.Select(e => MonsterSnapshotMapper.ToStMonsterInfo(ecs, e)).ToArray()
                });
                break;
                
            case CreationConditionType.WaitSignal:
                Task
                    .Delay(TimeSpan.FromSeconds(spawn.WaitCreationDelayTime))
                    .ContinueWith(x => session.Send(new WorldOtherInfosMonsterResponse
                    {
                        Monsters = monsters.Select(e => MonsterSnapshotMapper.ToStMonsterInfo(ecs, e)).ToArray()
                    }));
                break;
        }

        
    }

    private void Create(Entity creature, int identifier, MonsterSpawnBox spawn)
    {
        ecs.Set(creature, new CreatureComponent { Identifier = identifier });
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

    public void Emplace(Entity creature)
    {
        ecs.Set(creature, new ActorComponent { Identifier = Offset++, Type = ActorType.User });
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