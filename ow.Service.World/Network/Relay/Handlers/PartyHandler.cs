using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.World.Client.Protos.Requests;
using ow.Service.District.Network.Sync;
using SoulCore.IO.Network.Responses;
using System.Threading.Tasks;
using static ow.Framework.IO.Network.Relay.World.Client.Protos.RWCPartyProto;

namespace ow.Service.District.Network.Relay.Handlers
{
    [WorldHandler]
    public sealed class PartyHandler : RWCPartyProtoBase
    {
        private readonly SyncServer _syncServer;

        public override Task<Empty> Invite(RWCPartyInviteRequest request, ServerCallContext context)
        {
            if (_syncServer.Characters.TryGetValue(request.MemberCharacterId, out SyncSession? session))
                session.SendAsync(new SPartyInviteResponse
                {
                    Master = new() { Id = request.Master.Id, Name = request.Master.Name },
                    Member = new() { Id = session.Character.Id, Name = session.Character.Name }
                });

            return Task.FromResult(new Empty());
        }

        public PartyHandler(SyncServer syncServer)
        {
            _syncServer = syncServer;
        }
    }
}