using SoulCore.Game;
using SoulCore.IO.Network.Sync.Responses.Shared;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record DistrictEnterResponse
    {
        public DistrictConnectResult Result { get; init; } = DistrictConnectResult.Yes;
        public PlaceShared Place { get; init; } = default!;
    }
}
