using System.Diagnostics;
using System.Reflection;
using System.Text;
using OpenWorker.Havok.Attributes;
using OpenWorker.Havok.Enums;

namespace OpenWorker.Havok.Extensions;

public static class BinaryReaderExtensions
{
    private static IEnumerable<Type> Classes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .Where(x => x.IsClass)
        .Where(x => x.GetCustomAttribute<HavokSerializeClassAttribute>() is not null);
    
    public static void ReadObjectTopLevel(this BinaryReader reader)
    {
        var a3 = reader.ReadHavokNumber();

        switch (a3)
        {
            case 0:
                var u2 = reader.ReadByte();
                Debug.WriteLine(u2);
                break;
            
            case 5:
                var u1 = reader.ReadHavokNumber();
                Debug.WriteLine(u1);
                break;
			
            case 6:
                Debug.WriteLine("6");
                break;
        }
    }

    public static void ReadHavokClass(this BinaryReader reader, List<string> remainClasses, List<string> classes)
    {
        var name = ReadHavokString(reader);
        
        if (name.Length < 1)
        {
            name = remainClasses.Last();
            Debug.Assert(name.Length > 0);
            remainClasses.RemoveAt(remainClasses.Count - 1);
        }
        
        classes.Add(name);
        
        var version = reader.ReadHavokNumber();
        var parentIndex = reader.ReadHavokNumber() - 1;
        var parent = parentIndex < 0 ? string.Empty : classes[parentIndex];
        var members = reader.ReadHavokNumber();
        
        Debug.Write($"<class name=\"{name}\" version=\"{version}\"");
        
        if (parent.Length > 0)
        {
            Debug.Write($" parent=\"{parent}\"");
        }
        
        Debug.WriteLine(">");
        
        Debug.Assert(Classes.Any(x => 
        {
            var a = x.GetCustomAttribute<HavokSerializeClassAttribute>();
            return a is not null && a.Name == name && a.Version == version;
        }));
        
        foreach (var _ in Enumerable.Range(0, members))
        {
            var member = reader.ReadHavokString();
            var v26 = reader.ReadHavokNumber();

            if ((v26 & (int)HavokLegacyType.Tuple) != 0)
            {
                var v60 = reader.ReadHavokNumber();
            }
            
            var type = (HavokLegacyType)(v26 & (int)HavokLegacyType.MaskBasicTypes);
            if (type is HavokLegacyType.Struct or HavokLegacyType.Object)
            {
                var @class = reader.ReadHavokString();
                if (@class.Length > 0)
                {
                    remainClasses.Add(@class);
                }
            }
            
            Debug.WriteLine($"  <member name=\"{member}\" />"); // type="" array="" class=""
        }
        
        Debug.WriteLine("</class>");
    }
    
    private static string ReadHavokString(this BinaryReader reader)
    {
        var length = reader.ReadHavokNumber();
        
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(buffer);
    }
    
    private static short[] ReadHavok16Array(byte[] source)
    {
        var result = new short[source.Length / sizeof(short)]; 
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }
    
    private static int[] ReadHavok32Array(byte[] source)
    {
        var result = new int[source.Length / sizeof(int)]; 
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }
    
    private static long[] ReadHavok64Array(byte[] source)
    {
        var result = new long[source.Length / sizeof(long)]; 
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }

    public static Array ReadHavokGenericArray(this BinaryReader reader, int size, int count)
    {
        var source = reader.ReadBytes(size * count);

        return size switch
        {
            1 => source,
            2 => ReadHavok16Array(source),
            4 => ReadHavok32Array(source),
            8 => ReadHavok64Array(source),
            _ => throw new NotSupportedException()
        };
    }
    
    public static int ReadHavokNumber(this BinaryReader reader)
    {
        int data = reader.ReadSByte();
        var value = (data >> 1) & 0x7FFFFFBF;
        var signFlag = data & 1;
        var bitsPerNumber = 6;

        if (data < 0)
        {
            int buffer;

            do
            {
                buffer = reader.ReadSByte();
                var more = (buffer & 0x7F) << bitsPerNumber;
                bitsPerNumber += 7;
                value |= more;
            } while ((buffer & 0x80u) != 0);
        }

        if (signFlag > 0)
        {
            value = -value;
        }

        return value;
    }

    public static TagType ReadTagType(this BinaryReader reader)
    {
        return (TagType)reader.ReadHavokNumber();
    }
}