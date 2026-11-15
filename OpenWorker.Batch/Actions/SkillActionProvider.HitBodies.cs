using System.Xml;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Batch.Actions;

public sealed partial class SkillActionProvider
{
    private const string HitBodyExtension = ".xml";

    /// <summary>
    /// How many halvings find the time of an animation from a moment on the clock: far below one frame.
    /// </summary>
    private const int TimeSearchSteps = 24;

    private Dictionary<string, HitBody?> HitBodies { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The body a blow lands on for the characters of an action file, from the file of the same name beside it. Null when there
    /// is none: the cylinder of the table is the whole body then.
    /// </summary>
    public HitBody? GetHitBody(string actionFile)
    {
        if (HitBodies.TryGetValue(actionFile, out var body))
        {
            return body;
        }

        var path = Path.Join(Folder, actionFile + HitBodyExtension);

        try
        {
            using var stream = File.OpenRead(path);

            body = HitBody.Read(stream);
        }

        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            body = null;
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException or XmlException)
        {
            LogUnreadableBody(logger, e, actionFile, path);
        }

        HitBodies.Add(actionFile, body);

        return body;
    }

    /// <summary>
    /// The animation of an action file by its name, or null.
    /// </summary>
    public ActionAnimation? GetAnimation(string actionFile, string animation)
    {
        return GetFile(actionFile) is { } file ? Find(file, animation, false) : null;
    }

    /// <summary>
    /// How far an animation has played, in its own time, a moment after it began on the clock: the stretches that play at their
    /// own speed are counted as the clients count them.
    /// </summary>
    public static float AnimationTime(ActionAnimation animation, float wall)
    {
        if (!float.IsFinite(wall) || wall <= 0f)
        {
            return 0f;
        }

        var clock = new SkillPlayback.ClipClock(animation, 1f);

        if (wall >= clock.Length)
        {
            return animation.Length;
        }

        var low = 0f;
        var high = animation.Length;

        for (var i = 0; i < TimeSearchSteps; i++)
        {
            var middle = (low + high) / 2f;

            if (clock.Wall(middle) <= wall)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blows land on {Name} by the cylinder of its table only: cannot read {Path}")]
    private static partial void LogUnreadableBody(ILogger logger, Exception exception, string name, string path);
}
