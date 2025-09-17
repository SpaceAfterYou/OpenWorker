namespace OpenWorker.Havok;

public static class HavokInputArchive
{
    public static void ReadArrayGeneric(Stream stream, byte[] output, int elementSize, int arraySize)
    {
        var total = elementSize * arraySize;
        var buffer = new byte[total];
        stream.ReadExactly(buffer);
    }
}