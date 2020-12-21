using ow.Framework.IO.File.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
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
