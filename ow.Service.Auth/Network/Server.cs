using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network.Sync;
using System;

namespace ow.Service.Auth.Network
{
    public sealed class Server : SyncServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, configuration.GetSection("Auth"))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();
    }
}