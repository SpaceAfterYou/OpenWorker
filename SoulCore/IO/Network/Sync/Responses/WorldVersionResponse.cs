namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record WorldVersionResponse
    {
        public int Id { get; init; }
        public int Main { get; init; }
        public int Sub { get; init; }
        public int Data { get; init; }
    }
}
