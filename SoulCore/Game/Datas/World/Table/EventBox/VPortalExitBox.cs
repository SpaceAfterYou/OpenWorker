﻿using SoulCore.IO.File.Extensions;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VPortalExitBox : VEntity
    {
        /// <summary>
        /// PortalBox ID of connected
        /// </summary>
        public uint ParentPortal { get; }

        internal VPortalExitBox(XmlNode xml) : base(xml) =>
            ParentPortal = xml.GetUInt32("m_iParentPortalBoxID");
    }
}
