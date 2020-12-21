using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network;
using System;

namespace ow.Service.Login.Network
{
    public sealed class Server : GameServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, configuration)
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();
    }
}