using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network.Sync;
using System;

namespace ow.Service.Gate.Network
{
    public sealed class Server : SyncServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, configuration.GetSection($"Gates:{configuration["Id"]}"))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();
    }
}