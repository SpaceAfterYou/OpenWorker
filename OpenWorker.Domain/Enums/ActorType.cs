namespace OpenWorker.Domain.Enums;

public enum ActorType : byte
{
    User = 0x0,
    Npc = 0x1,
    Monster = 0x2,
    Akashic = 0x3,
    InteractionObject = 0x4,
    VaccumCube = 0x5,
    SocialItemObject = 0x6
}