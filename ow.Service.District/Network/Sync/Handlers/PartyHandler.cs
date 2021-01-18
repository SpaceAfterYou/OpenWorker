﻿using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using ow.Service.District.Network.Relay;

namespace ow.Service.District.Network.Sync.Handlers
{
    public class PartyHandler
    {
        [Handler(ServerOpcode.PartyAccept, HandlerPermission.Authorized)]
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

        [Handler(ServerOpcode.PartyInvite, HandlerPermission.Authorized)]
        public void Invite(SyncSession session, PartyInviteRequest request)
        {
        }

        [Handler(ServerOpcode.PartyLeave, HandlerPermission.Authorized)]
        public void Leave(SyncSession session)
        {
            session.SendDeferred(new PartyDeleteResponse());
        }

        public PartyHandler(RWClient relay) =>
            _relay = relay;

        private readonly RWClient _relay;
    }
}
