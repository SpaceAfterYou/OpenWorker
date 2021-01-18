using Microsoft.Extensions.DependencyInjection;
using SoulCore.IO.Network.Relay.Attrubutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SoulCore.IO.Network.Relay.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddRelayHandlers(this IServiceCollection services)
        {
            foreach (Type handler in GetHandlers())
                services.AddTransient(handler);

            return services;
        }

        private static IEnumerable<Type> GetHandlers() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsDefined(typeof(GlobalHandlerAttribute)) || type.IsDefined(typeof(WorldHandlerAttribute)));
    }
}
