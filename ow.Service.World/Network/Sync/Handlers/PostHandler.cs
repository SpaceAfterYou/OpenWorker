using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Responses;

namespace ow.Service.District.Network.Sync.Handlers
{
    public static class PostHandler
    {
        [Handler(ServerOpcode.PostInfo)]
        public static void GetPostInfo(SyncSession session) => session
            .SendAsync(new PostInfoResponse() { });
    }
}
