using System.Diagnostics;
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

    public static VBatchFile Create(string name)
    {
        var path = Path.Join("batch", $"{name}.vbatch");
        return CreateFromPath(path);
    }
    
    public static VBatchFile CreateFromPath(string path)
    {
        Debug.Assert(File.Exists(path), $"{path} does not exist.");

        using var stream = new MemoryStream(File.ReadAllBytes(path));

        var document = XDocument.Load(stream, LoadOptions.None);
        return new VBatchFile(document);
    }
}