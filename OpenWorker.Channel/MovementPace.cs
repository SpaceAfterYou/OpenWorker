using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel;

/// <summary>
/// Whether a player could have covered the ground a move claims. A player saves up running time as time passes, at most
/// <see cref="WorldOptions.MoveBurst"/> of it, and a move spends the distance it covers divided by the highest speed
/// allowed: the run speed of its hero from <c>tb_Character_Info</c>, times the move speed of its abilities (100 is the
/// speed of the table), times <see cref="WorldOptions.SpeedTolerance"/>. A move that would spend more than is saved up
/// is refused. Saving up lets moves that arrive late, or several at once, through, and a client that reports its place
/// rarely; it bounds a teleport by the burst. Only the ground counts: the height is left to the navigation data.
/// </summary>
public sealed class MovementPace
{
    /// <summary>
    /// A refused move counts against the error budget at most once in this time: the moves the client sent before it saw
    /// the correction are refused too, and they are one fault.
    /// </summary>
    private static readonly TimeSpan ChargeInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<Hero, float> _speeds;
    private readonly float _fallback;
    private readonly float _tolerance;
    private readonly float _burst;
    private readonly TimeProvider _time;

    public MovementPace(ReadOnlyCollection<CharacterInfoRow> characters, IOptions<WorldOptions> options, TimeProvider time)
        : this(RunSpeeds(characters), options.Value, time)
    {
    }

    /// <param name="speeds">The run speed of each hero, in game units per second. With none, no move is refused.</param>
    internal MovementPace(IReadOnlyDictionary<Hero, float> speeds, WorldOptions options, TimeProvider time)
    {
        _speeds = new Dictionary<Hero, float>(speeds);
        _fallback = _speeds.Count == 0 ? 0f : _speeds.Values.Max();
        _tolerance = options.SpeedTolerance;
        _burst = (float)options.MoveBurst.TotalSeconds;
        _time = time;
    }

    /// <summary>
    /// The table gives speeds: moves are checked.
    /// </summary>
    public bool Enabled => _fallback > 0f;

    /// <summary>
    /// Spends the running the move takes, or refuses it and leaves the savings as they were.
    /// </summary>
    /// <param name="charge">The refusal counts against the error budget of the session: the first one in a second.</param>
    public bool TryMove(World world, Entity player, Vector3 from, Vector3 to, out bool charge)
    {
        charge = false;

        if (!Enabled)
        {
            return true;
        }

        if (!world.Has<MovementPaceComponent>(player))
        {
            world.Add<MovementPaceComponent>(player);
        }

        ref var pace = ref world.Get<MovementPaceComponent>(player);

        var now = _time.GetTimestamp();

        pace.Credit = pace.Started
            ? MathF.Min(_burst, pace.Credit + (float)_time.GetElapsedTime(pace.At, now).TotalSeconds)
            : _burst;

        pace.Started = true;
        pace.At = now;

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var cost = MathF.Sqrt((dx * dx) + (dy * dy)) / Speed(world, player);

        if (cost <= pace.Credit)
        {
            pace.Credit -= cost;

            return true;
        }

        if (!pace.Charged || _time.GetElapsedTime(pace.ChargedAt, now) >= ChargeInterval)
        {
            pace.Charged = true;
            pace.ChargedAt = now;

            charge = true;
        }

        return false;
    }

    /// <summary>
    /// The highest speed the player is allowed, in game units per second.
    /// </summary>
    private float Speed(World world, Entity player)
    {
        var speed = world.TryGet(player, out PersonInfoComponent person) && _speeds.TryGetValue(person.Hero, out var run)
            ? run
            : _fallback;

        // The move speed of the abilities is a percentage; a player that has none moves at the speed of the table.
        if (world.TryGet(player, out AbilityComponent abilities) && abilities.Speed.Move > 0f)
        {
            speed *= abilities.Speed.Move / 100f;
        }

        return speed * _tolerance;
    }

    /// <summary>
    /// The fastest a hero runs in the table, in or out of battle, over the rows of the hero.
    /// </summary>
    private static Dictionary<Hero, float> RunSpeeds(IEnumerable<CharacterInfoRow> characters)
    {
        var speeds = new Dictionary<Hero, float>();

        foreach (var row in characters)
        {
            var hero = (Hero)row.Character;
            var run = (float)Math.Max(row.Normal_Run_Speed, row.Battle_Run_Speed);

            if (run > 0f && (!speeds.TryGetValue(hero, out var known) || run > known))
            {
                speeds[hero] = run;
            }
        }

        return speeds;
    }
}
