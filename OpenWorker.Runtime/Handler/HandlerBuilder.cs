using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// Compiles the call of every <c>OnHandleAsync</c> of a handler class into a <see cref="PacketHandlerDelegate"/>: the
/// call takes the parsed request, and <see cref="PacketHandler{TRequest}"/> parses it from the span of the packet before
/// the call. Every request reads itself through <see cref="PacketReader"/> (<see cref="IWireReadable{TSelf}"/>); a handler
/// of a request that does not stops the table from being built.
/// </summary>
public sealed class HandlerBuilder
{
    private const string HandlerMethod = "OnHandleAsync";

    private ParameterExpression Instance { get; } = Expression
        .Parameter(typeof(IHotspotHandler), nameof(Instance));

    private ParameterExpression Player { get; } = Expression
        .Parameter(typeof(Entity), nameof(Player));

    private ParameterExpression CancellationToken { get; } = Expression
        .Parameter(typeof(CancellationToken), nameof(CancellationToken));

    private Expression CreateArgument(ParameterInfo param, ParameterExpression request)
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

        if (param.ParameterType == request.Type)
        {
            return request;
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

                var message = method.GetParameters().First(param =>
                    param.ParameterType.GetCustomAttribute<HotspotMessageAttribute>() is not null).ParameterType;

                var attribute = message.GetCustomAttribute<HotspotMessageAttribute>();
                Debug.Assert(attribute is not null);

                return new CreatedHandler(attribute.Opcode, @class, CreatePacketHandler(@class, method, message));
            });
    }

    /// <summary>
    /// The call takes the request already parsed, and the request is parsed from the span.
    /// </summary>
    private PacketHandlerDelegate CreatePacketHandler(Type @class, MethodInfo method, Type message)
    {
        var readable = message.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWireReadable<>) && i.GetGenericArguments()[0] == message);

        if (!readable)
        {
            throw new InvalidOperationException(
                $"{@class.Name} handles {message.Name}, which does not read itself through PacketReader (IWireReadable).");
        }

        var request = Expression.Parameter(message, "Request");
        var arguments = method.GetParameters().Select(param => CreateArgument(param, request));
        var call = Expression.Call(Expression.Convert(Instance, @class), method, arguments);

        var signature = typeof(Func<,,,,>).MakeGenericType(
            typeof(IHotspotHandler), typeof(Entity), message, typeof(CancellationToken), typeof(ValueTask));

        var compiled = Expression.Lambda(signature, call, Instance, Player, request, CancellationToken).Compile();

        var create = typeof(PacketHandler<>).MakeGenericType(message).GetMethod(nameof(PacketHandler<>.Create));
        Debug.Assert(create is not null);

        return (PacketHandlerDelegate)create.Invoke(null, [compiled])!;
    }
}
