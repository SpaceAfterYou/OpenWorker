using System.Collections.Generic;

namespace ow.Framework
{
    public sealed record InstanceConfiguration
    {
        public ushort Id { get; init; } = default!;
        public GateConfiguration Gate { get; init; } = default!;
        public RelayConfiguration Relay { get; init; } = default!;
        public IReadOnlyDictionary<string, DistrictConfiguration> District { get; init; } = default!;
    }
}