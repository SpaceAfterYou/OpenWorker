using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Hotspot.Modules.World.Responses;

public readonly struct StatKeyValuePair
{
    public required STAT_TYPE byIndex { get; init; }
    public required float statValue { get; init; }
}