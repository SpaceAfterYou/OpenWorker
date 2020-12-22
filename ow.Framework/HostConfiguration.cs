namespace ow.Framework
{
    public class HostConfiguration
    {
        public string Ip { get; init; } = default!;
        public ushort Port { get; init; } = default!;

        public override string ToString() => $"{Ip}:{Port}";
    }
}