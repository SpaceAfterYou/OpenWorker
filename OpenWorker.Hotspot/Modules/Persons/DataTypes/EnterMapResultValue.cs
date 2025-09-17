using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Modules.Persons.DataTypes;

public readonly struct EnterMapResultValue
{
    public ZoneValue Zone { get; init; }
    public ChangeServerType ChangeType { get; init; }
    public bool ChangeServer { get; init; }
    public int Result { get; init; }

    public static EnterMapResultValue Error => new() { Result = 1 };
}