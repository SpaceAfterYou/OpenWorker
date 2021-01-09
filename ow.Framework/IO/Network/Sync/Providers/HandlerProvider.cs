using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ow.Framework.IO.Network.Sync.Providers
{
    public delegate void Event(BaseSyncSession session, BinaryReader br);

    public class HandlerProvider : List<Event>
    {
        public HandlerProvider(IServiceProvider service, ILogger<HandlerProvider> logger) : base(GetHandlers(service, logger))
        {
        }

        private static void Dummy(BaseSyncSession session, BinaryReader _)
        {
#if !DEBUG
            session.Disconnect();
#endif // !DEBUG
        }

        private static List<Event> GetHandlers(IServiceProvider service, ILogger<HandlerProvider> logger)
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            List<Event> handlers = Enumerable.Repeat((Event)Dummy, GetMaxHandlersCount()).ToList();

            Dictionary<Type, object?> instances = new(methods.Count());

            foreach (MethodInfo method in methods)
            {
                object? instance = null;
                if (!method.IsStatic)
                {
                    if (method.DeclaringType is null)
                        Debug.Assert(false, "Cannot determe class type");

                    if (!instances.TryGetValue(method.DeclaringType, out instance))
                    {
                        if (method.DeclaringType.IsAbstract && method.DeclaringType.IsSealed)
                            Debug.Assert(false, "");

                        instance = service.GetRequiredService(method.DeclaringType);
                        instances.Add(method.DeclaringType, instance);
                    }
                }

                Event handler = CreateEventHandler(instance, service, method);

                HandlerAttribute? attribute = method.GetCustomAttribute<HandlerAttribute>();
                Debug.Assert(attribute is not null);

                logger.LogDebug($"Used SYNC EVENT ({attribute.Opcode}) invoker on {method.DeclaringType!.FullName}.{method.Name}.");

                handlers[ConvertUtils.LeToBeUInt16((ushort)attribute.Opcode)] = handler;
            }

            return handlers;
        }

        private static Event CreateEventHandler(object? instance, IServiceProvider service, MethodInfo method)
        {
            ParameterExpression session = Expression.Parameter(typeof(BaseSyncSession), "Session");
            ParameterExpression br = Expression.Parameter(typeof(BinaryReader), "BinaryReader");

            Expression[] arguments = method.GetParameters().Select(param =>
            {
                // In arguments not supported
                Debug.Assert(!param.IsIn);
                Debug.Assert(param.ParameterType is not null);

                // Session typed parameter
                if (param.ParameterType == typeof(BaseSyncSession) || param.ParameterType?.BaseType == typeof(BaseSyncSession))
                    return Expression.Convert(session, param.ParameterType) as Expression;

                // Packet structure parameter
                if (param.ParameterType!.IsDefined(typeof(RequestAttribute)))
                {
                    ConstructorInfo? constructor = param.ParameterType.GetConstructor(new[] { typeof(BinaryReader) });
                    Debug.Assert(constructor is not null);

                    NewExpression @class = Expression.New(constructor, br);
                    return @class;
                }

                // Otherwise, get parameter from service collection
                ConstantExpression innerService = Expression.Constant(service);

                MethodInfo? methodGetRequiredService = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", new[] { typeof(IServiceProvider), typeof(Type) });
                Debug.Assert(methodGetRequiredService is not null);

                MethodCallExpression call = Expression.Call(methodGetRequiredService, innerService, Expression.Constant(param.ParameterType));
                UnaryExpression conv = Expression.Convert(call, param.ParameterType);

                return conv;
            }).ToArray();

            MethodCallExpression caller = Expression.Call(instance is null ? null : Expression.Constant(instance), method, arguments);
            return Expression.Lambda<Event>(caller, session, br).Compile();
        }

        private static int GetMaxHandlersCount() => Convert.ToInt32(Enum
            .GetUnderlyingType(typeof(Opcodes.ServerOpcode))?
            .GetField("MaxValue", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?
            .GetRawConstantValue());
    }
}