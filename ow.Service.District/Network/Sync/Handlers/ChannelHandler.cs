using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ChannelHandler
    {
        [Handler(ServerOpcode.ChannelInfo, HandlerPermission.Authorized)]
        public void GetInfo(Session session) => session
            .SendAsync(new ChannelInfoResponse()
            {
                Location = _instance.Location.Id,
                Values = _dimensions.Values.Select(s => new ChannelInfoResponse.Entity()
                {
                    Id = (ushort)(1 + s.Id),
                    Status = s.Status
                })
            });

        //[Handler(ServerOpcode.ChannelSwitch, HandlerPermission.Authorized)]
        //public static void Switch(Session session) => session;

        public ChannelHandler(Instance instance, DimensionRepository dimensions) =>
            (_instance, _dimensions) = (instance, dimensions);

        private readonly Instance _instance;
        private readonly DimensionRepository _dimensions;
    }
}