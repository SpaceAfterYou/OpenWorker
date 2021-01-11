using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record SGateCharacterSelectResponse
    {
        public int Account { get; init; } = -1;
        public int Character { get; init; } = -1;
        public PlaceShared Place { get; init; } = new();
        public EndPointShared EndPoint { get; init; } = new();
    }
}