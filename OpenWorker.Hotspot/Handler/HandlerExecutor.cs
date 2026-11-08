using System.Diagnostics;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

public sealed partial class HandlerExecutor(
    HandlerCollection collection,
    IServiceProvider provider,
    World world,
    HandlerErrorBudget budget,
    ILogger<HandlerExecutor> logger)
{
    /// <summary>
    /// Reads the opcode of a packet, parses the request and runs its handler. A packet that cannot be read
    /// and a handler that fails never throw out of here: the error is counted for the session, and the
    /// session is closed once it has sent too many (<see cref="HandlerErrorBudget" />).
    /// </summary>
    public async ValueTask ExecuteAsync(ReadOnlyMemory<byte> buffer, Entity player, CancellationToken cancellationToken)
    {
        if (IsClosed(player))
        {
            // The budget ran out and the connection is closing; what was read before that is dropped.
            return;
        }

        // Will be disposed in reader
        /* using */ var stream = buffer.AsStream();

        using var reader = new BinaryReader(stream);

        MessageOpcode? opcode = null;
        Handler? handler = null;

        try
        {
            // A packet shorter than the opcode ends the stream here.
            var current = new MessageOpcode(reader);

            opcode = current;

            if (!collection.TryGet(current, out var found))
            {
                throw new InvalidDataException($"The opcode {current} has no place in the handler table.");
            }

            handler = found;

            // TODO: refactor handler's bc too complicated
            var instance = provider.GetService(found.Class) as IHotspotHandler
                ?? throw new InvalidOperationException($"The handler {found.Class.Name} is not registered.");

            await found.Delegate
                .Invoke(instance, player, reader, cancellationToken);
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

        if (handler is { } resolved && opcode is { } read)
        {
            PrintSanity(resolved, read, reader, buffer);
        }
    }

    private bool IsClosed(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out HandlerErrorsComponent errors) && errors.Closed;
    }

    /// <summary>
    /// Counts the error for the session. The first one of a window is logged with its exception and the next
    /// ones only counted; the one that uses up the budget closes the connection and says so once.
    /// </summary>
    private void OnError(Entity player, MessageOpcode? opcode, Exception exception)
    {
        var reason = HandlerErrorBudget.Classify(exception);
        var tag = HandlerErrorBudget.Tag(reason);

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
                session.Disconnect();
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
    private static partial void LogFirstError(ILogger logger, Exception exception, int entity, int actor, string? remote,
        OpcodeText opcode, string reason, int count, int limit, TimeSpan window);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
        Message = "Session {Entity} sent another packet {Opcode} that failed, reason {Reason}: error {Count} of {Limit}.")]
    private static partial void LogNextError(ILogger logger, Exception exception, int entity, OpcodeText opcode,
        string reason, int count, int limit);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Closing session {Entity} (actor {Actor}, {Remote}) after {Count} failed packets within {Window}, the last was {Opcode}, reason {Reason}.")]
    private static partial void LogSessionClosed(ILogger logger, Exception exception, int entity, int actor, string? remote,
        int count, TimeSpan window, OpcodeText opcode, string reason);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug,
        Message = "A packet {Opcode} failed after session {Entity} was gone, reason {Reason}.")]
    private static partial void LogErrorOfGoneSession(ILogger logger, Exception exception, int entity, OpcodeText opcode, string reason);

    [Conditional("DEBUG")]
    private void PrintSanity(Handler handler, MessageOpcode opcode, BinaryReader reader, ReadOnlyMemory<byte> buffer)
    {
        if (opcode is { Group: (byte)GroupOpcode.System, Command: (byte)SystemOpcode.KeepAlive or (byte)SystemOpcode.Ping })
        {
            return;
        }

        if (opcode.Group == (byte)GroupOpcode.Move)
        {
            return;
        }

        // if (handler.Class.Name == nameof(EmptyHandler) || reader.BaseStream.Position != buffer.Length)
        // {
            var source = opcode.ToFullString();
            var @class = handler.Class.Name;
            var method = handler.Delegate.Method.Name;

            logger.LogDebug("[ ExecuteAsync ] [{Source}] [{Class}.{Method}]", source, @class, method);
            logger.LogDebug("Packet Read Length: {Position} / Total: {Length}", reader.BaseStream.Position, buffer.Length);
        // }
    }
}
