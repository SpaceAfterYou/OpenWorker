namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record SgTokenUpdateResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public bool Renew { get; init; }
    }
}