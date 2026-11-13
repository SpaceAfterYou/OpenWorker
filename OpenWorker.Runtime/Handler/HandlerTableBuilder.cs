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
        return Number(CollectAndCreate().Aggregate(CreateEmptyCollection().ToArray(), Fill));
    }

    /// <summary>
    /// Gives every class of handler its place among the instances, in the order the classes first appear; the empty
    /// handler is first.
    /// </summary>
    private static Handler[] Number(Handler[] list)
    {
        var places = new Dictionary<Type, int>();

        for (var index = 0; index < list.Length; index++)
        {
            var handler = list[index];

            if (!places.TryGetValue(handler.Class, out var place))
            {
                place = places.Count;
                places.Add(handler.Class, place);
            }

            list[index] = handler with { Instance = place };
        }

        return list;
    }

    private Handler[] Fill(Handler[] list, IEnumerable<CreatedHandler> exists)
    {
        foreach (var handler in exists)
        {
            list[handler.Opcode.Value] = new Handler(handler.Class, handler.Packet, handler.Opcode.ToFullString());
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

        return Enumerable.Repeat(new Handler(type, EmptyHandler.OnUnhandledAsync, Handler.Unhandled), length);
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