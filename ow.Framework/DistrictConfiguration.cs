namespace ow.Framework
{
    public sealed record DistrictConfiguration
    {
        public ushort Location { get; init; }
        public HostConfiguration Host { get; init; } = default!;
    }
}