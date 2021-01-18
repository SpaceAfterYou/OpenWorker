using SoulCore.IO.File.Extensions;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VQuestMoveCheckBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint EpisodeCondition { get; }

        internal VQuestMoveCheckBox(XmlNode xml) : base(xml) =>
            EpisodeCondition = xml.GetUInt32("m_uiEpisodeCondition");
    }
}
