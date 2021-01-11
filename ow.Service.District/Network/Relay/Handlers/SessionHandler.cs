using Grpc.Core;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.World.Client.Protos.Requests;
using ow.Framework.IO.Network.Relay.World.Client.Protos.Responses;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Sync;
using System.Threading.Tasks;
using static ow.Framework.IO.Network.Relay.World.Client.Protos.RWCSessionProto;

namespace ow.Service.District.Network.Relay.Handlers
{
    [WorldHandler]
    public sealed class SessionHandler : RWCSessionProtoBase
    {
        public override Task<RWCSessionReserveResponse> Reserve(RWCSessionReserveRequest request, ServerCallContext context) => Task
            .FromResult(new RWCSessionReserveResponse
            {
                Result = !_sync.Characters.ContainsKey(request.Character) && _channels.TryReserve(request.Character)
            });

        public SessionHandler(SyncServer sync, ChannelRepository channels) =>
            (_sync, _channels) = (sync, channels);

        private readonly SyncServer _sync;
        private readonly ChannelRepository _channels;
    }
}