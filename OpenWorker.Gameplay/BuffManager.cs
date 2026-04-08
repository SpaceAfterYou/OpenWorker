using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay;

public sealed class BuffManager(World world, ReadOnlyCollection<BuffRow> buffs)
{
    private Dictionary<Entity, List<int>> Buffs { get; } = [];

    public bool Apply(Entity player, ActorComponent actor, int buff)
    {
        var session = world.Get<ServerSessionComponent>(player);

        var item = buffs.First(x => x.Id == buff);

        session.Send(new SkillUpdateBuffResponse
        {
            Target = actor,
            Buff = (short)buff,
            Time = (float)(item.Field7 * 0.001),
            Count = 1,
            Owner = 0
        });

        return true;
    }
}
