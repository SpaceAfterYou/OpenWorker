using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot;

public sealed class BuffManager(World world, ReadOnlyCollection<BuffRow> buffs)
{
    private Dictionary<Entity, List<int>> Buffs { get; } = [];
    
    public bool Apply(Entity player, ActorComponent actor, int buff)
    {
        var session = world.Get<ServerSessionComponent>(player);

        var item = buffs.First(x => x.Id == buff);
        
        session.Send(new SkillUpdateBuffResponse
        {
            Actor = actor,
            Buff = (short)buff,
            Time = (float)(item.Field7 * 0.001),
            Count = 1,
            Owner = 0
        });
        
        return true;
    }
}