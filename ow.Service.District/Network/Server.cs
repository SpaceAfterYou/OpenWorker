using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using ow.Framework.IO.Network;
using System;

namespace ow.Service.District.Network
{
    internal sealed class Server : GameServer
    {
        internal Server(IServiceProvider services, IConfiguration configuration) : base(services, configuration)
        {
        }

        protected override TcpSession CreateSession() => Services.GetRequiredService<Session>();
    }
}