using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OpenWorker.Batch;

public sealed record VBatchFile
{
    /// <summary>
    /// The layer every map of a batch has; the boxes of the other layers belong to one difficulty or one stage of the maps that
    /// share the batch.
    /// </summary>
    public const string CommonLayer = "Common";

    /// <summary>
    /// The layers of a batch by the difficulty of the maze that plays it (the client's <c>tb_Maze_Info</c>): the normal and the
    /// very hard episodes of a story, the casual raids, then the twenty-five stages a batch of the infinite tower holds.
    /// </summary>
    private static readonly string[] Layers =
    [
        "Normal", "VeryHard", "Expert",
        .. Enumerable.Range(1, 25).Select(e => $"Stage{e:00}")
    ];

    private VBatchFile(XNode document)
    {
        var eventBox = document.XPathSelectElement("root/Batchs [@eventtype='EventBox']");
        EventBox = new EventBox(eventBox ?? throw new InvalidDataException("The batch file has no EventBox batches."));

        var eventPoint = document.XPathSelectElement("root/Batchs [@eventtype='EventPoint']");
        EventPoint = new EventPoint(eventPoint ?? throw new InvalidDataException("The batch file has no EventPoint batches."));
    }

    public EventBox EventBox { get; }
    public EventPoint EventPoint { get; }

    /// <summary>
    /// The layer a maze of the difficulty plays, or null for a difficulty past the known ones.
    /// </summary>
    public static string? LayerOf(int difficulty) => difficulty >= 0 && difficulty < Layers.Length ? Layers[difficulty] : null;

    /// <summary>
    /// False when the client data has no batch of that name, which a server gets wrong by installing it wrong. With a
    /// <paramref name="layer"/>, only the boxes of the common layer and of that one are kept.
    /// </summary>
    public static bool TryCreate(string name, [NotNullWhen(true)] out VBatchFile? file, string? layer = null)
    {
        var path = Path.Join("batch", $"{name}.vbatch");

        if (!File.Exists(path))
        {
            file = null;
            return false;
        }

        file = CreateFromPath(path, layer);
        return true;
    }

    /// <exception cref="FileNotFoundException">There is no file at <paramref name="path"/>.</exception>
    public static VBatchFile CreateFromPath(string path, string? layer = null)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));

        var document = XDocument.Load(stream, LoadOptions.None);

        if (layer is not null)
        {
            KeepLayer(document, layer);
        }

        return new VBatchFile(document);
    }

    /// <summary>
    /// Removes the boxes and points of the other layers: a batch of several difficulties or stages holds each of them in its own
    /// layer, and a map plays the common one and its own.
    /// </summary>
    private static void KeepLayer(XDocument document, string layer)
    {
        var others = document
            .Descendants("iLayerBitmask")
            .Where(e => e.Attribute("value")?.Value is { } value && value != CommonLayer && value != layer)
            .Select(e => e.Parent)
            .OfType<XElement>()
            .ToList();

        foreach (var element in others)
        {
            element.Remove();
        }
    }
}
