using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OpenWorker.Batch;

public sealed record VBatchFile
{
    private VBatchFile(XNode document)
    {
        var eventBox = document.XPathSelectElement("root/Batchs [@eventtype='EventBox']");
        EventBox = new EventBox(eventBox ?? throw new ApplicationException());

        var eventPoint = document.XPathSelectElement("root/Batchs [@eventtype='EventPoint']");
        EventPoint = new EventPoint(eventPoint ?? throw new ApplicationException());
    }

    public EventBox EventBox { get; }
    public EventPoint EventPoint { get; }

    /// <summary>
    /// False when the client data has no batch of that name, which a server gets wrong by installing it wrong.
    /// </summary>
    public static bool TryCreate(string name, [NotNullWhen(true)] out VBatchFile? file)
    {
        var path = Path.Join("batch", $"{name}.vbatch");

        if (!File.Exists(path))
        {
            file = null;
            return false;
        }

        file = CreateFromPath(path);
        return true;
    }

    /// <exception cref="FileNotFoundException">There is no file at <paramref name="path"/>.</exception>
    public static VBatchFile CreateFromPath(string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));

        var document = XDocument.Load(stream, LoadOptions.None);
        return new VBatchFile(document);
    }
}