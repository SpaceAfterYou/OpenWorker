using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network;
using System;

namespace ow.Service.Login.Network
{
    public class Server : GameServer
    {
        public Server(IConfiguration configuration, IServiceProvider services) : base(configuration) =>
            _services = services;

        protected override TcpSession CreateSession() => _services.GetRequiredService<Session>();

        private readonly IServiceProvider _services;
    }
}
