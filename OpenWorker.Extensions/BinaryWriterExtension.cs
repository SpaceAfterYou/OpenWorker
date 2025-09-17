using System.Diagnostics;

namespace OpenWorker.Extensions;

public enum TimeSpanSize : byte
{
    Int32,
    Int64
}

public static class BinaryWriterExtension
{
    public static void Write(this BinaryWriter writer, DateTimeOffset value)
    {
        writer.Write(value.ToUnixTimeSeconds());
    }

    public static void Write(this BinaryWriter writer, TimeSpan value, TimeSpanSize size = TimeSpanSize.Int32)
    {
        Debug.Assert(size is TimeSpanSize.Int32 or TimeSpanSize.Int64);

        if (size is TimeSpanSize.Int32)
        {
            writer.Write((int)value.TotalSeconds);
        }

        else
        {
            writer.Write((long)value.TotalSeconds);
        }
    }
}