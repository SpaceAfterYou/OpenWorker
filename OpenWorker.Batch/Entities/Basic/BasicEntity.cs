using System.Drawing;
using System.Numerics;
using System.Xml.Linq;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities.Basic;

/// <summary>
///     Base Entity
/// </summary>
public abstract record BasicEntity
{
    protected BasicEntity(XElement x)
    {
        Id = x.GetUInt32("m_ID");
        Generate = x.GetUInt32("m_iGenerateID");
        LayerBitmask = x.GetEnum<LayerBitmask>("iLayerBitmask");
        PosTopLeft = x.GetVector3("m_vPosTopLeft");
        PosBottomRight = x.GetVector3("m_vPosBottomRight");
        Rotation = x.GetSingle("m_fRotation");
        Size = x.GetVector3("m_vSize");
        ShowCustomEntity = x.GetBool("m_bShowCustomEntity");
        Color = x.GetColor("m_ColorLDR");
    }

    /// <summary>
    ///     Event Object ID
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     Event Object Unique ID
    /// </summary>
    public int Generate { get; }

    /// <summary>
    ///     Event Object Layer
    /// </summary>
    public LayerBitmask LayerBitmask { get; }

    /// <summary>
    ///     Position of the top-left event object(The value is automatically assigned.)
    /// </summary>
    public Vector3 PosTopLeft { get; }

    /// <summary>
    ///     Position of the bottom-right event object(The value is automatically assigned.)
    /// </summary>
    public Vector3 PosBottomRight { get; }

    /// <summary>
    ///     Rotation of event object(The value is automatically assigned.)
    /// </summary>
    public float Rotation { get; }

    /// <summary>
    ///     Scale of event object
    /// </summary>
    public Vector3 Size { get; }

    /// <summary>
    ///     Visibile of event boject(true or false)
    /// </summary>
    public bool ShowCustomEntity { get; }

    /// <summary>
    ///     Color of event object
    /// </summary>
    public Color Color { get; }
}