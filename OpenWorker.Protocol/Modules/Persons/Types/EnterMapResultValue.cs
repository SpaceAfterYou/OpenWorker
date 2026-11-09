using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

public readonly struct EnterMapResultValue
{
    public ZoneValue Zone { get; init; }
    public ChangeServerType ChangeType { get; init; }
    public bool ChangeServer { get; init; }
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