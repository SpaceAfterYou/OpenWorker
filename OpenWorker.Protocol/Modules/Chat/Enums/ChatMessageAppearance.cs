namespace OpenWorker.Hotspot.Modules.Chat.Enums;

public enum ChatMessageAppearance : byte
{
    Normal = 0x1,
    Trade = 0x2,
    Party = 0x3,
    League = 0x4,
    Whisper = 0x5,
    Yell = 0x6,
    Notice = 0x7,
    System = 0x8,
    Event = 0x9,
    NpcTalk = 0xA,
    Max = 0xB
}