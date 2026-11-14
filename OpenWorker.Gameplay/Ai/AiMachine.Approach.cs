using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// How a monster comes up to its target for a skill: it takes a place around the target and walks to it, and one that cannot
/// get anywhere is put where it can fight, or home.
/// </summary>
public sealed partial class AiMachine
{
    private TargetPlaces? _ownPlaces;
    private int _place = TargetPlaces.None;
    private int _placeTarget = NoTarget;
    private Vector2 _placeOffset;
    private Vector3? _approachEnd;
    private int _failedWays;
    private float _noFloorTime = NoFloorWarp;

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
