using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// How a monster comes up to its target for a skill: it takes a place around the target and walks to it, and one that cannot
/// get anywhere is put where it can fight, or home.
/// </summary>
public sealed partial class AiMachine
{
    private const float HalfTurn = 180f;

    /// <summary>
    /// The angle between two places round a target.
    /// </summary>
    private const float PlaceWidth = 360f / TargetPlaces.Count;

    /// <summary>
    /// How far round its target a monster comes in one leg of a battle move, in degrees.
    /// </summary>
    private const float LegTurn = 45f;

    /// <summary>
    /// How many legs a battle move tries toward one place before it looks at the other angles.
    /// </summary>
    private const int RoundTries = 7;

    private TargetPlaces? _ownPlaces;
    private int _place = TargetPlaces.None;
    private int _placeTarget = NoTarget;
    private Vector2 _placeOffset;
    private Vector3? _approachEnd;
    private int _failedWays;
    private float _noFloorTime = NoFloorWarp;

    private void ResetApproach()
    {
        _ownPlaces = null;
        _place = TargetPlaces.None;
        _placeTarget = NoTarget;
        _placeOffset = default;
        _approachEnd = null;
        _failedWays = 0;
        _noFloorTime = NoFloorWarp;
    }

    private TargetPlaces Places => _host.Places ?? (_ownPlaces ??= new TargetPlaces());

    /// <summary>
    /// The place around the target the monster has taken, or <see cref="TargetPlaces.None"/>.
    /// </summary>
    public int Place => _place;

    /// <summary>
    /// The monster leaves the place it holds around its target.
    /// </summary>
    public void LeavePlace()
    {
        if (_place != TargetPlaces.None)
        {
            Places.Leave(_placeTarget, _place);
        }

        _place = TargetPlaces.None;
        _placeTarget = NoTarget;
        _approachEnd = null;
    }

    /// <summary>
    /// Takes the place around the target on the side the monster stands, at a distance within the reach of the skill.
    /// </summary>
    private void TakePlace(AiTarget target, AiSkill skill)
    {
        LeavePlace();

        var away = _host.Position - target.Position;
        var place = Places.Choose(target.Id, AiMath.YawOf(away.X, away.Y));
        var distance = away.Length();
        var low = Math.Max(PlaceNearest, skill.RangeMin);

        // Within reach it comes no farther than it stands; out of reach it comes anywhere within; too near it steps back.
        distance = distance < skill.RangeMin ? skill.RangeMin + RangeSlack
            : distance <= skill.RangeMax ? RandomBetween(low, distance)
            : RandomBetween(low, skill.RangeMax);

        distance = Math.Max(distance, _host.Radius + target.Radius);

        Places.Take(target.Id, place);

        _place = place;
        _placeTarget = target.Id;
        _placeOffset = TargetPlaces.Offset(place, distance);
    }

    /// <summary>
    /// Out of reach: walks to its place around the target, which follows the target. It looks again every
    /// <see cref="ApproachLook"/> seconds, and looks for a way only when the place has moved or it has stopped.
    /// </summary>
    private void Approach(AiTarget target, AiSkill skill)
    {
        if (_host.IsMoving && _repathTime <= ApproachLook)
        {
            return;
        }

        _repathTime = 0f;

        if (_place == TargetPlaces.None || _placeTarget != target.Id)
        {
            TakePlace(target, skill);
        }

        var place = target.Position + new Vector3(_placeOffset, 0f);

        if (AiMath.Distance2D(_host.Position, place) < PlaceReached)
        {
            return;
        }

        if (_host.IsMoving && _approachEnd is { } end && AiMath.Distance2D(end, place) < PlaceMoved)
        {
            return;
        }

        _approachEnd = place;

        if (!_host.MoveTo(place, true))
        {
            _approachEnd = null;

            GiveUpReaching();
        }
    }

