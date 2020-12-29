using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network.Sync;
using System;

namespace ow.Service.District.Network.Sync
{
    public sealed class Server : SyncServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, configuration.GetSection($"Districts:{configuration.GetValue<string>("Id")}"))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();
    }
}