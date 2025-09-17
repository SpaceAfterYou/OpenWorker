using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum MonsterSpawnType : byte
{
    Monster,
    DestructionObject,
    TreasureBox,
    Unit,
    SocialObject,

    [XmlEnum("NPC")]
    Npc
}