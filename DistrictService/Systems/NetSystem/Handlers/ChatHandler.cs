using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Chat;
using DistrictService.Systems.NetSystem.Repositories;
using SoulWorker.Types;

namespace DistrictService.Systems.NetSystem.Handlers
{
    internal static class ChatHandler
    {
        [Handler(HandlerOpcode.ChatReceiveMessage, HandlerPermission.Authorized)]
        public static void Receive(Session session, ReceiveRequest request, ChatCommandRepository commands)
        {
            if (request.Message.Length == 0) { return; }

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

            session.Channel.BrodcastChatMessage(session, request);
        }
    }
}