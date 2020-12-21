using ow.Framework.Game;
using ow.Framework.IO.Network.Responses.Shared;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record DistrictEnterResponse
    {
        public DistrictConnectResult Result { get; init; } = DistrictConnectResult.Yes;
        public PlaceShared Place { get; init; } = default!;
    }
}