using System.Numerics;
using System.Text;

namespace OpenWorker.Batch.Actions;

/// <param name="Repeats">Whether the animation starts over by itself until something stops it, like the waiting and the loop of a skill.</param>
/// <param name="Hides">The moments of the animation at which the monster hides, if it does.</param>
/// <param name="Defense">The defense type the animation gives whoever plays it, see <see cref="ActionDefense"/>.</param>
/// <param name="Defenses">The stretches of the animation in which the defense type changes.</param>
/// <param name="Detaches">The moments at which the animation lets go of a grabbed target.</param>
/// <param name="Attaches">The moments at which the animation starts to carry away what stands before it.</param>
public sealed record ActionAnimation(
    string Name,
    float Length,
    IReadOnlyList<AttackTrigger> Attacks,
    bool Repeats = false,
    IReadOnlyList<InvisibleEvent>? Hides = null,
    byte Defense = ActionDefense.Unchanged,
    IReadOnlyList<DefenseEvent>? Defenses = null,
    IReadOnlyList<DetachEvent>? Detaches = null,
    IReadOnlyList<AttachEvent>? Attaches = null);

/// <summary>
/// What an animation says about the defense type of whoever plays it. The numbers are the ones of the file.
/// </summary>
public static class ActionDefense
{
    /// <summary>
    /// Whoever plays the animation is hit as a plain target would be, whatever its table says.
    /// </summary>
    public const byte Normal = 0;

    public const byte SuperArmor = 2;
    public const byte Invincible = 3;

    /// <summary>
    /// The animation says nothing: the defense type of whoever plays it stays what its table gives.
    /// </summary>
    public const byte Unchanged = 4;
}

/// <summary>
/// A stretch of an animation in which the defense type is something else, as the event of the file says.
/// </summary>
/// <param name="Start">Seconds from the start of the animation.</param>
/// <param name="End">Seconds from the start of the animation.</param>
/// <param name="Defense">The defense type, see <see cref="ActionDefense"/> and the defense types of a creature.</param>
/// <param name="Immune">Nothing that harms by effect sticks while it lasts.</param>
public sealed record DefenseEvent(float Start, float End, byte Defense, bool Immune);

/// <summary>
/// The moment a monster that holds a target lets it go: the target flies to the place the grab named in the time given, and
/// the blow named by the event of the animation after it lands on the target there.
/// </summary>
/// <param name="Start">Seconds from the start of the animation.</param>
/// <param name="DropTime">How long the target takes to fly to the place.</param>
/// <param name="Animation">The animation that holds the blow that follows.</param>
/// <param name="Event">The place of the blow among the attacks of that animation, counted from zero.</param>
public sealed record DetachEvent(float Start, float DropTime, string Animation, int Event);

/// <summary>
/// For a time the one who plays the animation carries along whatever stands before it and is not too heavy.
/// </summary>
/// <param name="Start">Seconds from the start of the animation.</param>
/// <param name="Duration">How long it looks for something to carry.</param>
/// <param name="Distance">How far before it a target has to stand.</param>
/// <param name="Angle">How wide before it, in degrees.</param>
/// <param name="Offset">Where the carried targets are put, from the carrier, turned by its yaw.</param>
/// <param name="WeightRank">A target of this rank or a higher one cannot be carried and stops the carrier.</param>
public sealed record AttachEvent(float Start, float Duration, float Distance, float Angle, Vector3 Offset, int WeightRank);

/// <summary>
/// The moment the akashic skill of a hero brings a called card into the fight, and where.
/// </summary>
/// <param name="Start">Seconds from the start of the animation, as the file has it.</param>
/// <param name="Offset">Where the card appears, from the hero, before it is turned by the hero's yaw.</param>
/// <param name="Alpha">How visible the card is when it appears, for the clients.</param>
/// <param name="Blend">How long the card takes to appear, for the clients.</param>
/// <param name="Rotate">Whether the card is turned the way the hero faces.</param>
public sealed record AkashicSummon(float Start, Vector3 Offset, float Alpha, float Blend, bool Rotate);

