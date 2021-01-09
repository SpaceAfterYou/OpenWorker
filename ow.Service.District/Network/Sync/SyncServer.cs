using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network.Sync;
using System;

namespace ow.Service.District.Network.Sync
{
    public sealed class SyncServer : BaseSyncServer
    {
        public SyncServer(IServiceProvider services, IConfiguration configuration) : base(services, GetHost(configuration))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<SyncSession>();

        private static IConfigurationSection GetHost(IConfiguration configuration) =>
            configuration.GetSection($"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Host");
    }
}