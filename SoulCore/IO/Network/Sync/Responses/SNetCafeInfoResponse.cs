namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record SNetCafeInfoResponse
    {
        public bool NetCafe { get; init; }
        public int Event { get; init; }
    }
}
