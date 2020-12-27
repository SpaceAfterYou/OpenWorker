using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record DimensionBrodcastCharacterInResponse
    {
        public CharacterShared Character { get; init; } = default!;
        public PlaceShared Place { get; init; } = default!;
    }
}
