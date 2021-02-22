using ow.Service.District.Network.Sync;
using System.Collections.Generic;

namespace ow.Service.District.Game.Repositories
{
    public delegate void ChatCommandHandler(SyncSession session, params string[] @params);

    public class ChatCommandRepository : Dictionary<string, ChatCommandHandler>
    {
    }
}