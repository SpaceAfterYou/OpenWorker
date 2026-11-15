using OpenWorker.Batch.Actions;

namespace OpenWorker.Lua.Ai;

internal partial class MonsterBody
{
    /// <summary>
    /// The animation the hit bones of the monster are placed by now, and how far it has played in its own time: the animation
    /// of the skill it plays, else the first frame of the way it stands.
    /// </summary>
    public (ActionAnimation? Animation, float Time) HitAnimation()
    {
        var now = _manager.Now;

        foreach (var (start, end, clip) in _clips)
        {
            if (now >= start && now < end)
            {
                return (clip, clip.Bones is null ? 0f : SkillActionProvider.AnimationTime(clip, (float)(now - start)));
            }
        }

        return (_actions.GetAnimation(Behavior.ActionFile, _battlePose ? "B_Stand" : "N_Stand"), 0f);
    }

    /// <summary>
    /// A broken part changes the monster: it stops whatever it does and plays the animation of the change, which nothing
    /// interrupts. How long it lasts, zero when the file has no such animation.
    /// </summary>
    public float PlayChange(string animation)
    {
        var length = _actions.GetAnimationLength(Behavior.ActionFile, animation);

        if (length <= 0f)
        {
            return 0f;
        }

        Stagger(length);

        return PlayAction(animation);
    }
}
