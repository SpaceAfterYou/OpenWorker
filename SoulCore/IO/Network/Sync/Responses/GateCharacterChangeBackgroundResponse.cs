namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record GateCharacterChangeBackgroundResponse
    {
        public int AccountId { get; init; }
        public uint BackgroundId { get; init; }
    }
}
