using System;
using System.Xml;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VLuaFunctionBox : VEntity
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
        public int CheckId { get; }

        internal VLuaFunctionBox(XmlNode xml) : base(xml)
        {
            Type = (LuaFunctionType)Enum.Parse(typeof(LuaFunctionType), xml.SelectSingleNode("m_eType").Attributes.GetNamedItem("value").Value, false);
            Function = xml.SelectSingleNode("m_szFunction").Attributes.GetNamedItem("value").Value;
            CheckId = int.Parse(xml.SelectSingleNode("m_iCheckID").Attributes.GetNamedItem("value").Value);
        }
    }
}
