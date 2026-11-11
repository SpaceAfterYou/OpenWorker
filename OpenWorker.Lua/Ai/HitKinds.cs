using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Lua.Ai;

internal static class HitKinds
{
    /// <summary>
    /// What kind of blow an attack of an action file deals: a trap, a projectile, or a skill by itself.
    /// </summary>
    public static AiHitKind Of(AttackTrigger trigger)
    {
        return trigger.IsTrap ? AiHitKind.Trap : trigger.Projectile is not null ? AiHitKind.Projectile : AiHitKind.Skill;
    }
}
