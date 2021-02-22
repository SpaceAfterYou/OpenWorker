using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ow.Framework.Database.Characters;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using SoulCore.IO.Network;
using System;
using System.Collections.Concurrent;

namespace ow.Service.District.Network.Sync
{
    public sealed class SyncServer : BaseServer<SyncServer, SyncSession>
    {
        internal static readonly SyncServer Empty = new();

        internal readonly BinTable Table = BinTable.Empty;
        internal readonly Instance Instance = Instance.Empty;
        internal readonly ChannelRepository Channels = ChannelRepository.Empty;
        internal readonly ConcurrentDictionary<int, SyncSession> Characters = new();
        internal readonly IDbContextFactory<CharacterContext> DatabaseCharacter;

        public SyncServer(IDbContextFactory<CharacterContext> databaseCharacter, IServiceProvider services, IConfiguration configuration) : base(services, GetIp(configuration), GetPort(configuration))
        {
            Table = new(configuration);
            Instance = new(configuration, Table);
            Channels = new(configuration, Instance);
            DatabaseCharacter = databaseCharacter;
        }

        private SyncServer() : base(null, "", 0)
        {
        }

        private static string GetIp(IConfiguration configuration) => configuration
            .GetValue<string>($"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Host:Ip");

        private static ushort GetPort(IConfiguration configuration) => configuration
            .GetValue<ushort>($"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Host:Port");
    }
}