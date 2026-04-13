namespace OpenWorker.Hotspot.Modules.Party.Enums;

public enum PartyApplyError : byte
{
    Success = 0x0,
    PartyRecruit = 0x1,
    Overlap = 0x2,
    Party = 0x3,
    PartyNot = 0x4,
    Count = 0x5,
    PartyCount = 0x6,
    PartyLevel = 0x7,
};
