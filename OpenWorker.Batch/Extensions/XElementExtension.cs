using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace OpenWorker.Batch.Extensions;

internal static class XElementExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetString(this XElement @this, string xpath)
    {
        return @this.GetRawValue(xpath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float GetSingle(this XElement @this, string xpath)
    {
        return float.Parse(@this.GetRawValue(xpath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetUInt32(this XElement @this, string xpath)
    {
        return int.Parse(@this.GetRawValue(xpath));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetInt32(this XElement @this, string xpath)
    {
        return int.Parse(@this.GetRawValue(xpath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static short GetInt16(this XElement @this, string xpath)
    {
        return short.Parse(@this.GetRawValue(xpath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte GetByte(this XElement @this, string xpath)
    {
        return byte.Parse(@this.GetRawValue(xpath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetBool(this XElement @this, string xpath)
    {
        return bool.Parse(@this.GetRawValue(xpath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector3 GetVector3(this XElement @this, string xpath)
    {
        var values = @this.Split(xpath, float.Parse);
        Debug.Assert(values.Length == 3);

        return new Vector3(values[0], values[1], values[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Color GetColor(this XElement @this, string xpath)
    {
        var values = @this.Split(xpath, byte.Parse);
        Debug.Assert(values.Length == 4);

        return Color.FromArgb(red: values[0], green: values[1], blue: values[2], alpha: values[3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TEnum GetEnum<TEnum>(this XElement @this, string xpath) where TEnum : struct
    {
        var value = @this.GetEnumByPropertyName<TEnum>(xpath) ?? @this.GetEnumByAttributeName<TEnum>(xpath);
        return value ?? throw new XmlParsingException($"No value found for {xpath} for enum {nameof(TEnum)}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] Split<T>(this XElement @this, string xpath, Func<string, T> parser)
    {
        return @this
            .GetRawValue(xpath)
            .Split(',')
            .Select(parser)
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetRawValue(this XElement @this, string xpath)
    {
        var value = @this
            .XPathSelectElement(xpath)?
            .Attributes()
            .First(v => v.Name == "value").Value;

        return value ?? throw new XmlParsingException($"No value found for {xpath}");
    }

    private static TEnum? GetEnumByPropertyName<TEnum>(this XElement @this, string xpath) where TEnum : struct
    {
        var name = @this.GetString(xpath);

        if (Enum.TryParse<TEnum>(name, false, out var result))
        {
            return result;
        }

        return null;
    }

    private static TEnum? GetEnumByAttributeName<TEnum>(this XElement @this, string xpath) where TEnum : struct
    {
        var name = @this.GetString(xpath);
        var type = typeof(TEnum);

        var found = type
            .GetFields()
            .FirstOrDefault(f => f.GetCustomAttribute<XmlEnumAttribute>()?.Name == name)?.Name;

        if (Enum.TryParse<TEnum>(found, false, out var result))
        {
            return result;
        }

        return null;
    }
}