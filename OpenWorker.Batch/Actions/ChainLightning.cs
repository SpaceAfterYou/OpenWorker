using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// A target the chain reaches. <see cref="Order"/> is its place in the chain, the first one is 0.
/// </summary>
public readonly record struct ChainHit(int Target, int Order);

/// <summary>
/// A target the chain takes on its way or lets go.
/// </summary>
public readonly record struct ChainChange(int Target, bool Added);

/// <summary>
/// A chain jumping between targets. Each link flies to its target and strikes it on arrival, and a repeating
/// chain strikes everyone it holds again at every interval until it runs out of life.
/// </summary>
public sealed class ChainLightning
{
    /// <summary>
    /// Only so many of the nearest targets are looked at for the next link.
    /// </summary>
    private const int Nearest = 5;

    private const float SearchDistance = 5000f;

    private readonly AttackTrigger _trigger;
    private readonly ChainInfo _chain;
    private readonly ProjectileInfo _flight;
    private readonly List<int> _targets = [];
    private readonly List<int> _struck = [];
    private readonly Vector3[] _links;
    private float _life;
    private float _sinceHit;

    private ChainLightning(in AttackTrigger trigger, Vector3 position)
    {
        _trigger = trigger;
        _chain = trigger.Chain ?? throw new ArgumentException("The attack is not a chain.", nameof(trigger));
        _flight = trigger.Projectile ?? throw new ArgumentException("The attack does not say how a link flies.", nameof(trigger));
        _life = _chain.LifeTime;
        _links = new Vector3[Math.Max(_chain.MaxCount, 1)];

        Array.Fill(_links, position);
    }

    public bool IsOver { get; private set; }

    /// <summary>
    /// The targets the chain holds, in the order they were taken.
    /// </summary>
    public IReadOnlyList<int> Targets => _targets;

    /// <summary>
    /// Starts a chain from the attacker. The first target is the nearest one in the area of the attack.
    /// </summary>
    public static ChainLightning Fire(in AttackTrigger trigger, Vector3 position, float yaw, IReadOnlyList<FlightTarget> targets)
    {
        var chain = new ChainLightning(trigger, position);
        var area = new AttackArea(in trigger, position, yaw);
        var nearest = targets
            .Where(target => area.Touches(target.Position, target.Radius, target.Height))
            .OrderBy(target => Vector3.DistanceSquared(target.Position, position))
            .Select(target => (int?)target.Key)
            .FirstOrDefault();

        if (nearest is { } first)
        {
            chain._targets.Add(first);
        }

        return chain;
    }

    /// <param name="seconds">Time since the last call.</param>
    /// <param name="targets">Everyone the chain may strike, as they stand now.</param>
    /// <param name="owner">Where the attacker stands now.</param>
    /// <param name="yaw">Where the attacker looks now.</param>
    public void Advance(float seconds, IReadOnlyList<FlightTarget> targets, Vector3 owner, float yaw, List<ChainHit> hits, List<ChainChange> changes)
    {
        if (IsOver)
        {
            return;
        }

        _life -= seconds;

        if (_life <= 0f)
        {
            IsOver = true;
            return;
        }

        var byKey = targets.ToDictionary(target => target.Key);

        Forget(byKey, changes);
        Repeat(seconds, byKey, owner, yaw, hits, changes);
        Extend(byKey, owner, yaw, changes);
        Fly(seconds, byKey, hits);
    }

    /// <summary>
    /// Targets that are gone leave the chain.
    /// </summary>
    private void Forget(Dictionary<int, FlightTarget> targets, List<ChainChange> changes)
    {
        foreach (var gone in _targets.Where(key => !targets.ContainsKey(key)).ToArray())
        {
            _targets.Remove(gone);
            _struck.Remove(gone);
            changes.Add(new ChainChange(gone, false));
        }

        _struck.RemoveAll(key => !targets.ContainsKey(key));
    }

    private void Repeat(float seconds, Dictionary<int, FlightTarget> targets, Vector3 owner, float yaw, List<ChainHit> hits, List<ChainChange> changes)
    {
        if (!_flight.IsContinuous || _flight.ContinuousTime <= 0f)
        {
            return;
        }

        _sinceHit += seconds;

        if (_sinceHit < _flight.ContinuousTime)
        {
            return;
        }

        _sinceHit -= _flight.ContinuousTime;

        if (!_chain.RecalculatesTargets)
        {
            StrikeAll(hits);
            return;
        }

        var previous = _targets.ToArray();
        var struck = _struck.ToArray();

        _targets.Clear();
        _struck.Clear();
        Extend(targets, owner, yaw, changes: null);

        if (_targets.FirstOrDefault(-1) == previous.FirstOrDefault(-1))
        {
            // The same chain again: everyone is struck where they are.
            _targets.Clear();
            _targets.AddRange(previous);
            _struck.AddRange(struck);
            StrikeAll(hits);
            return;
        }

        changes.AddRange(previous.Select(key => new ChainChange(key, false)));
        changes.AddRange(_targets.Select(key => new ChainChange(key, true)));
    }

