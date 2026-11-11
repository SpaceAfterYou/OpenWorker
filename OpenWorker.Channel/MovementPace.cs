using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Telemetry;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel;

/// <summary>
/// What the server does with the place a client says it has moved to, as the original server does it.<br/>
/// A move is refused only when it takes the player further than <see cref="WorldOptions.MaxMoveStep"/> from the place the
/// server keeps, the two axes added: the original refuses nothing else for the pace. A dash, an evade or a jump covers
/// far less than that in one message and goes through.<br/>
/// A move that covers more ground than the player could have run in the time it had is only counted
/// (<c>movement.too_fast</c>), as the original counts and logs it. A player saves up running time as time passes, at most
/// <see cref="WorldOptions.MoveBurst"/> of it, and a move spends the distance it covers divided by the highest speed
/// allowed: the run speed of its hero from <c>tb_Character_Info</c>, times the move speed of its abilities (100 is the
/// speed of the table), times <see cref="WorldOptions.SpeedTolerance"/>. Only the ground counts: the height is left to the
/// navigation data.<br/>
/// A client that has been told a place by the server (<see cref="PlaceTold"/>) keeps sending the places of the map it left
/// until it has taken the new one. Such a move, too far from the place the server keeps, is dropped without a fault for
/// <see cref="WorldOptions.PlaceGrace"/>.
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
    private readonly float _maxStep;
    private readonly TimeSpan _grace;
    private readonly TimeProvider _time;
    private readonly Counter<long>? _tooFast;
    private readonly Counter<long>? _refused;

    public MovementPace(ReadOnlyCollection<CharacterInfoRow> characters, IOptions<WorldOptions> options, TimeProvider time, IMeterFactory? meters = null)
        : this(RunSpeeds(characters), options.Value, time, meters)
    {
    }

    /// <param name="speeds">The run speed of each hero, in game units per second. With none, no move is counted as too fast.</param>
    internal MovementPace(IReadOnlyDictionary<Hero, float> speeds, WorldOptions options, TimeProvider time, IMeterFactory? meters = null)
    {
        _speeds = new Dictionary<Hero, float>(speeds);
        _fallback = _speeds.Count == 0 ? 0f : _speeds.Values.Max();
        _tolerance = options.SpeedTolerance;
        _burst = (float)options.MoveBurst.TotalSeconds;
        _maxStep = options.MaxMoveStep;
        _grace = options.PlaceGrace;
        _time = time;

        if (meters is not null)
        {
            var meter = OpenWorkerMeter.Create(meters);

            _tooFast = meter.CreateCounter<long>(
                "movement.too_fast",
                unit: "{move}",
                description: "Moves that covered more ground than the player could have run in the time it had. They are let through, as the original server does.");

            _refused = meter.CreateCounter<long>(
                "movement.refused",
                unit: "{move}",
                description: "Moves refused because they took the player too far from its place in one message. Tag reason: far, stale.");
        }
    }

    /// <summary>
    /// The table gives speeds: moves are checked for being too fast.
    /// </summary>
    public bool Enabled => _fallback > 0f;

    /// <summary>
    /// The server has told the client of the player a place: the moves it sent before it took the place are not faults.
    /// </summary>
    public void PlaceTold(World world, Entity player)
    {
        EnsureComponent(world, player);

        ref var pace = ref world.Get<MovementPaceComponent>(player);

        pace.Told = true;
        pace.ToldAt = _time.GetTimestamp();
    }

    /// <summary>
    /// Whether a move from a place to another is taken. A move that is taken spends the running it takes (a move that is
    /// too fast is counted and spends all that is saved up); a move that is not leaves the savings as they were.
    /// </summary>
    /// <param name="charge">The refusal counts against the error budget of the session: the first one in a second.</param>
    public MoveVerdict Check(World world, Entity player, Vector3 from, Vector3 to, out bool charge)
    {
        charge = false;

        EnsureComponent(world, player);

        ref var pace = ref world.Get<MovementPaceComponent>(player);

        var now = _time.GetTimestamp();
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        // A place that is not a number is as far as it gets.
        if (!(MathF.Abs(dx) + MathF.Abs(dy) <= _maxStep))
        {
            if (pace.Told && _time.GetElapsedTime(pace.ToldAt, now) <= _grace)
            {
                _refused?.Add(1, new KeyValuePair<string, object?>("reason", "stale"));

                return MoveVerdict.Stale;
            }

            _refused?.Add(1, new KeyValuePair<string, object?>("reason", "far"));

            if (!pace.Charged || _time.GetElapsedTime(pace.ChargedAt, now) >= ChargeInterval)
            {
                pace.Charged = true;
                pace.ChargedAt = now;

                charge = true;
            }

            return MoveVerdict.Refused;
        }

        // The client has taken the place the server told it: its moves are from there.
        pace.Told = false;

        if (Enabled)
        {
            Spend(ref pace, now, MathF.Sqrt((dx * dx) + (dy * dy)) / Speed(world, player));
        }

        return MoveVerdict.Taken;
    }

    private void Spend(ref MovementPaceComponent pace, long now, float cost)
    {
        pace.Credit = pace.Started
            ? MathF.Min(_burst, pace.Credit + (float)_time.GetElapsedTime(pace.At, now).TotalSeconds)
            : _burst;

        pace.Started = true;
        pace.At = now;

        if (cost <= pace.Credit)
        {
            pace.Credit -= cost;

            return;
        }

        pace.Credit = 0f;

        _tooFast?.Add(1);
    }

    private static void EnsureComponent(World world, Entity player)
    {
        if (!world.Has<MovementPaceComponent>(player))
        {
            world.Add<MovementPaceComponent>(player);
        }
    }

    /// <summary>
    /// The highest speed the player is expected to have, in game units per second.
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
