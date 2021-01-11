namespace ow.Framework
{
    public sealed record RelayConfiguration
    {
        public HostConfiguration Host { get; init; } = default!;
    }
}