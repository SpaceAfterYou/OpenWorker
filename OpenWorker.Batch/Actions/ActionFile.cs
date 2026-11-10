using System.Numerics;
using System.Text;

namespace OpenWorker.Batch.Actions;

/// <param name="Repeats">Whether the animation starts over by itself until something stops it, like the waiting and the loop of a skill.</param>
public sealed record ActionAnimation(string Name, float Length, IReadOnlyList<AttackTrigger> Attacks, bool Repeats = false);

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

    private static ActionAnimation ReadAnimation(BinaryReader reader, short version)
    {
        var name = ReadString(reader);

        reader.BaseStream.Seek(5 * sizeof(short) + sizeof(float), SeekOrigin.Current);

        if (version >= AnimationFieldSinceVersion)
        {
            reader.ReadInt16();
        }

        var length = reader.ReadSingle();

        // Animations that play once are marked, those that repeat are not.
        var repeats = reader.ReadInt16() == 0;

        reader.ReadInt16();

        var attacks = new List<AttackTrigger>();
        var events = reader.ReadInt32();

        for (var i = 0; i < events; i++)
        {
            var chunk = Open(reader);

            if (ReadEvent(reader, version) is { } attack)
            {
                attacks.Add(attack);
            }

            Close(reader, chunk);
        }

        SkipFrames(reader);
        SkipFrames(reader);

        if (version >= RotationFramesSinceVersion)
        {
            SkipFrames(reader);
        }

        return new ActionAnimation(name, length, attacks, repeats);
    }

    private static AttackTrigger? ReadEvent(BinaryReader reader, short version)
    {
        var type = reader.ReadInt16();

        ReadString(reader);

        var id = reader.ReadInt32();
        var start = reader.ReadSingle();

        reader.ReadSingle();

        if (version >= ThirdFilterSinceVersion)
        {
            reader.BaseStream.Seek(3 * sizeof(int), SeekOrigin.Current);
        }
        else if (version >= FiltersSinceVersion)
        {
            reader.BaseStream.Seek(2 * sizeof(int), SeekOrigin.Current);
        }

        if (type != AttackTriggerType)
        {
            return null;
        }

        var rangeType = reader.ReadInt16();
        var attackType = reader.ReadInt16();

        reader.ReadInt16();

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
            reaction.DamageRate, reaction.ReactionType, projectile, chain, reaction.Buff, reaction.TargetType, reaction.Aura);
    }

    /// <summary>
    /// Whom an attack is for and what it does to them: the kind of target, the share of the damage, the buff it gives each
    /// target and how the target reacts.
    /// </summary>
    private readonly record struct Reaction(int TargetType, float DamageRate, int Buff, int ReactionType, int Aura);

    /// <summary>
    /// The aura an attack puts on its targets sits at the end of the attack, after the fields that later versions added to it.
    /// </summary>
    private static int ReadAura(BinaryReader reader, short version)
    {
        if (version < AuraSinceVersion)
        {
            return 0;
        }

        if (version >= ConnectionEffectSinceVersion)
        {
            SkipStrings(reader, 1);
        }

        SkipSince(reader, version, PartyBreakSinceVersion, 1);

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

        return reader.ReadInt32();
    }

    private static (ProjectileInfo Projectile, ChainInfo Chain, Reaction Reaction) ReadProjectile(BinaryReader reader, short version)
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

        // How a target reacts.
        Skip(reader, 3 * sizeof(float) + sizeof(int) + 5 * sizeof(float) + sizeof(short));

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
        Skip(reader, sizeof(int) + 1 + sizeof(float));
        Skip(reader, 1 + 3 * sizeof(int) + 2 * sizeof(float));

        if (version < FiltersSinceVersion)
        {
            SkipStrings(reader, 1);
        }

        // What it does to a grabbed target and how it sounds.
        SkipStrings(reader, 1);
        Skip(reader, 3 * sizeof(float));
        SkipStrings(reader, 2);
        Skip(reader, 6 * sizeof(float));

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
        SkipSince(reader, version, ReactionAngleSinceVersion, sizeof(float));
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

        return (projectile, chain, new Reaction(targetType, damageRate, buff, reactionType, ReadAura(reader, version)));
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