    /// <summary>
    /// The monster takes a place before its target, within the angles from where the target looks, and walks one leg round the
    /// target toward it. When no way is found there it tries the other angles round the target, a twelfth of a turn at a time.
    /// </summary>
    private void GoRoundTarget(AiTarget target, float distanceMin, float distanceMax, float angleMin, float angleMax)
    {
        LeavePlace();

        if (TryGoRound(target, distanceMin, distanceMax, angleMin, angleMax))
        {
            return;
        }

        for (var from = angleMax; from < HalfTurn; from += PlaceWidth)
        {
            if (TryGoRound(target, distanceMin, distanceMax, from, Math.Min(from + PlaceWidth, HalfTurn)))
            {
                return;
            }
        }

        for (var from = 0f; from < angleMin; from += PlaceWidth)
        {
            if (TryGoRound(target, distanceMin, distanceMax, from, Math.Min(from + PlaceWidth, angleMin)))
            {
                return;
            }
        }

        CountFailedWay();
    }

    private bool TryGoRound(AiTarget target, float distanceMin, float distanceMax, float angleMin, float angleMax)
    {
        LeavePlace();

        var place = Places.ChooseByAngle(target.Id, angleMin, angleMax);
        var high = TargetPlaces.Angle(place);
        var low = Math.Max(0f, high - PlaceWidth);

        Places.Take(target.Id, place);

        _place = place;
        _placeTarget = target.Id;

        var away = new Vector2(_host.Position.X - target.Position.X, _host.Position.Y - target.Position.Y);
        var current = away.Length();
        var side = current < 1f ? _host.Yaw + HalfTurn : AiMath.YawOf(away.X, away.Y);

        for (var attempt = 0; attempt < RoundTries; attempt++)
        {
            // It keeps its distance half the time when that is within the state's, and stands off the target's body.
            var distance = current < distanceMin || current > distanceMax || _random.Next(2) == 0
                ? RandomBetween(distanceMin, distanceMax)
                : current;

            distance += target.Radius;

            if ((int)distance == 0)
            {
                break;
            }

            // Far away it may come round twice as much in one leg.
            var legTurn = (int)current / (int)distance >= 2 ? 2 * LegTurn : LegTurn;
            var wanted = RandomBetween((int)low * 10, (int)high * 10) * 0.1f * (_random.Next(2) == 0 ? -1 : 1);
            var turn = Wrap(side - (target.Yaw + wanted));
            var leg = Math.Clamp(turn, -legTurn, legTurn);
            var offset = AiMath.Direction(side - leg) * distance;
            var end = new Vector3(target.Position.X + offset.X, target.Position.Y + offset.Y, target.Position.Z);

            _placeOffset = offset;

            if (_host.MoveTo(end, true))
            {
                return true;
            }
        }

        return false;
    }

    private static float Wrap(float angle)
    {
        angle %= 2 * HalfTurn;

        return angle > HalfTurn ? angle - 2 * HalfTurn : angle < -HalfTurn ? angle + 2 * HalfTurn : angle;
    }

    /// <summary>
    /// The third way in a row that is not found puts the monster somewhere: home when it has no target; when the target stands
    /// on a floor of the monster's own sector, as near the monster as the target could go in a straight line, or home when
    /// the target could not go anywhere.
    /// </summary>
    private void CountFailedWay()
    {
        if (++_failedWays < FailedWaysBeforeWarp)
        {
            return;
        }

        _failedWays = 0;

        var target = _skill is { } skill ? TargetFor(skill) : Target;

        if (target is not { } enemy)
        {
            _host.Teleport(_host.HomePosition);

            return;
        }

        var navigation = _host.Navigation;

        if (!_host.IsInOwnSector(enemy.Position) || !navigation.TryGetFloor(enemy.Position, out _))
        {
            return;
        }

        var reach = navigation.Clamp(enemy.Position, _host.Position, enemy.Radius, out _);

        _host.Teleport(reach.X == enemy.Position.X && reach.Y == enemy.Position.Y ? _host.HomePosition : reach);
    }

    /// <summary>
    /// A monster that waits or wanders where there is no floor is put home once it has stood so for a while.
    /// </summary>
    private void CheckFloor(float elapsed)
    {
        if (IsFollower || _host.Navigation.TryGetFloor(_host.Position, out _))
        {
            return;
        }

        _noFloorTime -= elapsed;

        if (_noFloorTime > 0f)
        {
            return;
        }

        _noFloorTime = NoFloorWarp;

        _host.Teleport(_host.HomePosition);
    }
}
