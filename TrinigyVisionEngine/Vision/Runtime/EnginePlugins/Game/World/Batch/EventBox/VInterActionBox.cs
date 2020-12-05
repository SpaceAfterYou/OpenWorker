using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VInterActionBox : VEntity
    {
        /// <summary>
        /// Interactive table ID
        /// </summary>
        public int Interaction { get; }

        /// <summary>
        /// Interactive ObjectKey
        /// </summary>
        public string ObjectKey { get; }

        internal VInterActionBox(XmlNode xml) : base(xml)
        {
            Interaction = int.Parse(xml.SelectSingleNode("m_iInteractionID").Attributes.GetNamedItem("value").Value);
            ObjectKey = xml.SelectSingleNode("m_sObjectKey").Attributes.GetNamedItem("value").Value;
        }
    }
}
