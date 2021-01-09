using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;

namespace ow.Service.District.Network.Sync.Handlers
{
    public static class PartyHandler
    {
        [Handler(ServerOpcode.PartyAccept, HandlerPermission.Authorized)]
        public static void Accept(SyncSession session, PartyAcceptRequest request)
        {
        }

        [Handler(ServerOpcode.PartyInvite, HandlerPermission.Authorized)]
        public static void Invite(SyncSession session, PartyInviteRequest request)
        {
        }

        [Handler(ServerOpcode.PartyLeave, HandlerPermission.Authorized)]
        public static void Leave(SyncSession session)
        {
            session.SendAsync(new PartyDeleteResponse());
        }
    }
}