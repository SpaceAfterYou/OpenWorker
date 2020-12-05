using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VMazeEscapeBox : VEntity
    {
        /// <summary>
        /// Field ID
        /// </summary>
        public int Field { get; }

        /// <summary>
        /// ID of Event Object.
        /// </summary>
        public int EventObject { get; }

        internal VMazeEscapeBox(XmlNode xml) : base(xml)
        {
            Field = int.Parse(xml.SelectSingleNode("m_iField").Attributes.GetNamedItem("value").Value);
            EventObject = int.Parse(xml.SelectSingleNode("m_iEventObject").Attributes.GetNamedItem("value").Value);
        }
    }
}
