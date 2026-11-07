using System.Numerics;
using System.Text;

namespace OpenWorker.Batch.Actions;

public sealed record ActionAnimation(string Name, float Length, IReadOnlyList<AttackTrigger> Attacks);

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
    private const short RandomRadiusMinSinceVersion = 210;
    private const short TargetOnlySinceVersion = 218;
    private const short EffectVisibilitySinceVersion = 255;
    private const short ThirdFilterSinceVersion = 254;
    private const short AnimationFieldSinceVersion = 235;
    private const short RotationFramesSinceVersion = 241;

    private ActionFile(Dictionary<string, ActionAnimation> animations)
    {
        Animations = animations;
    }

    public IReadOnlyDictionary<string, ActionAnimation> Animations { get; }

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

        for (var i = 0; i < count; i++)
        {
            var animation = ReadAnimation(reader, version);

            animations.TryAdd(animation.Name, animation);
        }

        return new ActionFile(animations);
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

        reader.BaseStream.Seek(2 * sizeof(short), SeekOrigin.Current);

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

        return new ActionAnimation(name, length, attacks);
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

        var (projectile, damageRate, reactionType) = ReadProjectile(reader, version);

        return new AttackTrigger(id, rangeType, attackType, spawnObject, start, angle, center, boxX, boxY, radius, height, startPosition, damageRate, reactionType, projectile);
    }

    private static (ProjectileInfo Projectile, float DamageRate, int ReactionType) ReadProjectile(BinaryReader reader, short version)
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
        // Which targets it may affect.
        Skip(reader, 2 * sizeof(int));

        var damageRate = reader.ReadSingle();

        reader.ReadInt32();

        var reactionType = reader.ReadInt32();

        // How a target reacts, then what a hit passes on to others.
        Skip(reader, 3 * sizeof(float) + sizeof(int) + 5 * sizeof(float) + sizeof(short));
        Skip(reader, sizeof(int) + 3 * sizeof(float) + 9 * sizeof(float));
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

        var projectile = new ProjectileInfo(start, speed, lifeTime, hasGravity, directionType, radius, isContinuous, continuousTime, isGuided, targetOnly, explodeDecision, direction, penetrates, penetrateCount);

        return (projectile, damageRate, reactionType);
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static void Skip(BinaryReader reader, int bytes)
    {
        reader.BaseStream.Seek(bytes, SeekOrigin.Current);
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
