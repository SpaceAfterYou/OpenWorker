using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Requests;
using SoulCore.IO.Network.Responses;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ChatHandler
    {
        [Handler(CategoryCommand.Chat, ChatCommand.Normal)]
        public static void Receive(SyncSession session, ChatReceiveRequest request)
        {
            session.Channel.BroadcastDeferred(new ChatMessageResponse()
            {
                Character = session.Character.Id,
                Chat = request.Type,
                Message = request.Message
            });
        }
    }
}