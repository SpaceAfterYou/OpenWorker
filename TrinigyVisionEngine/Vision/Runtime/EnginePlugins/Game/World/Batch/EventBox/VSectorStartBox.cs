using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VSectorStartBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public int Sector { get; }

        internal VSectorStartBox(XmlNode xml) : base(xml)
        {
            Sector = int.Parse(xml.SelectSingleNode("m_nSectorID").Attributes.GetNamedItem("value").Value);
        }
    }
}
