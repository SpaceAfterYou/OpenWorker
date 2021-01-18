namespace SoulCore
{
    public sealed record HostConfiguration
    {
        public string Ip { get; init; } = default!;
        public ushort Port { get; init; }

        public override string ToString() => $"{Ip}:{Port}";
    }
}
