using Grpc.Core;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.World.Server.Protos.Requests;
using ow.Framework.IO.Network.Relay.World.Server.Protos.Responses;
using ow.Service.Relay.World.Repository;
using System.Threading.Tasks;

using static ow.Framework.IO.Network.Relay.World.Server.Protos.RWSPartyProto;

namespace ow.Service.Relay.World.Network.Handlers
{
    [WorldHandler]
    internal class PartyHandler : RWSPartyProtoBase
    {
        private readonly ILogger<PartyHandler> _logger;

        private readonly PartyRepository _parties;

        public PartyHandler(PartyRepository parties, ILogger<PartyHandler> logger) =>
            (_parties, _logger) = (parties, logger);

        public override Task<RWSPartyAcceptResponse> Accept(RWSPartyAcceptRequest request, ServerCallContext context)
        {
            return Task.FromResult<RWSPartyAcceptResponse>(new());
        }
    }
}