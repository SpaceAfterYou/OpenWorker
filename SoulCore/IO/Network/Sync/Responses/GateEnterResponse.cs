using SoulCore.Game.Enums;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record GateEnterResponse
    {
        public int AccountId { get; init; }
        public GateEnterResult Result { get; init; }
    }
}
