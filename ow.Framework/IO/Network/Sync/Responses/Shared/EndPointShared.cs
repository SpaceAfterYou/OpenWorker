namespace ow.Framework.IO.Network.Sync.Responses.Shared
{
    public record EndPointShared
    {
        public string Ip { get; init; } = string.Empty;
        public ushort Port { get; init; }
    }
}