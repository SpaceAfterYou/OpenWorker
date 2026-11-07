using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// Something the flight can run into.
/// </summary>
public readonly record struct FlightTarget(int Key, Vector3 Position, float Radius, float Height);

/// <summary>
/// A place where a projectile went off. <see cref="Collider"/> is whom it hit, if it hit someone.
/// </summary>
public readonly record struct Explosion(Vector3 Position, int? Collider);

/// <summary>
/// A projectile on its way. The server has no map geometry, so the ground is flat at the height of the shooter's feet, and there are no walls.
/// </summary>
public sealed class ProjectileFlight
{
    private const float Gravity = 980f;
    private const float DegreesToRadians = MathF.PI / 180f;
    private const float MinStride = 25f;

    private const short ExplodesOnGround = 1;
    private const short ExplodesOnGroundOnly = 6;
    private const short LastDecisionWithTargets = 2;

    private readonly ProjectileInfo _info;
    private readonly float _ground;
    private readonly int? _target;
    private readonly HashSet<int> _pierced = [];
    private Vector3 _velocity;
    private float _sinceHit;
    private bool _penetrates;

    private ProjectileFlight(ProjectileInfo info, Vector3 position, Vector3 direction, float ground, int? target)
    {
        _info = info;
        _ground = ground;
        _target = info.IsGuided ? target : null;
        _velocity = direction * info.Speed;
        _penetrates = info.Penetrates;
        Position = position;
        Direction = direction;
    }

    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; }
    public float Age { get; private set; }
    public bool IsOver { get; private set; }

    /// <param name="trigger">The attack, which must be a projectile.</param>
    /// <param name="position">Where the shooter stands.</param>
    /// <param name="right">To the right of the shooter.</param>
    /// <param name="yaw">Where the shooter looks.</param>
    /// <param name="camera">The rotation of the camera: X turns, Z lowers.</param>
    /// <param name="target">Who a guided projectile follows.</param>
    public static ProjectileFlight Launch(in AttackTrigger trigger, Vector3 position, Vector3 right, float yaw, Vector3 camera, int? target = null)
    {
        var info = trigger.Projectile ?? throw new ArgumentException("The attack does not fly.", nameof(trigger));
        var template = info.Direction;
        Vector3 direction;

        if (info.DirectionType == ProjectileInfo.ByCamera)
        {
            var cameraYaw = camera.X + 90f;
            var cameraPitch = -camera.Z;

            direction = Rotate(Axis(template.X, template.Y), cameraYaw, cameraPitch);
            right = Rotate(RightAxis(template.X, template.Y, template.Z), cameraYaw, cameraPitch);
        }
        else
        {
            direction = Axis(template.X + yaw - 90f, template.Y);
        }

        var start = trigger.AttackType == AttackTrigger.FromBody
            ? Rotate(info.Start, yaw - 90f, 0f) + position
            : position + new Vector3(0f, 0f, info.Start.Z) + direction * info.Start.X + right * info.Start.Y;

        return new ProjectileFlight(info, start, direction, position.Z, target);
    }

    /// <summary>
    /// Moves the projectile on. A fast one is checked along its way, so that it does not jump over a target.
    /// </summary>
    public List<Explosion> Advance(float seconds, IReadOnlyList<FlightTarget> targets)
    {
        var explosions = new List<Explosion>();
        var stride = MathF.Max(_info.Radius, MinStride);
        var steps = _info.Speed > 0f ? Math.Max(1, (int)MathF.Ceiling(_info.Speed * seconds / stride)) : 1;
        var step = seconds / steps;

        for (var i = 0; i < steps && !IsOver; i++)
        {
            Step(step, targets, explosions);
        }

        return explosions;
    }

    private void Step(float seconds, IReadOnlyList<FlightTarget> targets, List<Explosion> explosions)
    {
        Age += seconds;

        if (Age >= _info.LifeTime)
        {
            IsOver = true;
            return;
        }

        Move(seconds, targets);

        if (_info.ExplodeDecision is ExplodesOnGround or ExplodesOnGroundOnly && Position.Z <= _ground)
        {
            explosions.Add(new Explosion(Position with { Z = _ground }, null));
            IsOver = true;
            return;
        }

        if (_info.IsContinuous && _info.ContinuousTime > 0f)
        {
            _sinceHit += seconds;

            if (_sinceHit >= _info.ContinuousTime)
            {
                _sinceHit -= _info.ContinuousTime;

                Think(targets, explosions);
            }

            return;
        }

        if (_info.ExplodeDecision <= LastDecisionWithTargets)
        {
            Think(targets, explosions);
        }
    }

    private void Move(float seconds, IReadOnlyList<FlightTarget> targets)
    {
        if (_target is { } key)
        {
            foreach (var target in targets)
            {
                if (target.Key == key && target.Position != Position)
                {
                    _velocity = Vector3.Normalize(target.Position - Position) * _info.Speed;
                    break;
                }
            }
        }

        if (_info.HasGravity)
        {
            _velocity.Z -= Gravity * seconds;
        }

        var delta = _velocity * seconds;

        if (_info.DirectionType == ProjectileInfo.AlongGround)
        {
            delta.Z = 0f;
        }

        Position += delta;
    }

    private void Think(IReadOnlyList<FlightTarget> targets, List<Explosion> explosions)
    {
        if (_penetrates)
        {
            foreach (var target in targets)
            {
                if (_pierced.Contains(target.Key) || !Touches(target))
                {
                    continue;
                }

                _pierced.Add(target.Key);

                if (_info.IsContinuous)
                {
                    continue;
                }

                explosions.Add(new Explosion(Position, target.Key));

                if (_pierced.Count > _info.PenetrateCount)
                {
                    _penetrates = false;
                    IsOver = true;
                    return;
                }
            }

            if (_info.IsContinuous)
            {
                explosions.Add(new Explosion(Position, null));
            }

            return;
        }

        var nearest = targets
            .Where(Touches)
            .OrderBy(e => Vector3.DistanceSquared(e.Position, Position))
            .Select(e => (FlightTarget?)e)
            .FirstOrDefault();

        if (nearest is { } hit)
        {
            explosions.Add(new Explosion(Position, hit.Key));
            IsOver = !_info.IsContinuous;
        }
        else if (_info.IsContinuous)
        {
            explosions.Add(new Explosion(Position, null));
        }
    }

    private bool Touches(FlightTarget target)
    {
        var radius = _info.Radius;

        if (target.Position.Z > Position.Z + radius || Position.Z - radius > target.Position.Z + target.Height)
        {
            return false;
        }

        var offset = new Vector2(target.Position.X - Position.X, target.Position.Y - Position.Y);

        return offset.Length() <= radius + target.Radius;
    }

    /// <summary>
    /// Where a thing with these angles looks: zero looks along +X.
    /// </summary>
    private static Vector3 Axis(float yaw, float pitch)
    {
        return Rotate(Vector3.UnitX, yaw, pitch);
    }

    private static Vector3 RightAxis(float yaw, float pitch, float roll)
    {
        var (sin, cos) = MathF.SinCos(roll * DegreesToRadians);

        return Rotate(new Vector3(0f, cos, sin), yaw, pitch);
    }

    private static Vector3 Rotate(Vector3 value, float yaw, float pitch)
    {
        var (sinPitch, cosPitch) = MathF.SinCos(pitch * DegreesToRadians);
        var (sinYaw, cosYaw) = MathF.SinCos(yaw * DegreesToRadians);
        var x = value.X * cosPitch + value.Z * sinPitch;
        var z = -value.X * sinPitch + value.Z * cosPitch;

        return new Vector3(x * cosYaw - value.Y * sinYaw, x * sinYaw + value.Y * cosYaw, z);
    }
}
