using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace Core.Systems.GameSystem.Datas.World.Table
{
    /// <summary>
    /// Base Entity
    /// </summary>
    public abstract class VEntity
    {
        /// <summary>
        /// Event Object ID
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Event Object Unique ID
        /// </summary>
        public uint GenerateId { get; }

        /// <summary>
        /// Event Object Layer
        /// </summary>
        public LayerBitmask LayerBitmask { get; }

        /// <summary>
        /// Position of the top-left event object(The value is automatically assigned.)
        /// </summary>
        public Vector3 PosTopLeft { get; }

        /// <summary>
        /// Position of the bottom-right event object(The value is automatically assigned.)
        /// </summary>
        public Vector3 PosBottomRight { get; }

        /// <summary>
        /// Rotation of event object(The value is automatically assigned.)
        /// </summary>
        public float Rotation { get; }

        /// <summary>
        /// Scale of event object
        /// </summary>
        public Vector3 Size { get; }

        /// <summary>
        /// Visibile of event boject(true or false)
        /// </summary>
        public bool ShowCustomEntity { get; }

        /// <summary>
        /// Color of event object
        /// </summary>
        public Color Color { get; }

        protected VEntity(XmlNode xml)
        {
            Id = uint.Parse(xml.SelectSingleNode("m_ID").Attributes.GetNamedItem("value").Value);
            GenerateId = uint.Parse(xml.SelectSingleNode("m_iGenerateID").Attributes.GetNamedItem("value").Value);
            LayerBitmask = (LayerBitmask)Enum.Parse(typeof(LayerBitmask), xml.SelectSingleNode("iLayerBitmask").Attributes.GetNamedItem("value").Value, true);

            float[] posTopLeft = xml.SelectSingleNode("m_vPosTopLeft").Attributes.GetNamedItem("value").Value.Split(',').Select(float.Parse).ToArray();
            PosTopLeft = new Vector3(posTopLeft[0], posTopLeft[1], posTopLeft[2]);

            float[] posBottomRight = xml.SelectSingleNode("m_vPosBottomRight").Attributes.GetNamedItem("value").Value.Split(',').Select(float.Parse).ToArray();
            PosBottomRight = new Vector3(posBottomRight[0], posBottomRight[1], posBottomRight[2]);

            Rotation = float.Parse(xml.SelectSingleNode("m_fRotation").Attributes.GetNamedItem("value").Value);

            float[] size = xml.SelectSingleNode("m_vSize").Attributes.GetNamedItem("value").Value.Split(',').Select(float.Parse).ToArray();
            Size = new Vector3(size[0], size[1], size[2]);

            ShowCustomEntity = bool.Parse(xml.SelectSingleNode("m_bShowCustomEntity").Attributes.GetNamedItem("value").Value);

            byte[] colorLdr = xml.SelectSingleNode("m_ColorLDR").Attributes.GetNamedItem("value").Value.Split(',').Select(byte.Parse).ToArray();
            Color = Color.FromArgb(colorLdr[3], colorLdr[0], colorLdr[1], colorLdr[2]);
        }
    }
}
