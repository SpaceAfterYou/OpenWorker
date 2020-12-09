using ow.Framework.IO.Network;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using System;

namespace GateService.Network
{
    public sealed class Server : SwServer
    {
        public Server(IConfiguration configuration, IServiceProvider services) : base(configuration) =>
            _services = services;

        protected override TcpSession CreateSession() =>
            _services.GetRequiredService<Session>();

        private readonly IServiceProvider _services;
    }
}
