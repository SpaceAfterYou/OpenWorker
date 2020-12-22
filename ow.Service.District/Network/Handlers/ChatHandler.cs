using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Service.District.Network.Repositories;

namespace ow.Service.District.Network.Handlers
{
    internal static class ChatHandler
    {
        [Handler(ServerOpcode.ChatReceiveMessage, HandlerPermission.Authorized)]
        public static void Receive(Session session, ChatReceiveRequest request, ChatCommandRepository commands)
        {
            if (request.Message.Length == 0)
                return;

            if (request.Message.StartsWith('!'))
            {
                string[] msg = request.Message.Split(' ');
                if (commands.TryGetValue(msg[0], out var command))
                {
                    command(session, msg);

                    session.SendAsync(new ChatResponse()
                    {
                        Character = session.Character.Id,
                        Chat = ChatType.System,
                        Message = "Command executed"
                    });

                    return;
                }

                session.SendAsync(new ChatResponse()
                {
                    Character = session.Character.Id,
                    Chat = ChatType.Red,
                    Message = "Command not found"
                });
                return;
            }

            session.Dimension.BroadcastAsync(request);
        }
    }
}