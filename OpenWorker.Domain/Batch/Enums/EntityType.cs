using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum EntityType : byte
{
    [XmlEnum("PC")]
    PlayerCharacter,

    Npc,
    Monster,
    None
}