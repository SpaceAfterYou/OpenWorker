using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct DistrictLogoutRequest
    {
        public int Character { get; }
        public int Account { get; }
        public DistrictLogOutWay Way { get; }

        public DistrictLogoutRequest(BinaryReader br) => (Character, Account, Way) = (br.ReadInt32(), br.ReadInt32(), br.ReadLogoutWayType());
    }
}
