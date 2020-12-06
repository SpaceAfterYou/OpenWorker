using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VPortalExitBox : VEntity
    {
        /// <summary>
        /// PortalBox ID of connected
        /// </summary>
        public uint ParentPortal { get; }

        internal VPortalExitBox(XmlNode xml) : base(xml)
        {
            ParentPortal = uint.Parse(xml.SelectSingleNode("m_iParentPortalBoxID").Attributes.GetNamedItem("value").Value);
        }
    }
}
