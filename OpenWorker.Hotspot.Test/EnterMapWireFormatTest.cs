using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class EnterMapWireFormatTest
{
    [TestMethod]
    public void ErrorResponseIsWritableAndCarriesResult()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        new WorldEnterResponse { Map = EnterMapResultValue.Error }.Write(writer);
        writer.Flush();

        var bytes = stream.ToArray();

        // The result is followed by the two flags of this response.
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, bytes.Length - 2 - sizeof(int)));
    }
}
