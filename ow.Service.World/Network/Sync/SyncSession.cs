using ow.Service.District.Game;
using SoulCore.IO.Network;
using System.Collections.Generic;

namespace ow.Service.District.Network.Sync
{
    public sealed class SyncSession : BaseSession<SyncServer, SyncSession>
    {
        internal static readonly SyncSession Empty = new();

        internal Account Account { get; set; } = Account.Empty;
        internal Character Character { get; set; } = Character.Empty;
        internal Profile Profile { get; set; } = Profile.Empty;
        internal ChannelMember Channel { get; set; } = ChannelMember.Empty;
        internal Storages Storages { get; set; } = Storages.Empty;
        internal readonly SpecialOptions SpecialOptions = new();
        internal readonly Stats Stats = new();

        public SyncSession(SyncServer server) : base(server)
        {
        }

        protected override void OnDisconnected()
        {
            Server.Characters.Remove(Character.Id, out _);

            Channel.Leave();
        }

        private SyncSession() : base(SyncServer.Empty)
        {
        }
    }
}