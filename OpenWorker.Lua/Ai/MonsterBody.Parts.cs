using OpenWorker.Batch.Actions;

namespace OpenWorker.Lua.Ai;

internal partial class MonsterBody
{
    private const string WalkAnimation = "N_Walk";
    private const string GazeAnimation = "B_Gaze_F";
    private static readonly string[] BattleRun = ["B_Run_F", "B_Run"];
    private static readonly string[] Run = ["N_Run", "B_Run_F", "B_Run"];

    /// <summary>
    /// The animations the monster has begun of its own (idle actions, turns, reactions, a spawn, a change) and when, the last
    /// one last: the bones follow the one that plays.
    /// </summary>
    private readonly List<(double Start, string Animation)> _motions = [];

    /// <summary>
    /// The clients have been told the monster walks, and since when it walks or stands.
    /// </summary>
    private bool _walkShown;

    private double _poseSince;

    /// <summary>
    /// The animation the hit bones of the monster are placed by now, and how far it has played in its own time: the animation
    /// of the skill it plays, else the one it began of its own until that is over, else its walk or the way it stands, which go
    /// round.
    /// </summary>
    public (ActionAnimation? Animation, float Time) HitAnimation()
    {
        var now = _manager.Now;
        var since = _poseSince;

        foreach (var (start, end, clip) in _clips)
        {
            if (now >= start && now < end)
            {
                return (clip, clip.Bones is null ? 0f : SkillActionProvider.AnimationTime(clip, (float)(now - start)));
            }

            if (end <= now)
            {
                since = Math.Max(since, end);
            }
        }

        for (var index = _motions.Count - 1; index >= 0; index--)
        {
            var (start, name) = _motions[index];

            if (start > now)
            {
                continue;
            }

            if (_actions.GetAnimation(Behavior.ActionFile, name) is { Length: > 0f } motion)
            {
                var played = (float)(now - start);

                if (motion.Repeats)
                {
                    return (motion, played % motion.Length);
                }

                if (played < motion.Length)
                {
                    return (motion, played);
                }

                since = Math.Max(since, start + motion.Length);
            }

            break;
        }

        var pose = PoseAnimation();

        return (pose, pose is { Length: > 0f } ? (float)(now - since) % pose.Length : 0f);
    }

    /// <summary>
    /// What the monster plays when it does nothing else: its walk (a run when it runs, a sideways step when it walks facing its
    /// target), or the way it stands in or out of the fight.
    /// </summary>
    private ActionAnimation? PoseAnimation()
    {
        var file = Behavior.ActionFile;

        if (_walkShown && IsMoving)
        {
            if (_gazing && _actions.GetAnimation(file, GazeAnimation) is { } gaze)
            {
                return gaze;
            }

            if (!_run && _actions.GetAnimation(file, WalkAnimation) is { } walk)
            {
                return walk;
            }

            foreach (var name in _battlePose ? BattleRun : Run)
            {
                if (_actions.GetAnimation(file, name) is { } run)
                {
                    return run;
                }
            }
        }

        return _actions.GetAnimation(file, _battlePose ? "B_Stand" : "N_Stand");
    }

    /// <summary>
    /// The monster begins an animation of its own, now or later: one that begins now ends the ones before it.
    /// </summary>
    private void BeginMotion(string animation, float after)
    {
        if (after <= 0f)
        {
            _motions.Clear();
        }

        _motions.Add((_manager.Now + Math.Max(after, 0f), animation));
    }

    /// <summary>
    /// The clients are told the monster walks, or that it stands again: the walk or the stand begins, and ends what it played.
    /// </summary>
    private void ShowWalk(bool walking)
    {
        if (walking == _walkShown)
        {
            return;
        }

        _walkShown = walking;
        _poseSince = _manager.Now;
        _motions.Clear();
    }

    private void ForgetMotions()
    {
        _motions.Clear();
        _walkShown = false;
        _poseSince = _manager.Now;
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
