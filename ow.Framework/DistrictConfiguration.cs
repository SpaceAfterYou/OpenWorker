namespace ow.Framework
{
    public class DistrictConfiguration
    {
        public byte Gate { get; init; }
        public byte ChannelOffset { get; init; }
        public ushort Location { get; init; }
        public HostConfiguration Host { get; init; } = default!;
    }
}