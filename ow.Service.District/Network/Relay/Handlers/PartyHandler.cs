using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.World.Client.Protos.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Network.Sync;
using System;
using System.Threading.Tasks;
using static ow.Framework.IO.Network.Relay.World.Client.Protos.RWCPartyProto;

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
                session.SendAsync(new SPartyInviteResponse
                {
                    Master = new() { Id = request.Master.Id, Name = request.Master.Name },
                    Member = new() { Id = session.Character.Id, Name = session.Character.Name }
                });
            }

            return Task.FromResult(new Empty());
        }

        public PartyHandler(SyncServer syncServer)
        {
            _syncServer = syncServer;
        }

        private readonly SyncServer _syncServer;
    }
}