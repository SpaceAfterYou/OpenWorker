using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What a monster is made of as far as a blow is concerned.
/// </summary>
/// <param name="Weight">The weight rank of the monster: a heavy attacker throws a light monster farther.</param>
/// <param name="Limit">What the monster cannot be thrown by.</param>
/// <param name="StandType">How it stands: 2 and 3 are fixed in their place and 1 waits for a fight.</param>
/// <param name="Rank">The rank of the monster; a blow of a grade below it only makes it flinch.</param>
/// <param name="Radius">How wide the monster is when it is pushed against a wall.</param>
public sealed record ReactionProfile(int Weight, ReactionLimit Limit, int StandType, int Rank, float Radius);

/// <summary>
/// What happened to a monster in a step that the clients have to be told, or the mind has to know.
/// </summary>
[Flags]
public enum ReactionEvents
{
    None = 0,

    /// <summary>The monster came down from a throw: everybody is told where.</summary>
    Landed = 1,

    /// <summary>A wall cut a flight short: everybody is told where the monster fell.</summary>
    FlightCut = 2,

    /// <summary>A player stands in the way of a slide, which stops: everybody is told where the monster is.</summary>
    SlideBlocked = 4,

    /// <summary>A monster that lay down begins to get up.</summary>
    RaiseBegan = 8,

    /// <summary>The reaction is over and the monster is free again.</summary>
    Ended = 16
}

/// <param name="Position">Where the monster is now.</param>
/// <param name="Events">What happened.</param>
/// <param name="Motion">The motion class the monster is in, which a client that has to be told reads.</param>
public readonly record struct ReactionStep(Vector3 Position, ReactionEvents Events, short Motion);

/// <param name="Position">Where the monster is after the blow.</param>
/// <param name="ExtraMove">The throw as the clients are told it in the hit.</param>
/// <param name="FlySpeed">How fast the monster flies, as the clients are told in the hit.</param>
/// <param name="Shown">The monster shows a reaction: whatever it was doing is over.</param>
/// <param name="Motion">The motion class of the reaction, or <see cref="MotionClass.None"/>.</param>
/// <param name="Facing">The way the monster turns to face the attacker, in degrees; not a number when it keeps its facing.</param>
public readonly record struct HitOutcome(Vector3 Position, Vector3 ExtraMove, float FlySpeed, bool Shown, short Motion, float Facing = float.NaN);

/// <summary>
/// What a blow does to a monster over the time that follows it, worked out as the clients work it out from the same blow, so
/// that the place the server keeps for the monster is the place they show: a short slide for a flinch, a throw with a fall
/// for a launch, and the time the monster is lost to the reaction, from the animations of its own action file. The mind of
/// the monster acts when <see cref="BlocksAttack"/> and <see cref="BlocksMove"/> let it.
/// </summary>
public sealed class MonsterReaction(ReactionProfile profile, INavigation ground, Func<string, float> lengthOf, Random random)
{
    private const float Gravity = 1960f;
    private const float HighestLift = 200f;
    private const float LeastLift = 50f;
    private const float HangTime = 0.3f;
    private const float GroundDelay = 0.2f;
    private const float AirDelay = 0.05f;
    private const float SlideTime = 0.1f;
    private const float SlideTail = 0.2f;
    private const float SlideStop = 3f;
    private const float Airborne = 5f;
    private const float LoopLimit = 5f;
    private const float LeastLying = 1f;
    private const float LyingStep = 0.1f;
    private const int LyingSteps = 21;
    private const float MaxWayRadius = 100f;
    private const int FirstFixedStand = 2;

    private enum Phase
    {
        Idle,
        Flinch,
        KnockBack,
        Stunned,

        /// <summary>Thrown: in the air, or lying down already with nothing to fall.</summary>
        Air,
        Landing,
        Lying,
        Rising,
        ArmorBreak
    }

    private Phase _phase;
    private short _motion = MotionClass.Idle;
    private float _left;

    // The fall.
    private bool _flying;
    private float _height;
    private float _baseFloor;
    private float _velocity;
    private float _jumpHeight;
    private float _jumpTime;
    private float _previousLift;
    private float _climbed;
    private float _jumpDelay;
    private float _hang;
    private float _direction;
    private float _airTime;
    private float _groundDown;

    // The flight along the ground.
    private bool _movingInFly;
    private float _flyYaw;
    private float _flyDistance;
    private float _flyMoved;
    private float _flySpeed;

