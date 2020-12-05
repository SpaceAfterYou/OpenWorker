using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VCheckSectorBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public int CheckSector { get; }

        /// <summary>
        ///
        /// </summary>
        public string Lua { get; }

        /// <summary>
        ///
        /// </summary>
        public int CheckGate { get; }

        internal VCheckSectorBox(XmlNode xml) : base(xml)
        {
            CheckSector = int.Parse(xml.SelectSingleNode("m_iCheckSector").Attributes.GetNamedItem("value").Value);
            Lua = xml.SelectSingleNode("m_szLua").Attributes.GetNamedItem("value").Value;
            CheckGate = int.Parse(xml.SelectSingleNode("m_iCheckGate").Attributes.GetNamedItem("value").Value);
        }
    }
}
