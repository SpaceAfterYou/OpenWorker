using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// The space of an attack in the world. Characters look along -Y when their yaw is zero, and an arc grows from
/// its center towards where the attacker looks, so a center behind the attacker is normal.
/// </summary>
public readonly struct AttackArea
{
    private const float DegreesToRadians = MathF.PI / 180f;
    private const float FullCircle = 180f;

    private readonly bool _box;
    private readonly Vector2 _center;
    private readonly Vector2 _direction;
    private readonly float _yaw;
    private readonly float _bottom;
    private readonly float _top;
    private readonly float _radiusStart;
    private readonly float _radiusEnd;
    private readonly float _halfAngle;
    private readonly Vector2 _halfSize;

    public AttackArea(in AttackTrigger trigger, Vector3 position, float yaw)
    {
        var (sin, cos) = MathF.SinCos(yaw * DegreesToRadians);

        _box = trigger.RangeType == AttackTrigger.BoxRange;
        _center = new Vector2(
            trigger.Center.X * cos - trigger.Center.Y * sin + position.X,
            trigger.Center.X * sin + trigger.Center.Y * cos + position.Y);
        _direction = new Vector2(sin, -cos);
        _yaw = yaw;
        _bottom = position.Z + trigger.Center.Z;
        _top = _bottom + trigger.Height;
        _radiusStart = trigger.StartPosition;
        _radiusEnd = trigger.Radius;
        _halfAngle = trigger.Angle / 2f;
        _halfSize = new Vector2(trigger.BoxX, trigger.BoxY) / 2f;
    }

    /// <summary>
    /// Whether a standing cylinder touches the area.
    /// </summary>
    public bool Touches(Vector3 target, float radius, float height)
    {
        if (target.Z > _top || _bottom > target.Z + height)
        {
            return false;
        }

        var offset = new Vector2(target.X, target.Y) - _center;

        return _box ? TouchesBox(offset, radius) : TouchesArc(offset, radius);
    }

    private bool TouchesBox(Vector2 offset, float radius)
    {
        var (sin, cos) = MathF.SinCos(-_yaw * DegreesToRadians);
        var local = new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
        var nearest = Vector2.Clamp(local, -_halfSize, _halfSize);

        return Vector2.DistanceSquared(local, nearest) <= radius * radius;
    }

    private bool TouchesArc(Vector2 offset, float radius)
    {
        var distance = offset.Length();

        if (distance < _radiusStart - radius || distance > _radiusEnd + radius)
        {
            return false;
        }

        if (_halfAngle >= FullCircle || distance <= float.Epsilon)
        {
            return true;
        }

        var dot = Math.Clamp(Vector2.Dot(offset / distance, _direction), -1f, 1f);

        if (MathF.Acos(dot) / DegreesToRadians <= _halfAngle)
        {
            return true;
        }

        return TouchesEdge(offset, radius, _halfAngle) || TouchesEdge(offset, radius, -_halfAngle);
    }

    /// <summary>
    /// A wide target can touch the side of an arc while its center is outside of it.
    /// </summary>
    private bool TouchesEdge(Vector2 offset, float radius, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle * DegreesToRadians);
        var side = new Vector2(_direction.X * cos - _direction.Y * sin, _direction.X * sin + _direction.Y * cos);
        var along = Math.Clamp(Vector2.Dot(offset, side), _radiusStart, _radiusEnd);

        return Vector2.DistanceSquared(offset, side * along) <= radius * radius;
    }
}