    // The slide.
    private bool _sliding;
    private Vector2 _slideTarget;
    private float _slideTime;
    private float _slideLeft;

    /// <summary>
    /// Whether something stands where a sliding monster would end: a player in the way stops a slide.
    /// </summary>
    public Func<Vector3, bool>? IsBlocked { get; set; }

    /// <summary>
    /// The motion class the monster is in.
    /// </summary>
    public short Motion => _motion;

    /// <summary>
    /// A reaction takes the monster: whatever it was doing is over, and it does nothing else until the reaction is.
    /// </summary>
    public bool IsBusy => _phase != Phase.Idle;

    /// <summary>
    /// The monster cannot start a skill: it flinches (a monster fixed in its place does not mind), is thrown, lies or gets up.
    /// </summary>
    public bool BlocksAttack => _phase switch
    {
        Phase.Idle => false,
        Phase.Flinch or Phase.KnockBack => !IsFixed,
        _ => true
    };

    /// <summary>
    /// The monster is fixed in its place for good: it turns and attacks but no blow slides it.
    /// </summary>
    private bool IsFixed => profile.StandType >= FirstFixedStand;

    /// <summary>
    /// The monster cannot walk: it is in a reaction, which is no common motion.
    /// </summary>
    public bool BlocksMove => _phase != Phase.Idle;

    /// <summary>
    /// The monster is off the ground.
    /// </summary>
    public bool IsAirborne => _phase == Phase.Air && _height > Airborne;

    /// <summary>
    /// The monster is lying down or landing on its back.
    /// </summary>
    public bool IsDown => _phase is Phase.Landing or Phase.Lying;

    /// <summary>
    /// How high above the floor the monster is.
    /// </summary>
    public float Height => _height;

    /// <summary>
    /// The monster is told to stand still in an armor break that no blow can interrupt.
    /// </summary>
    public bool IsArmorBreaking => _phase == Phase.ArmorBreak;

    /// <summary>
    /// A new life: everything is forgotten.
    /// </summary>
    public void Reset()
    {
        _phase = Phase.Idle;
        _motion = MotionClass.Idle;
        _left = 0f;
        _flying = false;
        _height = 0f;
        _movingInFly = false;
        _flySpeed = 0f;
        _jumpDelay = 0f;
        _hang = 0f;
        _climbed = 0f;

        StopSlide();
    }

    /// <summary>
    /// A monster that flinches stands up from the flinch at once: nothing else of a reaction is cut short.
    /// </summary>
    public void EndFlinch()
    {
        if (_phase == Phase.Flinch)
        {
            Reset();
        }
    }

    private void StopSlide()
    {
        _sliding = false;
        _slideLeft = 0f;
        _slideTime = 0f;
    }

    /// <summary>
    /// The armor of the monster broke and it shows it: it stands helpless for the time and nothing moves it.
    /// </summary>
    public void BreakArmor(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        Reset();

        _phase = Phase.ArmorBreak;
        _motion = MotionClass.ArmorBreakStart;
        _left = seconds;
    }

    // ---- A blow -------------------------------------------------------------------------------

