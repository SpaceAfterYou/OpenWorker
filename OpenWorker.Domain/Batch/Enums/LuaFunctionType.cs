using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum LuaFunctionType : byte
{
    Self,
    Party,
    Monster,
    Warp,

    [XmlEnum("NPC")]
    Npc
}