using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ow.Framework.IO.Network.Providers
{
    public delegate void Event(GameSession session, BinaryReader br);

    public class HandlerProvider : List<Event>
    {
        public HandlerProvider(IServiceProvider service, ILogger<HandlerProvider> logger) : base(GetHandlers(service, logger))
        {
        }

        private static void Dummy(GameSession session, BinaryReader _)
        {
#if !DEBUG
            session.Disconnect();
#endif // !DEBUG
        }

        private static List<Event> GetHandlers(IServiceProvider service, ILogger<HandlerProvider> logger)
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            List<Event> handlers = Enumerable.Repeat((Event)Dummy, ushort.MaxValue).ToList();

            foreach (MethodInfo method in methods)
            {
                Event handler = CreateEventHandler(service, method);
                HandlerAttribute attribute = method.GetCustomAttribute<HandlerAttribute>();

                logger.LogDebug($"Used EVENT ({attribute.Opcode}) invoker on {method.DeclaringType?.FullName}.{method.Name}.");

                handlers[ConvertUtils.LeToBeUInt16((ushort)attribute.Opcode)] = handler;
            }

            return handlers;
        }

        private static Event CreateEventHandler(IServiceProvider service, MethodInfo method)
        {
            ParameterExpression session = Expression.Parameter(typeof(GameSession), "Session");
            ParameterExpression br = Expression.Parameter(typeof(BinaryReader), "BinaryReader");

            Expression[] arguments = method.GetParameters().Select(param =>
            {
                // In arguments not supported
                Debug.Assert(!param.IsIn);

                // Session typed parameter
                if (param.ParameterType.IsSubclassOf(typeof(GameSession)))
                {
                    return Expression.Convert(session, param.ParameterType) as Expression;
                }

                // Packet structure parameter
                if (param.ParameterType.IsDefined(typeof(RequestAttribute)))
                {
                    ConstructorInfo constructor = param.ParameterType.GetConstructor(new[] { typeof(BinaryReader) });
                    Debug.Assert(constructor is not null);

                    NewExpression @class = Expression.New(constructor, br);
                    return @class;
                }

                // Otherwise, get parameter from service collection
                ConstantExpression innerService = Expression.Constant(service);

                MethodInfo getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", new[] { typeof(IServiceProvider), typeof(Type) });
                Debug.Assert(getServiceMethod is not null);

                MethodCallExpression call = Expression.Call(null, getServiceMethod, innerService, Expression.Constant(param.ParameterType));
                UnaryExpression conv = Expression.Convert(call, param.ParameterType);
                return conv;
            }).ToArray();

            var caller = Expression.Call(null, method, arguments);
            return Expression.Lambda<Event>(caller, session, br).Compile();
        }
    }
}
