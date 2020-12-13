using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using System;
using System.Net;

namespace ow.Framework.IO.Network
{
    public class GameServer : TcpServer
    {
        private readonly IServiceProvider _services;

        public GameServer(IServiceProvider services, IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"])) =>
            _services = services;

        protected override TcpSession CreateSession() => _services.GetRequiredService<GameSession>();
    }
}