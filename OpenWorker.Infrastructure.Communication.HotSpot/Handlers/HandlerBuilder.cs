using DefaultEcs;
using SoulWorkerResearch.SoulCore.IO.Net;
using SoulWorkerResearch.SoulCore.IO.Net.Attributes;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal sealed class HandlerBuilder
{
    internal CreatedHandler CreateHandlerForClass(Type @class)
    {
        var method = CreateContext(@class);
        return new CreatedHandler(method.Opcode, @class, method.Method);
    }

    private Expression CreateArgument(ParameterInfo param)
    {
        Debug.Assert(!param.IsIn, "In arguments not supported");
        Debug.Assert(!param.IsOut, "Out arguments not supported");

        if (param.ParameterType == typeof(CancellationToken))
        {
            return _ct;
        }

        if (param.ParameterType == typeof(Entity))
        {
            return _entity;
        }

        if (param.ParameterType.GetCustomAttribute<ServerMessageAttribute>() is ServerMessageAttribute message)
        {
            _opcode = message.Opcode;

            var @params = new[] { typeof(BinaryReader) };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var constructor = param.ParameterType.GetConstructor(flags, null, @params, null);
            return constructor is null ? Expression.New(param.ParameterType) : Expression.New(constructor, _reader);
        }

        throw new ApplicationException($"{param.ParameterType} Bad argument type");
    }

    private CreatedContext CreateContext(Type @class)
    {
        var method = @class.GetMethod(HANDLER_METHOD, BindingFlags.Instance | BindingFlags.Public);
        Debug.Assert(method is not null, $"Method {HANDLER_METHOD} not found");

        var arguments = method.GetParameters().Select(CreateArgument);

        var call = Expression.Call(method.IsStatic ? null : Expression.Convert(_instance, @class), method, arguments);
        var lambda = Expression.Lambda<HandlerMethod>(call, _instance, _entity, _reader, _ct).Compile();

        Debug.Assert(_opcode != Opcode.Empty);
        return new CreatedContext(_opcode, lambda);
    }

    private Opcode _opcode = Opcode.Empty;

    private readonly ParameterExpression _instance = Expression.Parameter(typeof(object), "Instance");
    private readonly ParameterExpression _entity = Expression.Parameter(typeof(Entity), "Session");
    private readonly ParameterExpression _reader = Expression.Parameter(typeof(BinaryReader), "BinaryReader");
    private readonly ParameterExpression _ct = Expression.Parameter(typeof(CancellationToken), "CancellationToken");

    private const string HANDLER_METHOD = "OnHandleAsync";
}
