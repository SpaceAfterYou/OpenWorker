using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum MonsterType : byte
{
    Npc,
    Monster,
    DestructionObject,

    [XmlEnum("PC")]
    PlayerCharacter
}