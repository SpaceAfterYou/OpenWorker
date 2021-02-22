using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Responses;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ChannelHandler
    {
        [Handler(CategoryCommand.Channel, ChannelCommand.Info)]
        public static void GetInfo(SyncSession session) => session
            .SendAsync(new ChannelInfoResponse()
            {
                Location = session.Server.Instance.LocationId,
                Values = session.Server.Channels.Values.Select(s => new ChannelInfoResponse.Entity()
                {
                    Id = (ushort)(1 + s.Id),
                    Status = s.Status
                })
            });

        [Handler(CategoryCommand.Channel, ChannelCommand.Change)]
        public static void Switch(SyncSession _)
        {
        }
    }
}