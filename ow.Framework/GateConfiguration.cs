namespace ow.Framework
{
    public sealed record GateConfiguration
    {
        public string Name { get; init; } = default!;
        public HostConfiguration Host { get; init; } = default!;
    }
}