using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Network.Sync.Repositories;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ChatHandler
    {
        [Handler(ServerOpcode.ChatReceiveMessage, HandlerPermission.Authorized)]
        public void Receive(Session session, ChatReceiveRequest request)
        {
            if (request.Message.Length == 0)
                return;

            if (request.Message.StartsWith('!'))
            {
                string[] msg = request.Message.Split(' ');
                if (_repository.TryGetValue(msg[0], out var command))
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

            session.Dimension?.BroadcastAsync(request);
        }

        public ChatHandler(ChatCommandRepository commands) => _repository = commands;

        private readonly ChatCommandRepository _repository;
    }
}