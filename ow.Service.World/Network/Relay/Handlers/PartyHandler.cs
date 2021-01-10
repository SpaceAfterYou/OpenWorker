using Grpc.Core;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.Protos;
using ow.Framework.IO.Network.Relay.Protos.Requests;
using ow.Framework.IO.Network.Relay.Protos.Responses;
using ow.Service.World.Game;
using System.Threading.Tasks;

namespace ow.Service.World.Network.Relay.Handlers
{
    [WorldHandler]
    internal class PartyHandler : PartyService.PartyServiceBase
    {
        private readonly ILogger<PartyHandler> _logger;

        private readonly PartyRepository _parties;

        public PartyHandler(PartyRepository parties, ILogger<PartyHandler> logger) =>
            (_parties, _logger) = (parties, logger);

        public override Task<PartyAcceptResponse> Accept(PartyAcceptRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PartyAcceptResponse { });
        }
    }
}