    /// <summary>
    /// A blow lands on the monster. It is thrown by the rule of the blow and of the monster, and when it lives on, the
    /// reaction begins. The answer says what the clients are told in the hit.
    /// </summary>
    /// <param name="defense">The defense type of the monster when the blow lands, after its super armor took the blow.</param>
    /// <param name="survives">The monster lives after the blow; one that dies is not moved.</param>
    public HitOutcome Hit(in Blow blow, DefenseType defense, bool survives, Vector3 position, in BlowRolls rolls)
    {
        var push = blow.Reaction;

        // A monster in the break of its armor takes no reaction; the clients are told it stays where it is.
        if (_phase == Phase.ArmorBreak)
        {
            var stays = push.Moves && !IsLaunch(blow.Kind) ? new Vector3(position.X, position.Y, 0f) : Vector3.Zero;

            return new HitOutcome(position, stays, 0f, false, MotionClass.None);
        }

        Anchor(position);

        var airborne = IsAirborne;
        var thrown = ExtraMove.Of(blow, new Struck(position, profile.Weight, profile.Limit, profile.StandType, defense, airborne), rolls);
        var vector = thrown.Vector;
        var kind = Limited(blow.Kind, push.Grade);
        var motion = DamageMotion(kind);
        var displayed = Displayed(push.Hardness, defense);
        var enabled = defense is not (DefenseType.SuperArmor or DefenseType.Invincible or DefenseType.NoReaction) || displayed;

        if (!enabled)
        {
            vector = Vector3.Zero;
        }

        // A throw with no height is no throw, though the animation of one plays.
        var physical = enabled && IsLaunch(kind) && vector.Z <= 0f ? ReactionKind.Flinch : kind;
        var flyAction = enabled && IsLaunch(physical);
        var shown = motion != MotionClass.None && displayed;
        var facing = survives && shown && enabled && !IsFixed && !IsDown ? Facing(thrown.Yaw) : float.NaN;

        if (survives && shown && enabled)
        {
            if (flyAction)
            {
                Launch(physical, vector, thrown.Yaw, push.Speed, motion, airborne);

                position = position with { Z = _baseFloor + _height };
            }
            else if (airborne && MotionClass.IsKnockedDown(motion))
            {
                Juggle(vector);
            }
            else
            {
                React(motion);
            }
        }
        else if (flyAction)
        {
            // The monster does not fly, but the clients are told the speed it would have.
            _flySpeed = LaunchSpeed(push.Speed, vector);
        }

        if (survives && !flyAction && defense == DefenseType.Normal && !IsFixed)
        {
            var offset = IsLaunch(kind) ? AiMath.Rotate(new Vector2(vector.X, vector.Y), thrown.Yaw) : new Vector2(vector.X, vector.Y);

            if (offset != Vector2.Zero)
            {
                Slide(offset, position);
            }
        }

        // When the throw only slides the monster, the clients are told where the slide ends.
        var told = vector;

        if (push.Moves && !IsLaunch(physical))
        {
            var place = _sliding ? _slideTarget : new Vector2(position.X, position.Y);

            told = new Vector3(place.X, place.Y, vector.Z);
        }

        return new HitOutcome(position, told, _flySpeed, shown && survives, shown ? motion : MotionClass.None, facing);
    }

    /// <summary>
    /// A monster that is hit turns its face to the attacker: the opposite of the way the blow goes.
    /// </summary>
    private static float Facing(float blowYaw)
    {
        var yaw = blowYaw + 180f;

        while (yaw > 180f)
        {
            yaw -= 360f;
        }

        while (yaw < -180f)
        {
            yaw += 360f;
        }

        return yaw;
    }

    /// <summary>
    /// The floor under the monster is where it stands, unless it is in the air.
    /// </summary>
    private void Anchor(Vector3 position)
    {
        if (_phase != Phase.Air)
        {
            _baseFloor = position.Z - _height;
        }
    }

    /// <summary>
    /// A blow of a grade below the rank of a monster, on a monster that is not fixed in its place, only makes it flinch if the
    /// table says it cannot be thrown.
    /// </summary>
    private ReactionKind Limited(ReactionKind kind, int grade)
    {
        var forced = profile.StandType < FirstFixedStand && profile.Rank <= grade;

        if (forced)
        {
            return kind;
        }

        if (profile.Limit.HasFlag(ReactionLimit.NoKnockBack) && kind == ReactionKind.KnockBack)
        {
            return ReactionKind.Flinch;
        }

        return profile.Limit.HasFlag(ReactionLimit.NoLaunch) && IsLaunch(kind) ? ReactionKind.Flinch : kind;
    }

    public static bool IsLaunch(ReactionKind kind) => kind is ReactionKind.LaunchUp or ReactionKind.KnockDown or ReactionKind.KnockDownStraight;

    /// <summary>
    /// Whether the animation of a reaction plays at all: a weak blow shows none, and a monster whose armor holds or that
    /// nothing can hurt shows one only for the blows that are strong enough.
    /// </summary>
    public static bool Displayed(int hardness, DefenseType defense)
    {
        if (hardness == 0)
        {
            return false;
        }

        return defense switch
        {
            DefenseType.Guard => hardness is 2 or 4,
            DefenseType.SuperArmor => hardness is 3 or 4,
            DefenseType.Invincible or DefenseType.NoReaction => false,
            _ => true
        };
    }

