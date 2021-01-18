using System.Collections.Generic;

namespace SoulCore
{
    public sealed record WorldConfiguration
    {
        public byte ChannelsPerDistrictInstance { get; init; }
        public byte ChannelsPerMazeInstance { get; init; }

        public IReadOnlyDictionary<string, InstanceConfiguration> Instance { get; init; } = default!;
    }
}
