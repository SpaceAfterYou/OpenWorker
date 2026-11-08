using Microsoft.Extensions.Logging;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Extensions;

namespace OpenWorker.Hotspot.Handler;

public sealed partial class HandlerTableBuilder(HandlerBuilder builder, ILogger<HandlerTable> logger)
{
    internal static IEnumerable<Type> InDomain =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(TypeExtension.IsHotspotHandler);

    internal Handler[] Build()
    {
        return CollectAndCreate().Aggregate(CreateEmptyCollection().ToArray(), Fill);
    }

    private Handler[] Fill(Handler[] list, IEnumerable<CreatedHandler> exists)
    {
        foreach (var handler in exists)
        {
            list[handler.Opcode] = new Handler(handler.Class, handler.Delegate);
            LogFoundHandler(handler.Opcode, handler.Class.Name);
        }

        return list;
    }

    private IEnumerable<IEnumerable<CreatedHandler>> CollectAndCreate()
    {
        return InDomain.Select(builder.CreateForType);
    }

    private static IEnumerable<Handler> CreateEmptyCollection()
    {
        var type = typeof(EmptyHandler);
        var length = GetMaxHandlerCount();

        return Enumerable.Repeat(new Handler(type, EmptyHandler.OnUnhandledAsync), length);
    }

    /// <summary>
    ///     @see <see cref="MessageOpcode.Value" />
    /// </summary>
    /// <returns></returns>
    private static int GetMaxHandlerCount()
    {
        return short.MaxValue;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "[ Found Hotspot Handler ]: [{Opcode}] {Name}")]
    private partial void LogFoundHandler(MessageOpcode opcode, string name);
}