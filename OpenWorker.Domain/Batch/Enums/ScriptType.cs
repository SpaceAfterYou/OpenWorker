using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum ScriptType : byte
{
    None,
    Spawn,

    [XmlEnum("HP")]
    Health
}