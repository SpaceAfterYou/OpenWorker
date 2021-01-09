using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ow.Framework.IO.Network.Relay
{
    public abstract class BaseRelayServer<THandler> : Server
        where THandler : Attribute
    {
        public BaseRelayServer(IConfigurationSection configuration, IServiceProvider services, ILogger logger)
        {
            _logger = logger;

            Ports.Add(CreateServerPort(configuration));

            foreach (ServerServiceDefinition handler in GetHandlers(services))
                Services.Add(handler);
        }

        private static ServerPort CreateServerPort(IConfigurationSection configuration) =>
            new(GetIp(configuration), GetPort(configuration), ServerCredentials.Insecure);

        private static string GetIp(IConfigurationSection configuration) =>
            configuration["Ip"];

        private static ushort GetPort(IConfigurationSection configuration) =>
            ushort.Parse(configuration["Port"]);

        private IEnumerable<ServerServiceDefinition> GetHandlers(IServiceProvider services) => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(s => s.IsDefined(typeof(THandler)))
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

        private readonly ILogger _logger;
    }
}