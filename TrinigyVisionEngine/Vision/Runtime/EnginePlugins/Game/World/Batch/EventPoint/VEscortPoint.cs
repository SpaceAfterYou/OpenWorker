using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventPoint
{
    public sealed class VEscortPoint : VPoint
    {
        /// <summary>
        ///
        /// </summary>
        public string Function { get; }

        /// <summary>
        ///
        /// </summary>
        public string EnableEffectPath { get; }

        /// <summary>
        ///
        /// </summary>
        public string DisableEffectPath { get; }

        internal VEscortPoint(XmlNode xml) : base(xml)
        {
            Function = xml.SelectSingleNode("m_szFunction").Attributes.GetNamedItem("value").Value;
            EnableEffectPath = xml.SelectSingleNode("m_sEnableEffectPath").Attributes.GetNamedItem("value").Value;
            DisableEffectPath = xml.SelectSingleNode("m_sDisableEffectPath").Attributes.GetNamedItem("value").Value;
        }
    }
}
