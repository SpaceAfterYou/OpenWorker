using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Items.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct StorageComponent(Entity[] Collection);