using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Messages.Response.Person.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct RankComponent(byte Level, int Experience)
{
    public RankComponent(RankValue value) : this(value.Level, value.Experience)
    {
        
    }
}