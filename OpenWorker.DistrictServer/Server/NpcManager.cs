using System.Collections.ObjectModel;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Server;

public sealed class NpcManager
{
    private short Location { get; } 
    
    public NpcManager(World ecs, BatchManager provider, ReadOnlyCollection<NPCRow> npcList, IConfiguration configuration)
    {
        Location = configuration.GetLocation();
        
        var id = 0;
        
        if (!provider.TryGetAndCache(Location, out var batch, out var type) || type != BatchType.District)
        {
            return;
        }
        
        Collection = batch.EventBox.MonsterSpawns
            .Where(e => e.CreationCondition == CreationConditionType.Immediate)
            .SelectMany(spawn => spawn.MonsterList.Where(e => e.Type == MonsterSpawnType.Npc).Select(monster =>
            {
                var entity = ecs.Create();

                var row = npcList.First(e => e.Id == monster.Id);

                var npc = new CreatureComponent___Old(
                    monster.Id,
                    spawn.GetPosition(),
                    spawn.Rotation,
                    100,
                    row.Level,
                    spawn.Waypoint,
                    spawn.Sector);

                entity.Add(new ActorComponent
                {
                    Identifier = ++id,
                    Type = ActorType.Npc
                });
                entity.Add(npc);

                return entity;
            }))
            .ToArray();
    }

    public Entity[] Collection { get; } = [];
}