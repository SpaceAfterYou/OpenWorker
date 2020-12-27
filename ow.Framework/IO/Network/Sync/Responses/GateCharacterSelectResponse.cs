using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record GateCharacterSelectResponse
    {
        public int AccountId { get; init; }
        public int CharacterId { get; init; }
        public PlaceShared Place { get; init; } = default!;
        public EndPointShared EndPoint { get; init; } = default!;
    }
}
