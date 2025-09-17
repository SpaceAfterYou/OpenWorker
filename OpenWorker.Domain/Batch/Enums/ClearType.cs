using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum ClearType : byte
{
    None,
    Kill,
    Always,
    ModeClear,

    [XmlEnum("Kill_Perpect")]
    KillPerpect
}