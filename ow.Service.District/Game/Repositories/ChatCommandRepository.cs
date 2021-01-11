using ow.Service.District.Network.Sync;
using System.Collections.Generic;

namespace ow.Service.District.Game.Repositories
{
    public delegate void ChatCommand(SyncSession session, params string[] @params);

    public class ChatCommandRepository : Dictionary<string, ChatCommand>
    {
    }
}