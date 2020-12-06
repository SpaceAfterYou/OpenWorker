using Core.Systems.GameSystem.Extensions;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table.EventBox
{
    public sealed class VQuestMoveCheckBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint EpisodeCondition { get; }

        internal VQuestMoveCheckBox(XmlNode xml) : base(xml) =>
            EpisodeCondition = xml.GetUInt32("m_uiEpisodeCondition");
    }
}