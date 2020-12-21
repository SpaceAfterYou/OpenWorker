using System.Collections.Generic;

namespace ow.Service.District.Network.Repositories
{
    internal delegate void ChatCommand(Session session, params string[] @params);

    internal class ChatCommandRepository : Dictionary<string, ChatCommand>
    {
    }
}