/// <summary>
/// The monster disappears at a moment of an animation: for a time, or until its life is down to a share.
/// </summary>
/// <param name="Start">Seconds from the start of the animation.</param>
/// <param name="Flags">What the monster keeps while it is gone: 1 its collision, 2 its effects, 4 its movement, 8 its mind.</param>
/// <param name="ConditionType">What ends it: 0 a time, 1 a share of the life, 2 the death of a monster it summoned, 3 the death of every monster of the given tables.</param>
/// <param name="Value">The time in milliseconds, the share of the life in thousandths, or the number of summoned monsters to wait for.</param>
/// <param name="Waits">For the kind that waits for monsters of the maze: their tables.</param>
public sealed record InvisibleEvent(float Start, byte Flags, int ConditionType, int Value, IReadOnlyList<int>? Waits = null)
{
    public const byte KeepsMind = 8;

    public const int ForTime = 0;
    public const int UntilHurt = 1;
    public const int UntilSummonsDie = 2;
    public const int UntilMonstersDie = 3;
}

/// <summary>
/// An action file of the client: what happens during each animation of a character.
/// Only attacks are kept, everything else concerns the picture and the sound.
/// </summary>
public sealed class ActionFile
{
    private const string Magic = "VBIN";
    private const string RootTag = "TRIG";
    private const string SequenceTag = "SEQU";
    private const short AttackTriggerType = 3;
    private const short InvisibleTriggerType = 14;
    private const short AkashicTriggerType = 18;
    private const short AttachTriggerType = 22;
    private const short DefenseTriggerType = 25;
    private const short DetachTriggerType = 30;

    private const short FiltersSinceVersion = 204;
    private const short CollisionRangeSinceVersion = 205;
    private const short PenetrationSinceVersion = 207;
    private const short ExplodeFactionSinceVersion = 211;
    private const short ExplodeTrapSinceVersion = 214;
    private const short PenetrationEffectSinceVersion = 215;
    private const short ReactionAngleSinceVersion = 216;
    private const short RandomShootSinceVersion = 221;
    private const short ChainRecalculationSinceVersion = 222;
    private const short RandomRadiusMinSinceVersion = 210;
    private const short TargetOnlySinceVersion = 218;
    private const short EffectVisibilitySinceVersion = 255;
    private const short ThirdFilterSinceVersion = 254;
    private const short AnimationFieldSinceVersion = 235;
    private const short RotationFramesSinceVersion = 241;
    private const short ConnectionEffectSinceVersion = 223;
    private const short PartyBreakSinceVersion = 225;
    private const short ConnectionBonesSinceVersion = 226;
    private const short InvisibleWithoutTargetSinceVersion = 227;
    private const short TargetPrioritySinceVersion = 230;
    private const short OrientationSinceVersion = 236;
    private const short ContinuousMeleeSinceVersion = 239;
    private const short GroupSinceVersion = 240;
    private const short AuraSinceVersion = 242;
    private const short AttachRankSinceVersion = 231;
    private const short ImmunitySinceVersion = 237;

    private ActionFile(Dictionary<string, ActionAnimation> animations, Dictionary<string, int> indexes)
    {
        Animations = animations;
        Indexes = indexes;
    }

    public IReadOnlyDictionary<string, ActionAnimation> Animations { get; }

    /// <summary>
    /// The place of every animation in the file: that is the number the clients use to name one.
    /// </summary>
    public IReadOnlyDictionary<string, int> Indexes { get; }

