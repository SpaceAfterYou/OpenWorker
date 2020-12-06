using System.Xml;

namespace Core.Extensions
{
    public static class XmlNodeExtension
    {
        public static string GetAttributeValue(this XmlNode xml, string xpath) =>
            xml.SelectSingleNode(xpath).Attributes.GetNamedItem("value").Value;
    }
}