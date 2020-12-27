using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.Game.Enums
{
    public record CharacterInfoResponse
    {
        public CharacterShared Character { get; init; } = default!;
        public PlaceShared Place { get; init; } = default!;
        public CharacterInfoResult Result { get; init; } = CharacterInfoResult.Positive;
    }
}