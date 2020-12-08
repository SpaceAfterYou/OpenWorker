using Core.Systems.LanSystem.Attrubutes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Systems.LanSystem
{
    public class Runner
    {
        private delegate void Handler(BinaryReader br);

        private readonly IServiceProvider _service;
        private readonly ILogger<Runner> _logger;
        private readonly ConnectionMultiplexer _multiplexer;

        public void Run()
        {
            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));

            foreach (MethodInfo method in methods)
            {
                Handler handler = CreateEventHandler(_service, method);
                HandlerAttribute attribute = method.GetCustomAttribute<HandlerAttribute>();

                _logger.LogDebug($"Used EVENT ({attribute.Channel}) invoker on {method.DeclaringType?.FullName}.{method.Name}.");

                _multiplexer.GetSubscriber().Subscribe(attribute.Channel, (RedisChannel channel, RedisValue value) =>
                {
                    using MemoryStream ms = new(value);
                    using BinaryReader br = new(ms);

                    handler(br);
                });
            }
        }

        public Runner(IServiceProvider service, ILogger<Runner> logger, ConnectionMultiplexer multiplexer)
        {
            _service = service;
            _logger = logger;
            _multiplexer = multiplexer;
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

            MethodCallExpression caller = Expression.Call(null, method, arguments);
            return Expression.Lambda<Handler>(caller, br).Compile();
        }
    }
}