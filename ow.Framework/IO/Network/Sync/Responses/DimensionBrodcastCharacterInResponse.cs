using ow.Framework.IO.Network.Responses.Shared;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record DimensionBrodcastCharacterInResponse
    {
        public CharacterShared Character { get; init; } = default!;
        public PlaceShared Place { get; init; } = default!;
    }
}