using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// What the others see of a skill a player starts (06/10): the record the client sent, with the actor of the player, the
/// place the server took for the skill and the divergence the server worked out. The player gets the answer 06/09 only;
/// the client plays its own skill when it asks for it.
/// </summary>
public static class PlayerSkillBroadcast
{
    public static SkillActiveBroadcastResponse Started(World world, Entity player, ActiveSkillValue skill, SkillPositionValue position, int divergence)
    {
        return new SkillActiveBroadcastResponse
        {
            Skill = skill with
            {
                Actor = world.Get<ActorComponent>(player),
                Unknown = 0,
                Position = position,
                Divergence = divergence
            }
        };
    }
}
