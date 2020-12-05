using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VQuestMoveCheckBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public int EpisodeCondition { get; }

        internal VQuestMoveCheckBox(XmlNode xml) : base(xml)
        {
            EpisodeCondition = int.Parse(xml.SelectSingleNode("m_uiEpisodeCondition").Attributes.GetNamedItem("value").Value);
        }
    }
}