    /// <summary>
    /// The motion class a blow puts the monster into, by what the monster is doing.
    /// </summary>
    private short DamageMotion(ReactionKind kind)
    {
        if (IsDown)
        {
            return kind switch
            {
                ReactionKind.LaunchUp => _motion is MotionClass.DownBackward or MotionClass.DownForward ? _motion : MotionClass.DownUp,
                ReactionKind.KnockDown => _motion is MotionClass.DownBackward or MotionClass.DownForward or MotionClass.DownStraight ? MotionClass.DownBackward : MotionClass.DownForward,
                ReactionKind.KnockDownStraight => MotionClass.DownStraight,
                _ => _motion
            };
        }

        if (_phase == Phase.Air)
        {
            return kind switch
            {
                ReactionKind.KnockDown => _motion is MotionClass.DownBackward or MotionClass.DownForward ? MotionClass.DownBackward : MotionClass.DownForward,
                ReactionKind.LaunchUp => MotionClass.DownUp,
                ReactionKind.KnockDownStraight => MotionClass.DownStraight,
                _ => MotionClass.IsKnockedDown(_motion) ? _motion : MotionClass.DownStraight
            };
        }

        return kind switch
        {
            ReactionKind.None => MotionClass.None,
            ReactionKind.Flinch => _motion == MotionClass.FlinchLeft ? MotionClass.FlinchRight : _motion == MotionClass.FlinchRight ? MotionClass.FlinchLeft : (short)(MotionClass.FlinchLeft + random.Next(2)),
            ReactionKind.LaunchUp => MotionClass.DownUp,
            ReactionKind.KnockDown => MotionClass.DownBackward,
            ReactionKind.KnockDownStraight => MotionClass.DownStraight,
            ReactionKind.KnockBack => MotionClass.KnockBack,
            ReactionKind.Stun => MotionClass.Stunned,
            _ => MotionClass.FlinchLeft
        };
    }

    /// <summary>
    /// A reaction that keeps the monster on its feet, or leaves what is happening to a monster that is down or in the air as
    /// it is.
    /// </summary>
    private void React(short motion)
    {
        if (_phase is Phase.Air or Phase.Landing or Phase.Lying)
        {
            return;
        }

        switch (motion)
        {
            case MotionClass.FlinchLeft:
                Begin(Phase.Flinch, motion, lengthOf("B_DMG_L"));
                break;
            case MotionClass.FlinchRight:
                Begin(Phase.Flinch, motion, lengthOf("B_DMG_R"));
                break;
            case MotionClass.KnockBack:
                Begin(Phase.KnockBack, motion, lengthOf("B_KB_Start") + lengthOf("B_KB_End"));
                break;
            case MotionClass.Stunned:
                Begin(Phase.Stunned, motion, lengthOf("B_Stun"));
                break;
            default:
                break;
        }
    }

    private void Begin(Phase phase, short motion, float seconds)
    {
        _phase = seconds > 0f ? phase : Phase.Idle;
        _motion = seconds > 0f ? motion : MotionClass.Idle;
        _left = seconds;
    }

    // ---- The throw ----------------------------------------------------------------------------

    private void Launch(ReactionKind kind, Vector3 vector, float yaw, float eventSpeed, short motion, bool airborne)
    {
        var distance = new Vector2(vector.X, vector.Y).Length();
        var height = vector.Z;

        if (kind != ReactionKind.KnockDown && _climbed > 0f)
        {
            height = Math.Max(LeastLift, height - _climbed);
        }

        Lift(height, airborne);

        _flyDistance = distance;
        _flyMoved = 0f;
        _flySpeed = LaunchSpeed(eventSpeed, vector);
        _hang = HangTime;
        _groundDown = LeastLying + (LyingStep * random.Next(LyingSteps));
        _movingInFly = true;
        _flyYaw = yaw;

        StopSlide();

        if (kind == ReactionKind.KnockDown)
        {
            _jumpDelay = airborne ? AirDelay : GroundDelay;

            if (airborne)
            {
                _height = 0f;
            }
        }

        _flying = true;
        _airTime = 0f;
        _phase = Phase.Air;
        _motion = motion;
        _left = lengthOf(Family(motion) + "_Start");
    }

    /// <summary>
    /// How fast a monster flies: the speed of the blow, or the distance it has to cover in a second.
    /// </summary>
    private static float LaunchSpeed(float eventSpeed, Vector3 vector)
    {
        return eventSpeed > 0f ? eventSpeed : new Vector2(vector.X, vector.Y).Length();
    }

    /// <summary>
    /// A monster in the air that is hit again is lifted a little by the blow, whatever it is, and stops moving along the ground.
    /// </summary>
    private void Juggle(Vector3 vector)
    {
        Lift(vector.Z, true);

        _flyDistance = new Vector2(vector.X, vector.Y).Length();
        _flyMoved = 0f;
        _flySpeed = 0f;
        _movingInFly = false;

        if (_velocity == 0f)
        {
            _jumpDelay = HangTime;
            _hang = 0f;
        }
        else
        {
            _jumpDelay = 0f;
            _hang = HangTime;
        }

        _groundDown = LeastLying + (LyingStep * random.Next(LyingSteps));
    }

