using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;

namespace OpenWorker.Hotspot.Handler;

public sealed class HandlerBuilder
{
    private const string HandlerMethod = "OnHandleAsync";

    private ParameterExpression Instance { get; } = Expression
        .Parameter(typeof(IHotspotHandler), nameof(Instance));

    private ParameterExpression Player { get; } = Expression
        .Parameter(typeof(object), nameof(Player));

    private ParameterExpression Reader { get; } = Expression
        .Parameter(typeof(BinaryReader), nameof(Reader));

    private ParameterExpression CancellationToken { get; } = Expression
        .Parameter(typeof(CancellationToken), nameof(CancellationToken));

    private Expression CreateArgument(ParameterInfo param)
    {
        if (param.ParameterType == typeof(CancellationToken))
        {
            return CancellationToken;
        }

        if (param.ParameterType == typeof(ServiceHandleContext))
        {
            var @params = new[] { Player, CancellationToken };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var constructor = param.ParameterType.GetConstructor(flags, null, @params.Select(e => e.Type).ToArray(), null);
            Debug.Assert(constructor is not null, $"{nameof(ServiceHandleContext)} constructor not found");

            return Expression.New(constructor, @params.Cast<Expression>());
        }

        if (param.ParameterType.GetCustomAttribute<HotspotMessageAttribute>() is not null)
        {
            var @params = new[] { Reader };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var constructor = param.ParameterType.GetConstructor(flags, null, @params.Select(e => e.Type).ToArray(), null);

            return constructor is null ?
                // messages without content may haven't BinaryReader param
                // so we need to create empty one
                Expression.New(param.ParameterType) :

                Expression.New(constructor, @params.Cast<Expression>());
        }

        throw new ArgumentException($"{param.ParameterType} Bad argument type", param.Name);
    }

    internal IEnumerable<CreatedHandler> CreateForType(Type @class)
    {
        return @class
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(e => e.Name == HandlerMethod).Select(method =>
            {
                Debug.Assert(method.IsStatic is false, "Static methods are not supported");

                var parameters = method.GetParameters();
                var arguments = parameters.Select(CreateArgument);

                var call = Expression.Call(Expression.Convert(Instance, @class), method, arguments);

                var message = parameters.First(param =>
                    param.ParameterType.GetCustomAttribute<HotspotMessageAttribute>() is not null);

                var attribute = message.ParameterType.GetCustomAttribute<HotspotMessageAttribute>();
                Debug.Assert(attribute is not null);

                var opcode = attribute.Opcode;
                var lambda = Expression
                    .Lambda<HandlerDelegate>(call, Instance, Player, Reader, CancellationToken)
                    .Compile();

                return new CreatedHandler(opcode, @class, lambda);
            });
    }
}