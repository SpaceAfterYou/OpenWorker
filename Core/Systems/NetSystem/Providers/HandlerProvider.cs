using Core.Systems.NetSystem.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Systems.NetSystem.Providers
{
    public delegate void Event(SwSession session, BinaryReader br);

    public class HandlerProvider : List<Event>
    {
        public HandlerProvider(IServiceProvider service, ILogger<HandlerProvider> logger) : base(GetHandlers(service, logger))
        {
        }

        private static void Dummy(SwSession _1, BinaryReader _2)
        {
        }

        private static List<Event> GetHandlers(IServiceProvider service, ILogger<HandlerProvider> logger)
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            List<Event> handlers = Enumerable.Repeat((Event)Dummy, ushort.MaxValue).ToList();

            foreach (var method in methods)
            {
                var handler = CreateEventHandler(service, method);
                var attribute = method.GetCustomAttribute<HandlerAttribute>();

                logger.LogDebug($"Used EVENT ({attribute.Opcode}) invoker on {method.DeclaringType?.FullName}.{method.Name}.");

                handlers[Convert.LeToBeUInt16((ushort)attribute.Opcode)] = handler;
            }

            return handlers;
        }

        private static Event CreateEventHandler(IServiceProvider service, MethodInfo method)
        {
            var session = Expression.Parameter(typeof(SwSession), "Session");
            var br = Expression.Parameter(typeof(BinaryReader), "BinaryReader");

            var arguments = method.GetParameters().Select(param =>
            {
                // In arguments not supported
                Debug.Assert(!param.IsIn);

                // Session typed parameter
                if (param.ParameterType == typeof(SwSession))
                {
                    return session as Expression;
                }

                // Packet structure parameter
                if (param.ParameterType.IsDefined(typeof(RequestAttribute)))
                {
                    var constructor = param.ParameterType.GetConstructor(new[] { typeof(BinaryReader) });
                    Debug.Assert(constructor is not null);

                    var @class = Expression.New(constructor, br);
                    return @class;
                }

                // Otherwise, get parameter from service collection
                var innerService = Expression.Constant(service);

                var getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", new[] { typeof(IServiceProvider), typeof(Type) });
                Debug.Assert(getServiceMethod is not null);

                var call = Expression.Call(null, getServiceMethod, innerService, Expression.Constant(param.ParameterType));
                var conv = Expression.Convert(call, param.ParameterType);
                return conv;
            }).ToArray();

            var caller = Expression.Call(null, method, arguments);
            return Expression.Lambda<Event>(caller, session, br).Compile();
        }
    }
}