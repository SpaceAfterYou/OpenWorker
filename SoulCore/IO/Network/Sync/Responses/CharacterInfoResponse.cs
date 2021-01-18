using SoulCore.IO.Network.Sync.Responses.Shared;

namespace SoulCore.IO.Network.Sync.Responses
{
    public record CharacterInfoResponse
    {
        public CharacterShared Character { get; init; } = default!;
        public PlaceShared Place { get; init; } = default!;
        public CharacterInfoResult Result { get; init; } = CharacterInfoResult.Positive;
    }
}
