using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static SoulCore.IO.Network.Sync.Providers.HandlerProvider;
using static SoulCore.IO.Network.Sync.Providers.HandlerProvider.SHPEntity;

namespace SoulCore.IO.Network.Sync.Providers
{
    public class HandlerProvider : List<SHPEntity>
    {
        public sealed record SHPEntity
        {
            public delegate void SHPEMethod(SSessionBase session, BinaryReader br);

            public HandlerPermission Permission { get; init; }
            public SHPEMethod Method { get; init; } = default!;
        }

        public HandlerProvider(IServiceProvider service, ILogger<HandlerProvider> logger) : base(GetHandlers(service, logger))
        {
        }

        private static void Dummy(SSessionBase session, BinaryReader _)
        {
#if !DEBUG
            session.Disconnect();
#endif // !DEBUG
        }

        private static List<SHPEntity> GetHandlers(IServiceProvider service, ILogger<HandlerProvider> logger)
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            List<SHPEntity> handlers = Enumerable.Repeat(new SHPEntity { Permission = HandlerPermission.None, Method = Dummy }, GetMaxHandlersCount()).ToList();

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

                SHPEMethod handlerMethod = CreateHandlerMethod(instance, service, method);

                HandlerAttribute? attribute = method.GetCustomAttribute<HandlerAttribute>();
                Debug.Assert(attribute is not null);

                logger.LogDebug($"Used SYNC EVENT ({attribute.Opcode}) invoker on {method.DeclaringType!.FullName}.{method.Name}.");

                handlers[ConvertUtils.LeToBeUInt16((ushort)attribute.Opcode)] = new()
                {
                    Method = handlerMethod,
                    Permission = attribute.Permission
                };
            }

            return handlers;
        }

        private static SHPEMethod CreateHandlerMethod(object? instance, IServiceProvider service, MethodInfo method)
        {
            ParameterExpression session = Expression.Parameter(typeof(SSessionBase), "Session");
            ParameterExpression br = Expression.Parameter(typeof(BinaryReader), "BinaryReader");

            Expression[] arguments = method.GetParameters().Select(param =>
            {
                // In arguments not supported
                Debug.Assert(!param.IsIn);
                Debug.Assert(param.ParameterType is not null);

                // Session typed parameter
                if (param.ParameterType == typeof(SSessionBase) || param.ParameterType?.BaseType == typeof(SSessionBase))
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
            return Expression.Lambda<SHPEMethod>(caller, session, br).Compile();
        }

        private static int GetMaxHandlersCount() => Convert.ToInt32(Enum
            .GetUnderlyingType(typeof(ServerOpcode))?
            .GetField("MaxValue", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?
            .GetRawConstantValue());
    }
}
