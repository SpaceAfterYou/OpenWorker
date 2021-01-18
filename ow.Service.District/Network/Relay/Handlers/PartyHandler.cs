using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SoulCore.IO.Network.Relay.Attrubutes;
using SoulCore.IO.Network.Relay.World.Client.Protos.Requests;
using SoulCore.IO.Network.Sync.Responses;
using ow.Service.District.Network.Sync;
using System;
using System.Threading.Tasks;
using static SoulCore.IO.Network.Relay.World.Client.Protos.RWCPartyProto;

namespace ow.Service.District.Network.Relay.Handlers
{
    [WorldHandler]
    public sealed class PartyHandler : RWCPartyProtoBase
    {
        public override Task<Empty> Invite(RWCPartyInviteRequest request, ServerCallContext context)
        {
            SyncSession session = (SyncSession)_syncServer.FindSession(new Guid(request.GUID));
            if (session is not null)
            {
                session.SendDeferred(new SPartyInviteResponse
                {
                    Master = new() { Id = request.Master.Id, Name = request.Master.Name },
                    Member = new() { Id = session.Character.Id, Name = session.Character.Name }
                });
            }

            return Task.FromResult(new Empty { });
        }

        public PartyHandler(SyncServer syncServer)
        {
            _syncServer = syncServer;
        }

        private readonly SyncServer _syncServer;
    }
}
