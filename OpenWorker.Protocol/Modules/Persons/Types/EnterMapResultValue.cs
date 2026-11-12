using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Where a move to another map goes: the zone, whether the client changes servers and how, and the result.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct EnterMapResultValue
{
    public ZoneValue Zone { get; init; }
    public bool ChangeServer { get; init; }
    public ChangeServerType ChangeType { get; init; }
    public int Result { get; init; }

    public static EnterMapResultValue Error => Failed(1);

    public static EnterMapResultValue Failed(int result) => new()
    {
        // The address is always written and must not be empty.
        Zone = new ZoneValue { Address = "0.0.0.0" },
        Result = result
    };

    public const int CannotEnter = 50104;
    public const int ServerUnavailable = 50003;
}