    /// <summary>
    /// The monster is thrown up by a height; the total it climbs from the ground is capped.
    /// </summary>
    private void Lift(float wanted, bool airborne)
    {
        if (!airborne || _climbed < 0f)
        {
            _climbed = 0f;
        }

        float velocity;

        if (wanted + _climbed <= HighestLift)
        {
            velocity = MathF.Sqrt(wanted * 2f * Gravity);
        }
        else
        {
            var allowed = HighestLift - _climbed;

            velocity = allowed > 0f ? MathF.Sqrt(allowed * 2f * Gravity) : 0f;
        }

        if (wanted <= 0f)
        {
            _jumpTime = 0.5f;
            _previousLift = (_velocity * _jumpTime) - (0.5f * Gravity * _jumpTime * _jumpTime);
            _direction = 0f;
        }
        else
        {
            _velocity = velocity;
            _jumpHeight = wanted;
            _jumpTime = 0f;
            _previousLift = 0f;
            _direction = 1f;
        }
    }

    // ---- Time ---------------------------------------------------------------------------------

    /// <summary>
    /// Moves the monster on by a time: the slide, the flight, the fall, and the animations that follow.
    /// </summary>
    public ReactionStep Step(float seconds, Vector3 position)
    {
        var events = ReactionEvents.None;

        Anchor(position);

        if (_sliding)
        {
            position = SlideStep(seconds, position, ref events);
        }

        switch (_phase)
        {
            case Phase.Air:
                position = Fly(seconds, position, ref events);
                break;
            case Phase.Landing:
                if (Wait(seconds))
                {
                    LieDown(ref events);
                }

                break;
            case Phase.Lying:
                _groundDown -= seconds;

                if (_groundDown <= 0f)
                {
                    Rise(ref events);
                }

                break;
            case Phase.Idle:
                break;
            default:
                if (Wait(seconds))
                {
                    End(ref events);
                }

                break;
        }

        return new ReactionStep(position, events, _motion);
    }

    private bool Wait(float seconds)
    {
        _left -= seconds;

        return _left <= 0f;
    }

    private void End(ref ReactionEvents events)
    {
        _phase = Phase.Idle;
        _motion = MotionClass.Idle;
        _left = 0f;
        events |= ReactionEvents.Ended;
    }

    private void LieDown(ref ReactionEvents events)
    {
        // A monster whose file has no lying animation does not lie.
        if (lengthOf(Family(_motion) + "_Down") <= 0f)
        {
            _groundDown = 0f;
        }

        _phase = Phase.Lying;

        if (_groundDown <= 0f)
        {
            Rise(ref events);
        }
    }

    private void Rise(ref ReactionEvents events)
    {
        _left = lengthOf(Family(_motion) + "_Raise");

        if (_left <= 0f)
        {
            End(ref events);

            return;
        }

        _phase = Phase.Rising;
        events |= ReactionEvents.RaiseBegan;
    }

    private Vector3 Fly(float seconds, Vector3 position, ref ReactionEvents events)
    {
        _airTime += seconds;

        if (!_flying)
        {
            // Knocked down with nowhere to fall: the animation of the throw plays and the monster lies.
            if (Wait(seconds))
            {
                _phase = Phase.Landing;
                _left = lengthOf(Family(_motion) + "_End");
                events |= ReactionEvents.Landed;
            }

            return position;
        }

        UpdateLift(seconds);

        if (_movingInFly)
        {
            position = FlyAlong(seconds, position, ref events);
        }

        var floor = FloorAt(position);

        // Down when the fall has brought the monster to the floor, or when it has been up for too long.
        if ((_height <= Airborne && _direction < 0f) || _airTime > LoopLimit + lengthOf(Family(_motion) + "_Start"))
        {
            _height = 0f;
            _flying = false;
            _movingInFly = false;
            _climbed = 0f;
            _phase = Phase.Landing;
            _left = lengthOf(Family(_motion) + "_End");
            events |= ReactionEvents.Landed;

            StopSlide();
        }

        return position with { Z = floor + _height };
    }

