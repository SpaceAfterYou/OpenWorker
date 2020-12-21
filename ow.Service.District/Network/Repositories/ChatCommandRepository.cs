using System.Collections.Generic;

namespace ow.Service.District.Network.Repositories
{
    public delegate void ChatCommand(Session session, params string[] @params);

    public class ChatCommandRepository : Dictionary<string, ChatCommand>
    {
    }
}