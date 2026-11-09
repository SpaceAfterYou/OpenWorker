using System.Reflection;
using System.Text;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Makes one row of a table out of the values a test names: the bytes of the row are written in the order of its columns,
/// every column the test does not name being zero (an empty string), and the row reads them back as a table file would.
/// </summary>
internal static class TestRows
{
    public static T Build<T>(params (string Column, object Value)[] values) where T : struct
    {
        var named = values.ToDictionary(e => e.Column, e => e.Value);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode);

        // The columns are the auto-properties of the generated row, in the order of their backing fields.
        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (!field.Name.StartsWith('<') || !field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
            {
                continue;
            }

            var name = field.Name[1..field.Name.IndexOf('>', StringComparison.Ordinal)];
            var type = field.FieldType.IsEnum ? Enum.GetUnderlyingType(field.FieldType) : field.FieldType;
            var value = named.TryGetValue(name, out var given) ? given : null;

            if (type == typeof(string))
            {
                var text = (string?)value ?? string.Empty;

                writer.Write((short)text.Length);
                writer.Write(Encoding.Unicode.GetBytes(text));
            }
            else if (type == typeof(byte)) writer.Write(Convert.ToByte(value ?? 0));
            else if (type == typeof(sbyte)) writer.Write(Convert.ToSByte(value ?? 0));
            else if (type == typeof(short)) writer.Write(Convert.ToInt16(value ?? 0));
            else if (type == typeof(ushort)) writer.Write(Convert.ToUInt16(value ?? 0));
            else if (type == typeof(int)) writer.Write(Convert.ToInt32(value ?? 0));
            else if (type == typeof(uint)) writer.Write(Convert.ToUInt32(value ?? 0));
            else if (type == typeof(long)) writer.Write(Convert.ToInt64(value ?? 0));
            else if (type == typeof(float)) writer.Write(Convert.ToSingle(value ?? 0f));
            else if (type == typeof(bool)) writer.Write(Convert.ToBoolean(value ?? false));
            else throw new NotSupportedException($"{typeof(T).Name}.{name} is a {type.Name}");
        }

        writer.Flush();
        stream.Position = 0;

        return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream, Encoding.Unicode))!;
    }
}
