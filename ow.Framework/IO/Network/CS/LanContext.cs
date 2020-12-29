using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Lan.Attrubutes;
using ow.Framework.IO.Lan.Exceptions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ow.Framework.IO.Lan
{
    public class LanContext : IDisposable
    {
        public ulong SetAccountIdBySessionKey(int accountId)
        {
            ulong sessionKey = (ulong)DateTime.UtcNow.Ticks;

            if (!_multiplexer.GetDatabase().HashSet("SessionKey", sessionKey.ToString(), accountId))
                throw new LanException();

            return sessionKey;
        }

        public int GetAccountIdBySessionKey(ulong sessionKey)
        {
            RedisValue value = _multiplexer.GetDatabase().HashGet("SessionKey", sessionKey.ToString());
            if (value.IsNullOrEmpty) throw new LanException();

            return (int)value;
        }

        public LanContext(IServiceProvider service, ILogger<LanContext> logger, ConnectionMultiplexer multiplexer)
        {
            _multiplexer = multiplexer;

            RegisterHandlers(service, logger, multiplexer);
        }

        private static void RegisterHandlers(IServiceProvider service, ILogger<LanContext> logger, ConnectionMultiplexer multiplexer)
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            foreach (MethodInfo method in methods)
            {
                Handler handler = CreateEventHandler(service, method);
                HandlerAttribute attribute = method.GetCustomAttribute<HandlerAttribute>() ?? throw new ApplicationException();

                logger.LogDebug($"Used LAN EVENT ({attribute.Channel}) invoker on {method.DeclaringType?.FullName}.{method.Name}.");

                multiplexer.GetSubscriber().Subscribe(attribute.Channel, (channel, value) =>
                {
                    using MemoryStream ms = new(value);
                    using BinaryReader br = new(ms);

                    handler(br);
                });
            }
        }

        private static Handler CreateEventHandler(IServiceProvider service, MethodInfo method)
        {
            ParameterExpression br = Expression.Parameter(typeof(BinaryReader), "BinaryReader");

            Expression[] arguments = method.GetParameters().Select<ParameterInfo, Expression>(param =>
            {
                // In arguments not supported
                Debug.Assert(!param.IsIn);

                // Packet structure parameter
                if (param.ParameterType.IsDefined(typeof(RequestAttribute)))
                {
                    ConstructorInfo constructor = param.ParameterType.GetConstructor(new[] { typeof(BinaryReader) }) ?? throw new ApplicationException();
                    Debug.Assert(constructor is not null);

                    NewExpression @class = Expression.New(constructor, br);
                    return @class;
                }

                // Otherwise, get parameter from service collection
                ConstantExpression innerService = Expression.Constant(service);

                MethodInfo getServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", new[] { typeof(IServiceProvider), typeof(Type) }) ?? throw new ApplicationException();
                Debug.Assert(getServiceMethod is not null);

                MethodCallExpression call = Expression.Call(null, getServiceMethod, innerService, Expression.Constant(param.ParameterType));
                UnaryExpression conv = Expression.Convert(call, param.ParameterType);
                return conv;
            }).ToArray();

            MethodCallExpression caller = Expression.Call(null, method, arguments);
            return Expression.Lambda<Handler>(caller, br).Compile();
        }

        public void Dispose()
        {
            _multiplexer.Dispose();
            GC.SuppressFinalize(this);
        }

        private delegate void Handler(BinaryReader br);

        private readonly ConnectionMultiplexer _multiplexer;
    }
}