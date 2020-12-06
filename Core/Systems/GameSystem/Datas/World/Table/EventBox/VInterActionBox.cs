using Core.Systems.GameSystem.Extensions;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VInterActionBox : VEntity
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