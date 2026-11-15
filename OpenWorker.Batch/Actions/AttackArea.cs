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
    /// The space of a ball: a full circle of its radius, as high as the ball.
    /// </summary>
    public static AttackArea Ball(Vector3 center, float radius)
    {
        var round = new AttackTrigger(0, 0, 0, 0, 0f, 2f * FullCircle, Vector3.Zero, 0f, 0f, radius, 2f * radius, 0f);

        return new AttackArea(round, center - new Vector3(0f, 0f, radius), 0f);
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

    /// <summary>
    /// Whether the area reaches a body made of a standing cylinder and hit bones, and the part it lands on: that of the bone
    /// nearest to the center of the area, unless the cylinder is nearer. A body above or below the area is reached only by its
    /// bones. Nothing is a cylinder without a radius.
    /// </summary>
    public bool Strikes(Vector3 target, float radius, float height, ReadOnlySpan<BonePlace> bones, out byte part)
    {
        part = 0;

        var outOfHeight = target.Z > _top || _bottom > target.Z + height;

        if (outOfHeight && bones.IsEmpty)
        {
            return false;
        }

        var struck = false;
        var nearest = float.MaxValue;

        if (!outOfHeight && radius > 0f)
        {
            var offset = new Vector2(target.X, target.Y) - _center;

            if (_box ? TouchesBox(offset, radius) : TouchesArc(offset, radius))
            {
                struck = true;
                nearest = offset.Length();
            }
        }

        foreach (var bone in bones)
        {
            var offset = new Vector2(bone.Center.X, bone.Center.Y) - _center;
            float distance;

            if (_box)
            {
                if (!TouchesBox(offset, bone.Radius))
                {
                    continue;
                }

                distance = Vector3.Distance(bone.Center, new Vector3(_center, _bottom));
            }
            else
            {
                // A ball is taken as high as twice its radius above and below the bone.
                if (bone.Center.Z - 2f * bone.Radius > _top || _bottom > bone.Center.Z + 2f * bone.Radius || !TouchesArc(offset, bone.Radius))
                {
                    continue;
                }

                distance = offset.Length();
            }

            struck = true;

            if (distance < nearest)
            {
                nearest = distance;
                part = bone.Part;
            }
        }

        return struck;
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
