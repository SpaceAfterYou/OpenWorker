using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Gameplay.Modules.Login.Components;

[EntityComponent(EntityComponentService.Gate)]
public readonly record struct SecondPasswordComponent
{
    public required string Password { get; init; }
    
    public bool Has => string.IsNullOrEmpty(Password) is false;
}