using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace ow.Framework.IO.File.Extensions
{
    internal static class XmlNodeExtension
    {
        internal static string GetString(this XmlNode xml, string xpath) =>
            xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException();

        internal static float GetSingle(this XmlNode xml, string xpath) =>
            float.Parse(xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException());

        internal static uint GetUInt32(this XmlNode xml, string xpath) =>
            uint.Parse(xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException());

        internal static ushort GetUInt16(this XmlNode xml, string xpath) =>
            ushort.Parse(xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException());

        internal static byte GetByte(this XmlNode xml, string xpath) =>
            byte.Parse(xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException());

        internal static bool GetBool(this XmlNode xml, string xpath) =>
            bool.Parse(xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value ?? throw new ApplicationException());

        internal static T[] GetSplitted<T>(this XmlNode xml, string xpath, Func<string, T> func) =>
            xml.SelectSingleNode(xpath)?.Attributes?.GetNamedItem("value")?.Value?.Split(',').Select(func).ToArray() ?? throw new ApplicationException();

        internal static Vector3 GetVector3(this XmlNode xml, string xpath)
        {
            float[] values = xml.GetSplitted(xpath, float.Parse);
            return new(values[0], values[1], values[2]);
        }

        internal static Color GetColor(this XmlNode xml, string xpath)
        {
            byte[] values = xml.GetSplitted(xpath, byte.Parse);
            return Color.FromArgb(values[0], values[1], values[2], values[3]);
        }

        internal static T GetEnum<T>(this XmlNode xml, string xpath)
        {
            Type type = typeof(T);

            string enumName = xml.GetString(xpath);

            if (Enum.TryParse(type, enumName, false, out object? result))
                return (T)result!;

            enumName = type.GetFields().First(f => f.GetCustomAttribute<XmlEnumAttribute>()?.Name == enumName).Name;
            return (T)Enum.Parse(type, enumName, false);
        }
    }
}