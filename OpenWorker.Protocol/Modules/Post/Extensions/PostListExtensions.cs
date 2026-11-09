using OpenWorker.Hotspot.Modules.Post.Types;

namespace OpenWorker.Hotspot.Modules.Post.Extensions;

public static class PostListExtensions
{
    public static PostValue[] ReadPostList(this BinaryReader reader)
    {
        return Enumerable
            .Range(0, reader.ReadByte())
            .Select(_ => new PostValue(reader))
            .ToArray();
    }

    public static PostAccountValue[] ReadPostAccountList(this BinaryReader reader)
    {
        return Enumerable
            .Range(0, reader.ReadInt16())
            .Select(_ => new PostAccountValue(reader))
            .ToArray();
    }

    public static void Write(this BinaryWriter writer, PostValue[] values)
    {
        writer.Write(checked((byte)values.Length));

        foreach (var value in values)
        {
            value.Write(writer);
        }
    }

    public static void Write(this BinaryWriter writer, PostAccountValue[] values)
    {
        writer.Write(checked((short)values.Length));

        foreach (var value in values)
        {
            value.Write(writer);
        }
    }
}
