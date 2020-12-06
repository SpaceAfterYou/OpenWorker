using Core.Systems.GameSystem.Extensions;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventPoint
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
            Function = xml.GetString("m_szFunction");
            EnableEffectPath = xml.GetString("m_sEnableEffectPath");
            DisableEffectPath = xml.GetString("m_sDisableEffectPath");
        }
    }
}