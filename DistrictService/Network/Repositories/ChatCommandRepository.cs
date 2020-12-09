using System.Collections.Generic;

namespace DistrictService.Network.Repositories
{
    public delegate void ChatCommand(Session session, params string[] @params);

    public class ChatCommandRepository : Dictionary<string, ChatCommand>
    {
    }
}