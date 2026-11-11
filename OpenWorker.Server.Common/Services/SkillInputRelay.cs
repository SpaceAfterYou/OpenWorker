using System.Numerics;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Skill.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// What the others see of the input of a player's skill: the key it holds (06/42), the target it picks (06/44), the sub
/// animation it asks for (06/4B), the charging it starts and ends (06/4D, 06/50), the places of the trap it drags (06/34) and
/// the list of targets it aims at (06/36). The original server tells everybody who sees the player but the player, and only
/// in a maze; the caller has checked both. What the client says is sent on as it is when it can be shown: a number out of
/// bounds is dropped, and the actor in the message is the player's own, never the one the request names.
/// </summary>
public sealed class SkillInputRelay(World world, IPlayerAudience audience)
{
    /// <summary>
    /// The most targets a list may hold: a skill aims at the monsters around, never at a crowd.
    /// </summary>
    public const int MaxPreTargets = 32;

    /// <summary>
    /// The highest number of a sub animation a client may ask for.
    /// </summary>
    public const int MaxSubAnimation = 255;

    /// <summary>
    /// The longest a key may be held to charge a skill, in seconds.
    /// </summary>
    public const float MaxPressTime = 60f;

    public void KeyPressed(Entity player, bool pressed)
    {
        audience.NearbyToOthers(player, new SkillKeypressBroadcastResponse { Target = ActorOf(player), State = pressed });
    }

    public void TargetChanged(Entity player, ActorValue target)
    {
        audience.NearbyToOthers(player, new SkillTargetChangeBroadcastResponse { Actor = ActorOf(player), TargetActor = target.Identifier });
    }

    public void SubInput(Entity player, int animation)
    {
        if (animation is < 0 or > MaxSubAnimation)
        {
            return;
        }

        audience.NearbyToOthers(player, new SkillSubInputBroadcastResponse { Actor = ActorOf(player), SubAnimIndex = animation });
    }

    /// <summary>
    /// The charging of a skill starts, or ends after the time the key was held.
    /// </summary>
    public void Charging(Entity player, bool released, float pressTime)
    {
        if (!released)
        {
            audience.NearbyToOthers(player, new SkillChargingStartBroadcastResponse { Actor = ActorOf(player) });

            return;
        }

        if (float.IsFinite(pressTime) && pressTime is >= 0f and <= MaxPressTime)
        {
            audience.NearbyToOthers(player, new SkillChargingEndBroadcastResponse { Actor = ActorOf(player), PressTime = pressTime });
        }
    }

    public void PreTargets(Entity player, ActorValue[] targets)
    {
        if (targets.Length <= MaxPreTargets)
        {
            audience.NearbyToOthers(player, new SkillPreTargetListResponse { Actor = ActorOf(player), ActorList = targets });
        }
    }

    public void TrapMoved(Entity player, int skill, short trigger, Vector3 position)
    {
        if (Place.IsValid(position))
        {
            audience.NearbyToOthers(player, new SkillTrapPosUpdateBroadcastResponse
            {
                Actor = ActorOf(player),
                Position = default(SkillTrapPosition) with { Skill = skill, TriggerIndex = trigger, Position = position }
            });
        }
    }

    private ActorValue ActorOf(Entity player)
    {
        return world.Get<ActorComponent>(player);
    }
}
