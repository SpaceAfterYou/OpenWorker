using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.Game.Extensions;
using System.Drawing;
using System.Numerics;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table
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
            Id = xml.GetUInt32("m_ID");
            GenerateId = xml.GetUInt32("m_iGenerateID");
            LayerBitmask = xml.GetEnum<LayerBitmask>("iLayerBitmask");
            PosTopLeft = xml.GetVector3("m_vPosTopLeft");
            PosBottomRight = xml.GetVector3("m_vPosBottomRight");
            Rotation = xml.GetSingle("m_fRotation");
            Size = xml.GetVector3("m_vSize");
            ShowCustomEntity = xml.GetBool("m_bShowCustomEntity");
            Color = xml.GetColor("m_ColorLDR");
        }
    }
}
