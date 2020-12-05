using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch
{
    public sealed class Root
    {
        public EventBox.VEntities EventBox { get; }
        public EventPoint.VEntities EventPoint { get; }

        internal Root(XmlNode xml)
        {
            EventBox = new(xml.SelectSingleNode("Batchs [@eventtype='EventBox']"));
            EventPoint = new(xml.SelectSingleNode("Batchs [@eventtype='EventPoint']"));
        }
    }
}