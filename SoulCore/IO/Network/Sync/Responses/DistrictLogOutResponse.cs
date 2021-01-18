using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Responses.Shared;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record DistrictLogOutResponse : SEndPointSharedResponse
    {
        public int Account { get; init; }
        public int Character { get; init; }
        public DistrictLogOutWay Way { get; init; } = DistrictLogOutWay.GoToGateService;
        public DistrictLogOutStatus Status { get; init; } = DistrictLogOutStatus.Success;
    }
}
