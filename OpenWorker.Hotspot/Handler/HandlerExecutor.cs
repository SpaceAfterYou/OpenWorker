using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Handler;

public sealed partial class HandlerExecutor(
    HandlerTable collection,
    IServiceProvider provider,
    World world,
    HandlerErrorBudget budget,
    ILogger<HandlerExecutor> logger,
    HandlerMetrics? metrics = null,
    PacketRateLimiter? limiter = null)
{
    private const int OpcodeSize = sizeof(short);

    /// <summary>
    /// The handlers are singletons: each class is resolved the first time a packet needs it and kept, in the place the
    /// table gives it. Resolving them all when the table is built would make every handler of the loaded assemblies,
    /// those whose services this server does not register among them.
    /// </summary>
    private readonly IHotspotHandler?[] _instances = new IHotspotHandler?[collection.Classes];

    // Readers that no packet uses. A handler holds its reader until it returns, awaits included, and other packets run on
    // the loop meanwhile, so there are as many as packets in flight at once. Usually that is one, the spare, taken and
    // given back with one atomic exchange; the others wait in the stack.
    private RequestReader? _spare;
    private readonly Stack<RequestReader> _readers = new();
    private readonly Lock _readersLock = new();

    /// <summary>
    /// Reads the opcode of a packet, parses the request and runs its handler. A packet that cannot be read
    /// and a handler that fails never throw out of here: the error is counted for the session, and the
    /// session is closed once it has sent too many (<see cref="HandlerErrorBudget" />). A packet sent faster than its
    /// group allows (<see cref="PacketRateLimiter" />) is dropped before it is parsed.
    /// </summary>
    /// <remarks>The scope of the logs is the one of the session's <see cref="ServerSessionComponent"/>, looked up here;
    /// the read loop, which holds the session already, passes it instead.</remarks>
    public ValueTask ExecuteAsync(ReadOnlyMemory<byte> buffer, Entity player, CancellationToken cancellationToken)
    {
        var scope = world.IsAlive(player) && world.TryGet(player, out ServerSessionComponent session) ? session.LogScope : null;

        return ExecuteAsync(buffer, player, scope, cancellationToken);
    }

    /// <inheritdoc cref="ExecuteAsync(ReadOnlyMemory{byte}, Entity, CancellationToken)"/>
    /// <param name="scope">The scope every log line of the packet is written under, the handler's own lines among them;
    /// <see langword="null"/> for none.</param>
    /// <remarks>Must run on the world loop, inside the callback that handles the packet: the scope begins here, flows
    /// through the awaits of the handler, and ends with it. A scope begun on the read loop would not reach the world loop,
    /// whose queue does not carry the execution context of whoever posts to it.</remarks>
    public async ValueTask ExecuteAsync(ReadOnlyMemory<byte> buffer, Entity player, SessionLogScope? scope, CancellationToken cancellationToken)
    {
        if (IsClosed(player))
        {
            // The budget ran out and the connection is closing; what was read before that is dropped.
            return;
        }

        using var logScope = scope is null ? null : logger.BeginScope(scope);

        RequestReader? reader = null;

        MessageOpcode? opcode = null;

        try
        {
            var current = ReadOpcode(buffer.Span);

            opcode = current;

            if (!Admit(player, current))
            {
                return;
            }

            if (!collection.TryGet(current, out var found))
            {
                throw new InvalidDataException($"The opcode {current} has no place in the handler table.");
            }

            var instance = _instances[found.Instance] ??= Resolve(found.Class);

            var started = metrics?.Start() ?? 0;

            try
            {
                ValueTask pending;

                // A request with a span format is parsed from the packet here, whole, before its handler awaits anything;
                // the others read from a BinaryReader over the packet, which the handler holds until it returns.
                if (found.Packet is { } packet)
                {
                    pending = packet(instance, player, buffer.Span[OpcodeSize..], cancellationToken);
                }

                else
                {
                    reader = RentReader(buffer);
                    pending = found.Delegate(instance, player, reader.Binary, cancellationToken);
                }

                await pending;
            }

            finally
            {
                metrics?.Finish(found.Opcode, started);
            }
        }

        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The server is stopping; that is no fault of the packet.
            return;
        }

        catch (Exception ex)
        {
            OnError(player, opcode, ex);
        }

        finally
        {
            if (reader is not null)
            {
                ReturnReader(reader);
            }
        }
    }

    /// <summary>
    /// The group and the command, the first two bytes of every packet. A packet shorter than that ends here.
    /// </summary>
    private static MessageOpcode ReadOpcode(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < OpcodeSize)
        {
            throw new EndOfStreamException($"A packet of {packet.Length} bytes is shorter than its opcode.");
        }

        return new MessageOpcode(packet[0], packet[1]);
    }

    private IHotspotHandler Resolve(Type type)
    {
        return provider.GetService(type) as IHotspotHandler
            ?? throw new InvalidOperationException($"The handler {type.Name} is not registered.");
    }

    private RequestReader RentReader(ReadOnlyMemory<byte> packet)
    {
        var reader = Interlocked.Exchange(ref _spare, null);

        if (reader is null)
        {
            lock (_readersLock)
            {
                _readers.TryPop(out reader);
            }
        }

        reader ??= new RequestReader();

        reader.Stream.Source = packet;
        reader.Stream.Position = OpcodeSize;

        return reader;
    }

    private void ReturnReader(RequestReader reader)
    {
        // The packet goes back to the read loop with the end of the handler; the reader must not keep it.
        reader.Stream.Source = ReadOnlyMemory<byte>.Empty;

        if (Interlocked.CompareExchange(ref _spare, reader, null) is null)
        {
            return;
        }

        lock (_readersLock)
        {
            _readers.Push(reader);
        }
    }

    /// <summary>
    /// Takes a token of the group of the packet from the session (<see cref="PacketRateLimiter"/>). A packet without one is
    /// dropped and counted, and nothing else happens to the session.
    /// </summary>
    private bool Admit(Entity player, MessageOpcode opcode)
    {
        if (limiter is null || !world.IsAlive(player))
        {
            return true;
        }

        if (!world.Has<PacketRateComponent>(player))
        {
            world.Add<PacketRateComponent>(player);
        }

        var group = PacketRateLimiter.GroupOf(opcode.Group);

        if (limiter.TryTake(ref world.Get<PacketRateComponent>(player), group))
        {
            return true;
        }

        metrics?.Refused(PacketRateLimiter.Tag(group));

        return false;
    }

    private bool IsClosed(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out HandlerErrorsComponent errors) && errors.Closed;
    }

    /// <summary>
    /// Counts a packet that a handler refused as impossible against the budget of the session, as a packet that fails:
    /// logged, measured, and the connection closed once the budget runs out. Call it on the world loop.
    /// </summary>
    public void Charge(Entity player, MessageOpcode opcode, HandlerErrorReason reason)
    {
        OnError(player, opcode, reason, exception: null);
    }

    private void OnError(Entity player, MessageOpcode? opcode, Exception exception)
    {
        OnError(player, opcode, HandlerErrorBudget.Classify(exception), exception);
    }

    /// <summary>
    /// Counts the error for the session. The first one of a window is logged with its exception and the next
    /// ones only counted; the one that uses up the budget closes the connection and says so once.
    /// </summary>
    private void OnError(Entity player, MessageOpcode? opcode, HandlerErrorReason reason, Exception? exception)
    {
        var tag = HandlerErrorBudget.Tag(reason);

        metrics?.Error(tag);

        // A handler may have destroyed the entity before it failed; there is no session left to charge.
        if (!world.IsAlive(player))
        {
            LogErrorOfGoneSession(logger, exception, player.Id, new OpcodeText(opcode), tag);

            return;
        }

        if (!world.Has<HandlerErrorsComponent>(player))
        {
            world.Add<HandlerErrorsComponent>(player);
        }

        ref var errors = ref world.Get<HandlerErrorsComponent>(player);

        var verdict = budget.Record(ref errors);

        var limit = budget.Options.MaxErrors;
        var window = budget.Options.Window;

        world.TryGet(player, out ActorComponent actor);

        var hasSession = world.TryGet(player, out ServerSessionComponent session);

        if (verdict.Exceeded)
        {
            LogSessionClosed(logger, exception, player.Id, actor.Identifier, session.RemoteEndPoint,
                verdict.Count, window, new OpcodeText(opcode), tag);

            // An entity that never got a session has no connection to close.
            if (hasSession)
            {
                session.Disconnect(SessionCloseReason.Malformed);
            }
        }

        else if (verdict.IsFirst)
        {
            LogFirstError(logger, exception, player.Id, actor.Identifier, session.RemoteEndPoint,
                new OpcodeText(opcode), tag, verdict.Count, limit, window);
        }

        else
        {
            LogNextError(logger, exception, player.Id, new OpcodeText(opcode), tag, verdict.Count, limit);
        }
    }

    /// <summary>
    /// The name of the opcode, worked out only when a log line is written.
    /// </summary>
    private readonly record struct OpcodeText(MessageOpcode? Opcode)
    {
        public override string ToString()
        {
            return Opcode?.ToFullString() ?? "none";
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Session {Entity} (actor {Actor}, {Remote}) sent a packet {Opcode} that failed, reason {Reason}: error {Count} of {Limit} within {Window}.")]
    private static partial void LogFirstError(ILogger logger, Exception? exception, int entity, int actor, string? remote,
        OpcodeText opcode, string reason, int count, int limit, TimeSpan window);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
        Message = "Session {Entity} sent another packet {Opcode} that failed, reason {Reason}: error {Count} of {Limit}.")]
    private static partial void LogNextError(ILogger logger, Exception? exception, int entity, OpcodeText opcode,
        string reason, int count, int limit);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Closing session {Entity} (actor {Actor}, {Remote}) after {Count} failed packets within {Window}, the last was {Opcode}, reason {Reason}.")]
    private static partial void LogSessionClosed(ILogger logger, Exception? exception, int entity, int actor, string? remote,
        int count, TimeSpan window, OpcodeText opcode, string reason);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug,
        Message = "A packet {Opcode} failed after session {Entity} was gone, reason {Reason}.")]
    private static partial void LogErrorOfGoneSession(ILogger logger, Exception? exception, int entity, OpcodeText opcode, string reason);
}
