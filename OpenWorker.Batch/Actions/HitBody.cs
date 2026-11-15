using System.Globalization;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// A bone of a character that a blow can land on: a ball of the given size around the bone.
/// </summary>
/// <param name="Radius">How far from the bone a blow still lands on it.</param>
/// <param name="Part">The part of the monster the bone belongs to; zero for none.</param>
/// <param name="Rest">Where the bone is, from the feet of the character, when no animation says.</param>
public sealed record HitBone(string Name, float Radius, byte Part, Vector3 Rest);

/// <summary>
/// A bone placed in the world at one moment, as a blow tests it.
/// </summary>
public readonly record struct BonePlace(Vector3 Center, float Radius, byte Part);

/// <summary>
/// The body a blow lands on, from the file the client keeps beside the action file of a character: a standing cylinder, used
/// instead of the one of the table when both of its sizes are set, and the bones that can be hit wherever the cylinder is.
/// </summary>
public sealed record HitBody(float CylinderRadius, float CylinderHeight, IReadOnlyList<HitBone> Bones)
{
    /// <summary>
    /// Whether the file gives the cylinder of the body.
    /// </summary>
    public bool HasCylinder => CylinderRadius > 0f && CylinderHeight > 0f;

    /// <summary>
    /// Reads the file. Values that are not numbers are taken as zero; a file that is not one gives null.
    /// </summary>
    public static HitBody? Read(Stream stream)
    {
        XDocument document;

        try
        {
            document = XDocument.Load(stream);
        }
        catch (XmlException)
        {
            return null;
        }

        if (document.Root is not { Name.LocalName: "HitCollision" } root)
        {
            return null;
        }

        var cylinder = root.Element("HitCollisionCylinder");
        var bones = root.Elements("HitCollisionData")
            .Select(e => new HitBone(
                (string?)e.Attribute("BoneName") ?? string.Empty,
                Math.Max(0f, Number(e, "Radius")),
                (byte)Math.Clamp((int)Number(e, "HitParts"), byte.MinValue, byte.MaxValue),
                new Vector3(Number(e, "BoneX"), Number(e, "BoneY"), Number(e, "BoneZ"))))
            .ToArray();

        return new HitBody(cylinder is null ? 0f : Number(cylinder, "CylinderRadius"), cylinder is null ? 0f : Number(cylinder, "CylinderHeight"), bones);
    }

    private static float Number(XElement element, string name)
    {
        return float.TryParse((string?)element.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value) ? value : 0f;
    }
}

/// <summary>
/// Where the hit bones of a character are in each frame of an animation, from its feet and before it is turned, thirty frames
/// a second.
/// </summary>
public sealed class BoneFrames(int bones, Vector3[] places)
{
    public const float FramesPerSecond = 30f;

    public int Bones { get; } = bones;

    public int Count => Bones == 0 ? 0 : places.Length / Bones;

    /// <summary>
    /// Where a bone is at a time of the animation: the frame of that time, the last one after the end. Zero for a bone the
    /// animation does not hold.
    /// </summary>
    public Vector3 At(int bone, float time)
    {
        if (bone < 0 || bone >= Bones || Count == 0)
        {
            return Vector3.Zero;
        }

        var frame = float.IsFinite(time) ? Math.Clamp((int)(time * FramesPerSecond), 0, Count - 1) : 0;

        return places[frame * Bones + bone];
    }
}
