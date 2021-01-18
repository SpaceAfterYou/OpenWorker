using SoulCore.IO.File.Extensions;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VInterActionBox : VEntity
    {
        /// <summary>
        /// Interactive table ID
        /// </summary>
        public uint Interaction { get; }

        /// <summary>
        /// Interactive ObjectKey
        /// </summary>
        public string ObjectKey { get; }

        internal VInterActionBox(XmlNode xml) : base(xml)
        {
            Interaction = xml.GetUInt32("m_iInteractionID");
            ObjectKey = xml.GetString("m_sObjectKey");
        }
    }
}
