using SoulCore.IO.File.Extensions;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VSectorStartBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public uint Sector { get; }

        internal VSectorStartBox(XmlNode xml) : base(xml) =>
            Sector = xml.GetUInt32("m_nSectorID");
    }
}
