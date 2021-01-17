namespace ow.Framework.IO.Network.Sync.Responses.Shared
{
    public record SEndPointSharedResponse
    {
        public string Ip { get; init; } = string.Empty;
        public ushort Port { get; init; }

        public static SEndPointSharedResponse Empty { get; } = new();
    }
}