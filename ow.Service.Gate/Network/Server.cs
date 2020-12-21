using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework;
using ow.Framework.IO.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ow.Service.Gate.Network
{
    public sealed class Server : GameServer
    {
        public Server(IServiceProvider services, IConfiguration configuration) : base(services, GetEndPoint(configuration))
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();

        private static IPEndPoint GetEndPoint(IConfiguration configuration)
        {
            ushort gateId = configuration.GetValue<ushort>("Gate");

            GateConfiguration gate = configuration
                .GetSection("Gates")
                .Get<IReadOnlyList<GateConfiguration>>()
                .First(s => s.Id == gateId);

            return IPEndPoint.Parse($"{gate.Host}");
        }
    }
}