using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Responses.Shared;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record DistrictLogOutResponse : EndPointShared
    {
        public int Account { get; init; }
        public int Character { get; init; }
        public DistrictLogOutWay Way { get; init; } = DistrictLogOutWay.GoToGateService;
        public DistrictLogOutStatus Status { get; init; } = DistrictLogOutStatus.Success;
    }
}