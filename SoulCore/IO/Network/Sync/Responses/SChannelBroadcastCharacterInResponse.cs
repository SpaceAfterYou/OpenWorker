using SoulCore.IO.Network.Sync.Responses.Shared;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record SChannelBroadcastCharacterInResponse
    {
        public CharacterShared Character { get; init; } = default!;
        public PlaceShared Place { get; init; } = default!;
    }
}
