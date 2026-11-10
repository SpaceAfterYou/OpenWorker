namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The error a service answers when the NPC of its request is not there, stands too far or does not offer it: the code of
/// the client for that service where the original game has one, the general failure of the service otherwise.
/// </summary>
public static class ServiceNpcCodes
{
    /// <summary>A repair at an NPC that is no shop.</summary>
    public const ushort Repair = 52454;

    /// <summary>An upgrade or an evolution at an NPC that does not upgrade.</summary>
    public const ushort Upgrade = 52217;

    /// <summary>Production at an NPC that does not make the recipe's group.</summary>
    public const ushort Make = 52308;

    /// <summary>An exchange at an NPC that does not offer it.</summary>
    public const ushort Exchange = 52260;

    /// <summary>Feeding akashic cards at an NPC that does not mix them.</summary>
    public const ushort AkashicCompose = 52296;

    /// <summary>Making or taking out akashic cards away from an NPC: the general failure of akashic records.</summary>
    public const ushort Akashic = 52002;

    /// <summary>Taking items apart away from an NPC: the failure of the list.</summary>
    public const ushort Disassemble = 52270;

    /// <summary>A ride asked at an NPC that carries no one, that is not there or stands too far; also a ride asked while on one.</summary>
    public const ushort Transport = 58000;

    /// <summary>A ride that costs more gold than the player has.</summary>
    public const ushort TransportGold = 58001;

    /// <summary>A ride the NPC does not offer, or one that ends nowhere on this map.</summary>
    public const ushort TransportRide = 58002;
}
