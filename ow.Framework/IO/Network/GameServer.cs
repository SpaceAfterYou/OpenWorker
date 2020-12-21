using Microsoft.Extensions.Configuration;
using NetCoreServer;
using System;
using System.Net;

namespace ow.Framework.IO.Network
{
    public abstract class GameServer : TcpServer
    {
        protected readonly IServiceProvider Services;

        protected GameServer(IServiceProvider services, IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"])) =>
            Services = services;
    }
}