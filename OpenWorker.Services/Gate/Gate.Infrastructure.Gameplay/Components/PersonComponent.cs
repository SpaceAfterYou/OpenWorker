using SoulWorkerResearch.SoulCore.Defines;

namespace Gate.Infrastructure.Gameplay.Components;

public sealed class PersonComponent
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public Class Class { get; init; }
    public Advancement Advancement { get; init; }
}
