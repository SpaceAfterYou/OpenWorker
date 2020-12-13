using ow.Framework.IO.Network;
using System.Collections.Generic;

namespace ow.Service.District.Network.Repositories
{
    public delegate void ChatCommand(GameSession session, params string[] @params);

    public class ChatCommandRepository : Dictionary<string, ChatCommand>
    {
    }
}