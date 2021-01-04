using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ow.RelayService
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Server _server;

        public Worker(IConfiguration configuration, IServiceProvider services, ILogger<Worker> logger)
        {
            _logger = logger;
            _server = new()
            {
                Ports = { CreateServerPort(configuration) }
            };

            foreach (ServerServiceDefinition handler in GetHandlers(services))
                _server.Services.Add(handler);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken) =>
            await _server.ShutdownAsync();

        private static ServerPort CreateServerPort(IConfiguration configuration) =>
            new(configuration["Relay:Host:Ip"], int.Parse(configuration["Relay:Host:Port"]), ServerCredentials.Insecure);

        private IEnumerable<ServerServiceDefinition> GetHandlers(IServiceProvider services) => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(s => s.IsDefined(typeof(HandlerAttribute)))
            .Select(s => services.GetRequiredService(s))
            .Select(s =>
            {
                var o = new
                {
                    Handler = s,
                    Type = s.GetType()
                };

                foreach (MethodInfo method in o.Type.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance).Where(s => s.DeclaringType == o.Type))
                    _logger.LogDebug($"Used RELAY EVENT ({method.Name}) invoker on {method.DeclaringType!.FullName}.");

                return o;
            })
            .Select(s => (ServerServiceDefinition)s.Type.GetCustomAttribute<BindServiceMethodAttribute>()!.BindType.GetMethod("BindService", new Type[] { s.Type })?.Invoke(null, new object[] { s.Handler })!);
    }
}