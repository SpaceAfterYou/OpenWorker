using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework;
using ow.Framework.IO.Network;
using System;
using System.Net;

namespace ow.Service.Gate.Network
{
    public sealed class Server : GameServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, GetEndPoint(configuration))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();

        private static IPEndPoint GetEndPoint(IConfiguration configuration) =>
            IPEndPoint.Parse($"{ configuration.GetSection($"Gates:{configuration["Id"]}").Get<GateConfiguration>().Host}");
    }
}