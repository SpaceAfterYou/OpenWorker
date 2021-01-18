using SoulCore.Game.Datas.World.Table.Types;
using SoulCore.IO.File.Extensions;
using System.Xml;

namespace SoulCore.Game.Datas.World.Table.EventBox
{
    public sealed record VLuaFunctionBox : VEntity
    {
        /// <summary>
        /// Type of Target
        /// </summary>
        public LuaFunctionType Type { get; }

        /// <summary>
        /// LuaFunction
        /// </summary>
        public string Function { get; }

        /// <summary>
        /// ID of Monster or NPC for check.
        /// </summary>
        public uint CheckId { get; }

        internal VLuaFunctionBox(XmlNode xml) : base(xml)
        {
            Type = xml.GetEnum<LuaFunctionType>("m_eType");
            Function = xml.GetString("m_szFunction");
            CheckId = xml.GetUInt32("m_iCheckID");
        }
    }
}
