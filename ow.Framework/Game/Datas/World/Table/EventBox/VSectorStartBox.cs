using ow.Framework.Game.Extensions;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed class VSectorStartBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint Sector { get; }

        internal VSectorStartBox(XmlNode xml) : base(xml) =>
            Sector = xml.GetUInt32("m_nSectorID");
    }
}