    public static ActionFile Load(string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));

        return Read(stream);
    }

    /// <exception cref="InvalidDataException">The stream is not an action file.</exception>
    /// <exception cref="EndOfStreamException">The file ends too early.</exception>
    public static ActionFile Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.Latin1, leaveOpen: true);

        if (Encoding.Latin1.GetString(reader.ReadBytes(Magic.Length)) != Magic)
        {
            throw new InvalidDataException("Not an action file.");
        }

        // The version of the container, the one that matters is inside.
        reader.ReadInt32();

        Require(Open(reader), RootTag);

        var version = reader.ReadInt16();

        var head = Open(reader);
        var count = reader.ReadInt32();
        Close(reader, head);

        Close(reader, Open(reader));

        Require(Open(reader), SequenceTag);

        var animations = new Dictionary<string, ActionAnimation>(count, StringComparer.OrdinalIgnoreCase);
        var indexes = new Dictionary<string, int>(count, StringComparer.OrdinalIgnoreCase);

        // The client numbers the attacks of a file itself as it loads it, and names an attack by that number in the
        // messages of a hit: one after the other through the animations in the order of the file, starting at 1, and
        // after an animation that has attacks, on from the next block of fifty (51, 101, ...). The number written in the
        // event is not the one.
        var next = 1;

        for (var i = 0; i < count; i++)
        {
            var animation = ReadAnimation(reader, version);

            if (animation.Attacks.Count > 0)
            {
                var numbered = new List<AttackTrigger>(animation.Attacks.Count);

                foreach (var attack in animation.Attacks)
                {
                    numbered.Add(attack with { Id = (short)next++ });
                }

                next += 51 - (short)next % 50;
                animation = animation with { Attacks = numbered };
            }

            if (animations.TryAdd(animation.Name, animation))
            {
                indexes.Add(animation.Name, i);
            }
        }

        return new ActionFile(animations, indexes);
    }

    /// <summary>
    /// Reads the akashic file of a hero: for each card group and animation of the akashic skill (the key
    /// <c>"group:animation"</c>), the events that bring the card in. It is built like an action file, with a list of events
    /// under each key instead of animations.
    /// </summary>
    /// <exception cref="InvalidDataException">The stream is not an action file.</exception>
    /// <exception cref="EndOfStreamException">The file ends too early.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<AkashicSummon>> ReadSummons(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.Latin1, leaveOpen: true);

        if (Encoding.Latin1.GetString(reader.ReadBytes(Magic.Length)) != Magic)
        {
            throw new InvalidDataException("Not an action file.");
        }

        reader.ReadInt32();

        Require(Open(reader), RootTag);

        var version = reader.ReadInt16();

        var head = Open(reader);
        var count = reader.ReadInt32();
        Close(reader, head);

        Require(Open(reader), SequenceTag);

        var summons = new Dictionary<string, IReadOnlyList<AkashicSummon>>(count, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < count; i++)
        {
            var key = ReadString(reader);
            var events = reader.ReadInt32();
            var list = new List<AkashicSummon>();

            for (var j = 0; j < events; j++)
            {
                var chunk = Open(reader);
                var header = ReadHeader(reader, version);

                if (header.Type == AkashicTriggerType)
                {
                    // Every number of the event is written as four bytes, the switch of the turn as one.
                    var alpha = reader.ReadSingle();
                    var blend = reader.ReadSingle();
                    var offset = ReadVector3(reader);

                    list.Add(new AkashicSummon(header.Start, offset, alpha, blend, reader.ReadBoolean()));
                }

                Close(reader, chunk);
            }

            summons.TryAdd(key, list);
        }

        return summons;
    }

    private static ActionAnimation ReadAnimation(BinaryReader reader, short version)
    {
        var name = ReadString(reader);

        // What the character may do while it plays, the kind of motion, the group, how the buffer of inputs behaves.
        reader.BaseStream.Seek(4 * sizeof(short), SeekOrigin.Current);

        var defense = reader.ReadInt16();

        reader.BaseStream.Seek(sizeof(float), SeekOrigin.Current);

        if (version >= AnimationFieldSinceVersion)
        {
            reader.ReadInt16();
        }

        var length = reader.ReadSingle();

        // Animations that play once are marked, those that repeat are not.
        var repeats = reader.ReadInt16() == 0;

        reader.ReadInt16();

        var attacks = new List<AttackTrigger>();
        var hides = new List<InvisibleEvent>();
        var defenses = new List<DefenseEvent>();
        var detaches = new List<DetachEvent>();
        var attaches = new List<AttachEvent>();
        var events = reader.ReadInt32();

        for (var i = 0; i < events; i++)
        {
            var chunk = Open(reader);
            var header = ReadHeader(reader, version);

            switch (header.Type)
            {
                case AttackTriggerType:
                    attacks.Add(ReadAttack(reader, version, header));
                    break;

                case InvisibleTriggerType:
                    hides.Add(ReadInvisible(reader, header));
                    break;

                case DefenseTriggerType:
                    defenses.Add(ReadDefense(reader, version, header));
                    break;

                case DetachTriggerType:
                    detaches.Add(ReadDetach(reader, header));
                    break;

                case AttachTriggerType:
                    attaches.Add(ReadAttach(reader, version, header));
                    break;
            }

            Close(reader, chunk);
        }

        SkipFrames(reader);
        SkipFrames(reader);

        if (version >= RotationFramesSinceVersion)
        {
            SkipFrames(reader);
        }

        return new ActionAnimation(name, length, attacks, repeats, hides, (byte)Math.Clamp((int)defense, byte.MinValue, byte.MaxValue), defenses, detaches, attaches);
    }

    /// <summary>
    /// What every event starts with: its kind, name, number, the moments it begins and ends, and the filters of the modes it
    /// is for.
    /// </summary>
    private readonly record struct EventHeader(short Type, int Id, float Start, float End);

    private static EventHeader ReadHeader(BinaryReader reader, short version)
    {
        var type = reader.ReadInt16();

        ReadString(reader);

        var id = reader.ReadInt32();
        var start = reader.ReadSingle();
        var end = reader.ReadSingle();

        if (version >= ThirdFilterSinceVersion)
        {
            Skip(reader, 3 * sizeof(int));
        }
        else if (version >= FiltersSinceVersion)
        {
            Skip(reader, 2 * sizeof(int));
        }

        return new EventHeader(type, id, start, end);
    }

    /// <summary>
    /// An event that hides the monster: four switches for what it keeps, then what ends it.
    /// </summary>
    private static InvisibleEvent ReadInvisible(BinaryReader reader, in EventHeader header)
    {
        var flags = 0;

        for (var bit = 0; bit < 4; bit++)
        {
            flags |= reader.ReadBoolean() ? 1 << bit : 0;
        }

        var type = reader.ReadInt32();
        var value = reader.ReadInt32();
        var waits = new[] { value, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() };

        return new InvisibleEvent(header.Start, (byte)flags, type, value, type == InvisibleEvent.UntilMonstersDie ? waits.Where(e => e != 0).ToArray() : null);
    }

    private static DefenseEvent ReadDefense(BinaryReader reader, short version, in EventHeader header)
    {
        var defense = reader.ReadInt16();
        var immune = false;

        if (version >= ImmunitySinceVersion)
        {
            immune = reader.ReadBoolean();

            // The effect that shows when a blow is turned away.
            reader.ReadInt32();
        }

        return new DefenseEvent(header.Start, header.End, (byte)Math.Clamp((int)defense, byte.MinValue, byte.MaxValue), immune);
    }

    private static DetachEvent ReadDetach(BinaryReader reader, in EventHeader header)
    {
        var dropTime = reader.ReadSingle();
        var animation = ReadString(reader);

        // The skill it belongs to, which the animation of the monster does not need.
        reader.ReadInt32();

        return new DetachEvent(header.Start, dropTime, animation, reader.ReadInt32());
    }

    private static AttachEvent ReadAttach(BinaryReader reader, short version, in EventHeader header)
    {
        var duration = reader.ReadSingle();
        var distance = reader.ReadSingle();
        var angle = reader.ReadSingle();
        var offset = ReadVector3(reader);
        var rank = version >= AttachRankSinceVersion ? reader.ReadInt32() : 0;

        return new AttachEvent(header.Start, duration, distance, angle, offset, rank);
    }

    private static AttackTrigger ReadAttack(BinaryReader reader, short version, in EventHeader header)
    {
        var id = header.Id;
        var start = header.Start;

        var rangeType = reader.ReadInt16();
        var attackType = reader.ReadInt16();
        var hardness = reader.ReadInt16();
        var spawnObject = reader.ReadInt32();

        if (version < FiltersSinceVersion)
        {
            reader.BaseStream.Seek(2 * sizeof(int) + sizeof(short) + sizeof(int), SeekOrigin.Current);
        }

        var angle = reader.ReadSingle();
        var center = ReadVector3(reader);
        var boxX = reader.ReadSingle();
        var boxY = reader.ReadSingle();
        var radius = reader.ReadSingle();
        var height = reader.ReadSingle();
        var startPosition = reader.ReadSingle();

        var (projectile, chain, reaction) = ReadProjectile(reader, version);

        return new AttackTrigger(id, rangeType, attackType, spawnObject, start, angle, center, boxX, boxY, radius, height, startPosition,
            reaction.DamageRate, reaction.ReactionType, projectile, chain, reaction.Buff, reaction.TargetType, reaction.Aura,
            reaction.Push with { Hardness = hardness }, reaction.GrabDrop);
    }

    /// <summary>
    /// Whom an attack is for and what it does to them: the kind of target, the share of the damage, the buff it gives each
    /// target and how the target reacts.
    /// </summary>
    private readonly record struct Effects(int TargetType, float DamageRate, int Buff, int ReactionType, int Aura, AttackReaction Push, Vector3 GrabDrop);

    /// <summary>
    /// The aura an attack puts on its targets sits at the end of the attack, after the fields that later versions added to it.
    /// A blow that breaks the super armor of a player is told on the way.
    /// </summary>
    private static (int Aura, bool BreaksPlayerArmor) ReadAura(BinaryReader reader, short version)
    {
        var breaksPlayerArmor = false;

        if (version >= ConnectionEffectSinceVersion)
        {
            SkipStrings(reader, 1);
        }

        if (version >= PartyBreakSinceVersion)
        {
            breaksPlayerArmor = reader.ReadBoolean();
        }

        if (version >= ConnectionBonesSinceVersion)
        {
            SkipStrings(reader, 2);
        }

        SkipSince(reader, version, InvisibleWithoutTargetSinceVersion, 1);
        SkipSince(reader, version, TargetPrioritySinceVersion, 2 * sizeof(int));
        SkipSince(reader, version, OrientationSinceVersion, 2);

        if (version >= ContinuousMeleeSinceVersion)
        {
            Skip(reader, 2 * sizeof(int));
            SkipStrings(reader, 1);
        }

        SkipSince(reader, version, GroupSinceVersion, sizeof(short));

        return (version >= AuraSinceVersion ? reader.ReadInt32() : 0, breaksPlayerArmor);
    }

    private static (ProjectileInfo Projectile, ChainInfo Chain, Effects Effects) ReadProjectile(BinaryReader reader, short version)
    {
        var start = ReadVector3(reader);
        var speed = reader.ReadSingle();
        var lifeTime = reader.ReadSingle();
        var hasGravity = reader.ReadBoolean();
        var directionType = reader.ReadInt32();

        reader.ReadBoolean();

        var radius = reader.ReadSingle();
        var isContinuous = reader.ReadBoolean();
        var continuousTime = reader.ReadSingle();

        reader.ReadSingle();

        var isGuided = reader.ReadBoolean();

        Skip(reader, 2 * sizeof(float) + sizeof(short) + 2);

        var targetOnly = version >= TargetOnlySinceVersion && reader.ReadBoolean();

        Skip(reader, sizeof(short) + sizeof(float) + 2);

        var explodeDecision = reader.ReadInt16();
        var direction = ReadVector3(reader);

        SkipStrings(reader, 5);
        Skip(reader, 1 + sizeof(short) + sizeof(float));

        if (version >= RandomRadiusMinSinceVersion)
        {
            reader.ReadSingle();
        }

        Skip(reader, sizeof(float) + 3 * sizeof(float) + sizeof(int) + sizeof(float) + sizeof(int));

        if (version >= EffectVisibilitySinceVersion)
        {
            reader.ReadBoolean();
        }

        // What a hit looks like.
        Skip(reader, 1 + sizeof(float) + sizeof(int) + sizeof(float) + sizeof(int) + sizeof(float));
        // Which targets it may affect: the kind of target, then the states of the targets it reaches.
        var targetType = reader.ReadInt32();

        reader.ReadInt32();

        var damageRate = reader.ReadSingle();
        var buff = reader.ReadInt32();
        var reactionType = reader.ReadInt32();

        // How a target reacts: how far, how high and how fast the blow throws it, which way, and how a weak blow slows it.
        var distance = reader.ReadSingle();
        var pushHeight = reader.ReadSingle();
        var pushSpeed = reader.ReadSingle();
        var arrow = reader.ReadInt32();
        var heightInAir = reader.ReadSingle();
        var slowRate = reader.ReadSingle();
        var slowTime = reader.ReadSingle();
        var slowDelay = reader.ReadSingle();
        var hitFreeze = reader.ReadSingle();
        var weakSlowChance = reader.ReadInt16();

        var connectionType = reader.ReadInt32();
        var chainLifeTime = reader.ReadSingle();
        var chainMaxCount = (int)reader.ReadSingle();
        var damageMultiple = reader.ReadSingle();

        reader.ReadSingle();

        var chainCenter = ReadVector3(reader);

        Skip(reader, 2 * sizeof(float));

        var chainRadius = reader.ReadSingle();

        reader.ReadSingle();

        var chainStartPosition = reader.ReadSingle();

        SkipStrings(reader, 1);

        var grade = reader.ReadInt32();
        var usesTargetWeight = reader.ReadBoolean();

        Skip(reader, sizeof(float));
        Skip(reader, 1 + 3 * sizeof(int) + 2 * sizeof(float));

        if (version < FiltersSinceVersion)
        {
            SkipStrings(reader, 1);
        }

        // What it does to a grabbed target and how it sounds: the bone that holds the target, where on it, the sounds, the
        // turn of the target, and the place it is let go at, from the grabber.
        SkipStrings(reader, 1);
        Skip(reader, 3 * sizeof(float));
        SkipStrings(reader, 2);
        Skip(reader, 3 * sizeof(float));

        var grabDrop = ReadVector3(reader);

        if (version >= CollisionRangeSinceVersion)
        {
            Skip(reader, 9 * sizeof(float) + sizeof(float));
        }

        var penetrates = false;
        var penetrateCount = 0;

        if (version >= PenetrationSinceVersion)
        {
            penetrates = reader.ReadBoolean();
            penetrateCount = reader.ReadInt32();
        }

        SkipSince(reader, version, ExplodeFactionSinceVersion, sizeof(short));
        SkipSince(reader, version, ExplodeTrapSinceVersion, sizeof(short));
        SkipSince(reader, version, PenetrationEffectSinceVersion, 1);
        var angle = version >= ReactionAngleSinceVersion ? reader.ReadSingle() : 0f;
        SkipSince(reader, version, RandomShootSinceVersion, 2 * sizeof(short));

        var recalculates = false;

        if (version >= ChainRecalculationSinceVersion)
        {
            // Whether the chain looks like a laser, which only matters for the picture.
            reader.ReadBoolean();
            recalculates = reader.ReadBoolean();
        }

        var projectile = new ProjectileInfo(start, speed, lifeTime, hasGravity, directionType, radius, isContinuous, continuousTime, isGuided, targetOnly, explodeDecision, direction, penetrates, penetrateCount);
        var chain = new ChainInfo(connectionType, chainLifeTime, chainMaxCount, damageMultiple, chainCenter, chainRadius, chainStartPosition, recalculates);

        var (aura, breaksPlayerArmor) = ReadAura(reader, version);
        var push = new AttackReaction(distance, pushHeight, heightInAir, pushSpeed, arrow, angle, slowRate, slowTime, slowDelay, hitFreeze, weakSlowChance, grade, usesTargetWeight, 0, breaksPlayerArmor);

        return (projectile, chain, new Effects(targetType, damageRate, buff, reactionType, aura, push, grabDrop));
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static void Skip(BinaryReader reader, int bytes)
    {
        reader.BaseStream.Seek(bytes, SeekOrigin.Current);
    }

    private static void SkipSince(BinaryReader reader, short version, short since, int bytes)
    {
        if (version >= since)
        {
            Skip(reader, bytes);
        }
    }

    private static void SkipStrings(BinaryReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            ReadString(reader);
        }
    }

    private static void SkipFrames(BinaryReader reader)
    {
        if (reader.ReadInt32() > 0)
        {
            Close(reader, Open(reader));
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(reader.ReadInt32());

        return Encoding.Latin1.GetString(bytes);
    }

    /// <summary>
    /// A chunk is its nesting depth, a tag and a size, and closes with the depth and the tag again.
    /// </summary>
    private static Chunk Open(BinaryReader reader)
    {
        var depth = reader.ReadInt32();

        if (depth < 0)
        {
            throw new InvalidDataException("The file ends where a chunk was expected.");
        }

        var tag = ReadTag(reader);
        var size = reader.ReadInt32();

        return new Chunk(tag, reader.BaseStream.Position + size);
    }

    private static void Close(BinaryReader reader, Chunk chunk)
    {
        reader.BaseStream.Position = chunk.End;

        reader.ReadInt32();
        ReadTag(reader);
    }

    private static string ReadTag(BinaryReader reader)
    {
        var tag = reader.ReadBytes(sizeof(int));

        Array.Reverse(tag);

        return Encoding.Latin1.GetString(tag);
    }

    private static void Require(Chunk chunk, string tag)
    {
        if (chunk.Tag != tag)
        {
            throw new InvalidDataException($"Expected a {tag} chunk, found {chunk.Tag}.");
        }
    }

    private readonly record struct Chunk(string Tag, long End);
}
