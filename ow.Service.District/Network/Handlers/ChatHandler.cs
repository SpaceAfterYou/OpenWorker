using ow.Framework.Game.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Service.District.Network.Repositories;

namespace ow.Service.District.Network.Handlers
{
    internal static class ChatHandler
    {
        [Handler(ServerOpcode.ChatReceiveMessage, HandlerPermission.Authorized)]
        public static void Receive(GameSession session, ReceiveRequest request, ChatCommandRepository commands)
        {
            if (request.Message.Length == 0)
                return;

            if (request.Message.StartsWith('!'))
            {
                string[] msg = request.Message.Split(' ');
                if (commands.TryGetValue(msg[0], out var command))
                {
                    command(session, msg);
                    session.SendChatMessage(ChatType.System, "Command executed");
                    return;
                }

                session.SendChatMessage(ChatType.Red, "Command not found");
                return;
            }

            session.Entity.Get<DimensionEntity>().BroadcastChatMessage(session, request);
        }
    }
}