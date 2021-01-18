namespace SoulCore
{
    public sealed record RelayConfiguration
    {
        public HostConfiguration Host { get; init; } = default!;
    }
}
