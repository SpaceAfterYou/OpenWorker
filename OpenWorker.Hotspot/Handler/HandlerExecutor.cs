using System.Diagnostics;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

public sealed class HandlerExecutor(
    HandlerCollection collection,
    IServiceProvider provider,
    ILogger<HandlerExecutor> logger)
{
    public async ValueTask ExecuteAsync(ReadOnlyMemory<byte> buffer, Entity entity, CancellationToken cancellationToken)
    {
        // Will be disposed in reader
        /* using */ var stream = buffer.AsStream();

        using var reader = new BinaryReader(stream);

        var opcode = new MessageOpcode(reader);

        var handler = collection[opcode];

        // TODO: refactor handler's bc too complicated
        var instance = provider.GetService(handler.Class) as IHotspotHandler;

        Debug.Assert(instance is not null);

        try
        {
            await handler.Delegate
                .Invoke(instance, entity, reader, cancellationToken)
                .ConfigureAwait(false);
        }

        catch (Exception ex)
        {
            logger.LogCritical(ex, "");
        }

        PrintSanity(handler, opcode, reader, buffer);
    }

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