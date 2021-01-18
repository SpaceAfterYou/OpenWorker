﻿using Grpc.Core;
using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Relay.Attrubutes;
using SoulCore.IO.Network.Relay.World.Server.Protos.Requests;
using SoulCore.IO.Network.Relay.World.Server.Protos.Responses;
using ow.Service.World.Game;
using System.Threading.Tasks;
using static SoulCore.IO.Network.Relay.World.Server.Protos.RWSPartyProto;

namespace ow.Service.World.Network.Relay.Handlers
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
            return Task.FromResult(new RWSPartyAcceptResponse { });
        }
    }
}
