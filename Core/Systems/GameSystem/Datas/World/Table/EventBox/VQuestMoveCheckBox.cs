using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VQuestMoveCheckBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint EpisodeCondition { get; }

        internal VQuestMoveCheckBox(XmlNode xml) : base(xml)
        {
            EpisodeCondition = uint.Parse(xml.SelectSingleNode("m_uiEpisodeCondition").Attributes.GetNamedItem("value").Value);
        }
    }
}
