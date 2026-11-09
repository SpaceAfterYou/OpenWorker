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

    /// <summary>
    /// False when the buff table has no such buff, which a script can ask for.
    /// </summary>
    public bool Apply(Entity player, ActorComponent actor, int buff)
    {
        var item = buffs.FirstOrDefault(x => x.Id == buff);

        // The row of a buff that is not in the table stays empty.
        if (item.Id == 0)
        {
            return false;
        }

        var session = world.Get<ServerSessionComponent>(player);

        session.Send(new SkillUpdateBuffResponse
        {
            Target = actor,
            Buff = (short)buff,
            Time = (float)(item.DurationMilliseconds * 0.001),
            Count = 1,
            Owner = 0
        });

        return true;
    }
}
