using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VPortalExitBox : VEntity
    {
        /// <summary>
        /// PortalBox ID of connected
        /// </summary>
        public int ParentPortal { get; }

        internal VPortalExitBox(XmlNode xml) : base(xml)
        {
            ParentPortal = int.Parse(xml.SelectSingleNode("m_iParentPortalBoxID").Attributes.GetNamedItem("value").Value);
        }
    }
}
