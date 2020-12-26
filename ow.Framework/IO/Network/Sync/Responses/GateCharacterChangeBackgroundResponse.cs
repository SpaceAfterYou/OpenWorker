namespace ow.Framework.IO.Network.Responses
{
    public sealed record GateCharacterChangeBackgroundResponse
    {
        public int AccountId { get; init; }
        public uint BackgroundId { get; init; }
    }
}