    private void UpdateLift(float seconds)
    {
        if (_jumpDelay > 0f)
        {
            _jumpDelay -= seconds;

            if (_jumpDelay >= 0f)
            {
                return;
            }

            seconds += _jumpDelay;
        }

        _jumpTime += seconds;

        var lift = (_velocity * _jumpTime) - (0.5f * Gravity * _jumpTime * _jumpTime);
        var change = lift - _previousLift;

        _previousLift = lift;
        _direction = change;

        if (change <= 0f && _hang > 0f)
        {
            _jumpDelay = _hang;
            _hang = 0f;
        }

        if (change >= 0f || _height + change > 0f)
        {
            _height += change;
        }
        else
        {
            _height = 0f;
            _climbed = 0f;
            _direction = -1f;

            return;
        }

        _climbed += change;
    }

    private Vector3 FlyAlong(float seconds, Vector3 position, ref ReactionEvents events)
    {
        var step = seconds * _flySpeed;

        if (_flyDistance > _flyMoved)
        {
            step *= (_flyDistance - _flyMoved) / _flyDistance * 2f;

            if (_flyMoved + step < _flyDistance)
            {
                _flyMoved += step;
            }
            else
            {
                step = _flyDistance - _flyMoved;
                _flyMoved = _flyDistance;
                _flySpeed = 0f;
            }
        }

        if (step <= 0f)
        {
            return position;
        }

        var direction = AiMath.Direction(_flyYaw);
        var target = new Vector3(position.X + (direction.X * step), position.Y + (direction.Y * step), _baseFloor);
        var reached = ground.Clamp(new Vector3(position.X, position.Y, FloorAt(position)), target, Math.Min(profile.Radius, MaxWayRadius), out var arrived);

        if (!arrived)
        {
            _movingInFly = false;

            StopSlide();

            events |= ReactionEvents.FlightCut;
            target = reached;
        }

        return new Vector3(target.X, target.Y, position.Z);
    }

    private float FloorAt(Vector3 position)
    {
        if (ground.TryGetFloor(new Vector3(position.X, position.Y, _baseFloor), out var floor))
        {
            _baseFloor = floor;
        }

        return _baseFloor;
    }

    // ---- The slide ----------------------------------------------------------------------------

    /// <summary>
    /// A flinch pushes the monster along the floor: it goes to where the push ends within a tenth of a second, and has a little
    /// longer to get there.
    /// </summary>
    private void Slide(Vector2 offset, Vector3 position)
    {
        var start = _sliding ? _slideTarget : new Vector2(position.X, position.Y);
        var wanted = new Vector3(start.X + offset.X, start.Y + offset.Y, position.Z);
        var reached = ground.Clamp(new Vector3(position.X, position.Y, FloorAt(position)), wanted, Math.Min(profile.Radius, MaxWayRadius), out _);

        _slideTarget = new Vector2(reached.X, reached.Y);
        _slideTime = Math.Max(_slideTime, SlideTime);
        _slideLeft = _slideTime + SlideTail;
        _sliding = true;
    }

    private Vector3 SlideStep(float seconds, Vector3 position, ref ReactionEvents events)
    {
        if (_slideLeft <= 0f)
        {
            StopSlide();

            return new Vector3(_slideTarget.X, _slideTarget.Y, position.Z);
        }

        var difference = _slideTarget - new Vector2(position.X, position.Y);

        if (MathF.Abs(difference.X) < SlideStop && MathF.Abs(difference.Y) < SlideStop)
        {
            StopSlide();

            return position;
        }

        var share = seconds / _slideTime;
        var step = new Vector2(Toward(difference.X, share), Toward(difference.Y, share));
        var next = new Vector3(position.X + step.X, position.Y + step.Y, position.Z);

        _slideLeft -= seconds;

        if (IsBlocked?.Invoke(next) == true)
        {
            StopSlide();
            events |= ReactionEvents.SlideBlocked;

            return position;
        }

        var floor = FloorAt(next);

        return next with { Z = floor };
    }

    /// <summary>
    /// A share of the way left, never past it.
    /// </summary>
    private static float Toward(float difference, float share)
    {
        var step = difference * share;

        return difference > 0f ? Math.Min(difference, step) : Math.Max(difference, step);
    }

    // ---- Names --------------------------------------------------------------------------------

    /// <summary>
    /// The animations of a way of being knocked down all start with this.
    /// </summary>
    private static string Family(short motion)
    {
        return motion switch
        {
            MotionClass.DownUp => "B_KD_Upp",
            MotionClass.DownForward => "B_KD_Ham_F",
            MotionClass.DownBackward => "B_KD_Ham_B",
            _ => "B_KD_Str"
        };
    }
}
