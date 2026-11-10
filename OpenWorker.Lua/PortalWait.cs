using Arch.Core;

namespace OpenWorker.Lua;

/// <summary>
/// The players standing at the portals of a maze, and when they go through. A portal that leads to another place of the map
/// takes everyone in the maze along, so it waits for them: when all of them stand at it, it opens at once; when half of them
/// or more do, a countdown of a few seconds starts after which it opens for all, whoever is not there. A player alone is
/// everyone, so the portal opens as soon as the player steps into it. The countdown is told to the maze second by second,
/// and told off when it stops, because someone stepped out of the portal or left the maze.
/// </summary>
public sealed class PortalWait(Func<int> users, Action<int> tell, Action<PortalWait.Door> through)
{
    /// <summary>
    /// How long the countdown takes, in seconds, from the moment half of the players stand at a portal.
    /// </summary>
    public const float Delay = 6f;

    /// <summary>
    /// The number the countdown starts with.
    /// </summary>
    public const int FirstSecond = 5;

    /// <summary>
    /// A portal of the map and the place it leads to, as the client names them.
    /// </summary>
    public readonly record struct Door(int Portal, int Jump);

    private sealed class Crowd(Door door)
    {
        public Door Door { get; } = door;
        public List<Entity> Users { get; } = [];
    }

    private readonly List<Crowd> _crowds = [];
    private int _jump;
    private Crowd? _current;
    private float _time;
    private int _second;

    /// <summary>
    /// Whether a countdown runs.
    /// </summary>
    public bool IsCounting { get; private set; }

    /// <summary>
    /// How many players stand at the portal.
    /// </summary>
    public int WaitingAt(int portal) => _crowds.Where(e => e.Door.Portal == portal).Sum(e => e.Users.Count);

    /// <summary>
    /// The player steps into the portal. A player stands at one portal: the one they were at before is left.
    /// </summary>
    public void Add(Entity player, Door door)
    {
        RemoveFrom(player);

        _jump = door.Jump;

        var crowd = _crowds.Find(e => e.Door == door);

        if (crowd is null)
        {
            crowd = new Crowd(door);
            _crowds.Add(crowd);
        }

        crowd.Users.Add(player);

        Check();
    }

    /// <summary>
    /// The player steps out of the portal, or leaves the maze.
    /// </summary>
    public void Remove(Entity player)
    {
        RemoveFrom(player);

        // The players in the maze are fewer when somebody leaves it: the ones at the portal may be all that is left.
        Check();
    }

    private void RemoveFrom(Entity player)
    {
        foreach (var crowd in _crowds)
        {
            crowd.Users.Remove(player);
        }

        _crowds.RemoveAll(e => e.Users.Count == 0);
    }

    /// <summary>
    /// Counts the players at the portal that was stepped into last against the players in the maze.
    /// </summary>
    private void Check()
    {
        var everyone = users();

        if (everyone <= 0)
        {
            return;
        }

        foreach (var crowd in _crowds)
        {
            if (crowd.Door.Jump != _jump)
            {
                continue;
            }

            var count = crowd.Users.Count;

            if (count == everyone)
            {
                Through(crowd, tellOff: IsCounting);

                return;
            }

            if (count * 2 >= everyone)
            {
                _current = crowd;

                if (!IsCounting)
                {
                    IsCounting = true;
                    _time = Delay;
                    _second = FirstSecond;
                }

                return;
            }
        }

        if (IsCounting)
        {
            IsCounting = false;
            _current = null;

            tell(0);
        }
    }

    /// <summary>
    /// Time passes: the countdown tells the seconds and, when they are over, opens the portal for everyone.
    /// </summary>
    public void Advance(float seconds)
    {
        if (!IsCounting)
        {
            return;
        }

        _time -= seconds;

        while (IsCounting && (int)Math.Floor(_time) <= _second)
        {
            if (_second > 0)
            {
                tell(_second--);

                continue;
            }

            if (_current is { } crowd)
            {
                Through(crowd, tellOff: true);
            }

            else
            {
                IsCounting = false;
                tell(0);
            }
        }
    }

    private void Through(Crowd crowd, bool tellOff)
    {
        IsCounting = false;
        _jump = 0;
        _current = null;

        // Everyone goes through: nobody is left standing at any portal.
        _crowds.Clear();

        if (tellOff)
        {
            tell(0);
        }

        through(crowd.Door);
    }
}
