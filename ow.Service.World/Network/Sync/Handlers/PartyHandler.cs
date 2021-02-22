using ow.Service.District.Network.Relay;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Permissions;
using SoulCore.IO.Network.Requests;
using SoulCore.IO.Network.Responses;

namespace ow.Service.District.Network.Sync.Handlers
{
    public class PartyHandler
    {
        private readonly RWClient _relay;

        [Handler(CategoryCommand.Party, PartyCommand.Accept)]
        public void Accept(SyncSession session, PartyAcceptRequest request)
        {
            //var response = _relay.Party.Accept(new Framework.IO.Network.Relay.Protos.Requests.PartyAcceptRequest()
            //{
            //    Master = session.Character.Id,
            //    Character = request.Character
            //});

            //if (response.Result)
            //{
            //    session.SendAsync(new PartyAcceptResponse { Character = session. });
            //}
        }

        [Handler(CategoryCommand.Party, PartyCommand.Invite)]
        public void Invite(SyncSession session, PartyInviteRequest request)
        {
        }

        [Handler(CategoryCommand.Party, PartyCommand.Leave)]
        public void Leave(SyncSession session)
        {
            session.SendAsync(new PartyDeleteResponse());
        }

        public PartyHandler(RWClient relay) => _relay = relay;
    }
}