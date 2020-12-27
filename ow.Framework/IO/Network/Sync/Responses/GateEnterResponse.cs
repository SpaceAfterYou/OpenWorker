using ow.Framework.Game.Enums;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record GateEnterResponse
    {
        public int AccountId { get; init; }
        public GateEnterResult Result { get; init; }
    }
}
