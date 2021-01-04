using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ow.Framework.IO.Network.Relay.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddRelayChannel(this IServiceCollection services) => services
            .AddTransient<RelayChannel>();

        public static IServiceCollection AddRelayHandlers(this IServiceCollection services)
        {
            foreach (Type handler in GetHandlers())
                services.AddTransient(handler);

            return services;
        }

        private static IEnumerable<Type> GetHandlers() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsDefined(typeof(HandlerAttribute)));
    }
}