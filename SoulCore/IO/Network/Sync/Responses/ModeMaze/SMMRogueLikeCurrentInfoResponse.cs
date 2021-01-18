namespace SoulCore.IO.Network.Sync.Responses.ModeMaze
{
    public sealed record SMMRogueLikeCurrentInfoResponse
    {
        public int MapStep { get; init; }
        public int SectorStep { get; init; }
    }
}
