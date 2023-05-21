using Microsoft.Extensions.Logging;
using OpenWorker.Infrastructure.Communication.Hotspot.Extensions;
using SoulWorkerResearch.SoulCore;
using SoulWorkerResearch.SoulCore.IO.Net;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal sealed record HandlerCollection
{
    public HandlerCollection(ILogger<HandlerCollection> logger)
    {
        _logger = logger;
        _handlers = Build().ToImmutableArray();
    }

    internal Handler this[Opcode key] => _handlers[key];

    private IEnumerable<Handler> Build() => CreateHandlers().Aggregate(CreateEmpty().ToArray(), BuildHandlerInfo);

    private Handler[] BuildHandlerInfo(Handler[] handlers, CreatedHandler handler)
    {
        handlers[handler.Opcode] = new Handler(handler.Class, handler.Method);
        _logger.LogDebug("[ Handler ]: [{:X2}:{:X2}] {}", handler.Opcode.Group, handler.Opcode.Command, handler.Class.Name);

        return handlers;
    }

    private static IEnumerable<CreatedHandler> CreateHandlers() => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(TypeExtension.IsPacketHandler)
        .Select(@class => new HandlerBuilder().CreateHandlerForClass(@class));

    private static IEnumerable<Handler> CreateEmpty() => Enumerable
        .Repeat(new Handler(typeof(EmptyHandler), EmptyHandler.OnUnhandledAsync), GetMaxHandlersCount());

    private static int GetMaxHandlersCount()
    {
        var field = Config.PacketOpcodeType.GetField("MaxValue", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        Debug.Assert(field is not null);

        var value = field.GetRawConstantValue();
        Debug.Assert(Convert.ToDecimal(value) < int.MaxValue);

        return Convert.ToInt32(value);
    }

    private readonly IReadOnlyList<Handler> _handlers;
    private readonly ILogger _logger;
}
