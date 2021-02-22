using ow.Service.Login.Game.Gate;
using SoulCore.IO.Network;
using SoulCore.IO.Network.Attributes;

namespace ow.Service.Login.Network
{
    [SyncSession]
    public sealed class SyncSession : BaseSession<SyncServer, SyncSession>
    {
        internal Account Account { get; set; } = Account.Empty;
        internal CharacterList Characters { get; set; } = CharacterList.Empty;
        internal uint Background { get; set; }

        public SyncSession(SyncServer server) : base(server)
        {
        }
    }
}