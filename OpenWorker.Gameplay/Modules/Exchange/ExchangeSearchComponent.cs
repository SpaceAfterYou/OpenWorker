using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Exchange;

/// <summary>
/// When the player last searched the exchange, as <see cref="TimeProvider.GetTimestamp"/>; none yet while
/// <see cref="Searched"/> is false.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct ExchangeSearchComponent(bool Searched, long At);
