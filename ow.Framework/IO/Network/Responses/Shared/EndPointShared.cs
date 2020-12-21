namespace ow.Framework.IO.Network.Responses.Shared
{
    public record EndPointShared
    {
        public string Ip { get; init; } = default!;
        public ushort Port { get; init; }
    }
}