    private void StrikeAll(List<ChainHit> hits)
    {
        for (var order = 0; order < _struck.Count; order++)
        {
            hits.Add(new ChainHit(_struck[order], order));
        }
    }

    /// <summary>
    /// Takes new targets the way the chain connects them.
    /// </summary>
    private void Extend(Dictionary<int, FlightTarget> targets, Vector3 owner, float yaw, List<ChainChange>? changes)
    {
        var allAtOnce = _chain.ConnectionType == ChainInfo.AllAtOnce
            || (_chain.ConnectionType == ChainInfo.FromFirst && _struck.Count > 0);

        if (!allAtOnce)
        {
            if (_struck.Count == _targets.Count)
            {
                TakeNext(targets, owner, yaw, changes);
            }

            return;
        }

        for (var i = _targets.Count; i < _chain.MaxCount; i++)
        {
            if (!TakeNext(targets, owner, yaw, changes))
            {
                return;
            }
        }
    }

    private bool TakeNext(Dictionary<int, FlightTarget> targets, Vector3 owner, float yaw, List<ChainChange>? changes)
    {
        if (_targets.Count >= _chain.MaxCount)
        {
            return false;
        }

        var from = Source(targets, owner);
        var flat = new Vector2(from.X, from.Y);
        var area = _struck.Count > 0
            ? new AttackArea(new AttackTrigger(0, 0, 0, 0, 0f, 360f, _chain.Center, 0f, 0f, _chain.Radius, 2f * _chain.Radius, _chain.StartPosition), from - new Vector3(0, 0, _chain.Radius), 0f)
            : new AttackArea(in _trigger, owner, yaw);

        var next = targets.Values
            .Where(target => !_targets.Contains(target.Key))
            .Select(target => (Target: target, Distance: Vector2.Distance(new Vector2(target.Position.X, target.Position.Y), flat)))
            .Where(e => e.Distance < SearchDistance)
            .OrderBy(e => e.Distance)
            .Take(Nearest)
            .Select(e => e.Target)
            .Where(target => area.Touches(target.Position, target.Radius, target.Height))
            .Select(target => (int?)target.Key)
            .FirstOrDefault();

        if (next is not { } key)
        {
            return false;
        }

        _targets.Add(key);
        changes?.Add(new ChainChange(key, true));

        return true;
    }

    /// <summary>
    /// The next link starts from the last target struck, a chain from the first one starts from that one.
    /// </summary>
    private Vector3 Source(Dictionary<int, FlightTarget> targets, Vector3 owner)
    {
        if (_struck.Count == 0)
        {
            return owner;
        }

        var key = _chain.ConnectionType switch
        {
            ChainInfo.OneByOne => _struck[^1],
            ChainInfo.FromFirst => _struck[0],
            _ => (int?)null
        };

        return key is { } k && targets.TryGetValue(k, out var source) ? source.Position : owner;
    }

    /// <summary>
    /// Each link flies to its target and strikes whoever it touches first on the way.
    /// </summary>
    private void Fly(float seconds, Dictionary<int, FlightTarget> targets, List<ChainHit> hits)
    {
        for (var i = 0; i < _links.Length; i++)
        {
            if (i >= _targets.Count)
            {
                _links[i] = _targets.Count > 0 ? _links[_targets.Count - 1] : _links[i];
                continue;
            }

            if (!targets.TryGetValue(_targets[i], out var target))
            {
                continue;
            }

            if (_struck.Contains(target.Key))
            {
                _links[i] = target.Position;
                continue;
            }

            var way = target.Position - _links[i];
            var step = _flight.Speed * seconds;

            _links[i] = way.Length() > step ? _links[i] + Vector3.Normalize(way) * step : target.Position;

            Touch(_links[i], targets, hits);
        }
    }

    private void Touch(Vector3 link, Dictionary<int, FlightTarget> targets, List<ChainHit> hits)
    {
        if (_struck.Count >= _chain.MaxCount)
        {
            return;
        }

        var area = new AttackArea(new AttackTrigger(0, 0, 0, 0, 0f, 360f, Vector3.Zero, 0f, 0f, _flight.Radius, 2f * _flight.Radius, 0f), link - new Vector3(0, 0, _flight.Radius), 0f);
        var flat = new Vector2(link.X, link.Y);

        var touched = targets.Values
            .Where(target => !_struck.Contains(target.Key))
            .Select(target => (Target: target, Distance: Vector2.Distance(new Vector2(target.Position.X, target.Position.Y), flat)))
            .Where(e => e.Distance < SearchDistance)
            .OrderBy(e => e.Distance)
            .Take(Nearest)
            .Select(e => e.Target)
            .Where(target => area.Touches(target.Position, target.Radius, target.Height))
            .Select(target => (int?)target.Key)
            .FirstOrDefault();

        if (touched is not { } key)
        {
            return;
        }

        hits.Add(new ChainHit(key, _struck.Count));
        _struck.Add(key);
    }
}
