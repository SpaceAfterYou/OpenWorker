using System.Collections.Generic;

namespace ow.Framework
{
    public class GateConfiguration
    {
        public class HostConfiguration
        {
            public string Ip { get; init; } = default!;
            public ushort Port { get; init; } = default!;

            public override string ToString() => $"{Ip}:{Port}";
        }

        public class DistrictConfiguration
        {
            public byte ChannelOffset { get; init; }
            public ushort Location { get; init; }
            public string Id { get; init; } = default!;
            public HostConfiguration Host { get; init; } = default!;
        }

        public ushort Id { get; init; }
        public string Name { get; init; } = default!;
        public HostConfiguration Host { get; init; } = default!;
        public IReadOnlyList<DistrictConfiguration> Districts { get; init; } = default!;
    }
}