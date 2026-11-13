using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Stops the server in an order that does not depend on the order the services were registered in.<br/>
/// The host stops hosted services in the reverse of their registration, which would close the listener of the game before
/// or after the services that keep the records in Redis by accident. A hosted service that is a lifecycle service
/// is asked to stop <em>before any</em> service is stopped, and that is where this one drains:
/// <list type="number">
/// <item>the server is draining (readiness fails), and <see cref="ServerService"/> stops accepting: the listener closes;</item>
/// <item>the records of the server in Redis are taken back (<see cref="IServerRegistration"/>), so the other servers send
/// no player here any more; it runs next to the step below and is awaited after it;</item>
/// <item>every session closes through the normal path, with a message to the client that the server goes away: the
/// close handlers save the players on the world loop, the writers send what is queued and the connections close;</item>
/// </list>
/// Then the host stops the other services, the health endpoint among them, and last the container disposes the
/// world loop. The loop runs through the whole drain: the close handlers need it.<br/>
/// The drain as a whole is bound by <see cref="ShutdownOptions.DrainTimeout"/>; what has not finished by then
/// is left, logged, and the server goes on stopping.
/// </summary>
internal sealed partial class ServerShutdownService(
    ServerState state,
    ServerService server,
    IEnumerable<IServerRegistration> registrations,
    IOptions<ShutdownOptions> options,
    ILogger<ServerShutdownService> logger
) :
    IHostedLifecycleService
{
    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        // The signal to stop has set it already; a host that is stopped by code has not.
        state.BeginDrain();

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        budget.CancelAfter(options.Value.DrainTimeout);

        var started = Stopwatch.GetTimestamp();

        // 1. Nobody new comes in.
        try
        {
            await server.StopAcceptingAsync();
        }

        catch (Exception e)
        {
            // The sessions that are there still have to be closed.
            LogDrainFailed(logger, e);
        }

        LogDraining(logger, options.Value.DrainTimeout);

        try
        {
            // 2. The others stop sending players here. It goes on while the sessions close.
            var withdrawing = WithdrawAsync(budget.Token);

            // 3. The sessions close: the message to the client, the close handlers, the writers.
            var left = await server.CloseSessionsAsync(budget.Token);

            await withdrawing.WaitAsync(budget.Token);

            var elapsed = Stopwatch.GetElapsedTime(started);

            if (left > 0)
            {
                LogDrainTimedOut(logger, left, options.Value.DrainTimeout);
            }

            else
            {
                LogDrained(logger, elapsed);
            }
        }

        catch (OperationCanceledException) when (budget.IsCancellationRequested)
        {
            LogDrainTimedOut(logger, server.OpenSessions, options.Value.DrainTimeout);
        }

        catch (Exception e)
        {
            // The server stops either way: what is left of the drain is not worth a failed stop.
            LogDrainFailed(logger, e);
        }
    }

    private async Task WithdrawAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(registrations.Select(registration => WithdrawOneAsync(registration, cancellationToken)));
    }

    private async Task WithdrawOneAsync(IServerRegistration registration, CancellationToken cancellationToken)
    {
        try
        {
            await registration.WithdrawAsync(cancellationToken);
        }

        catch (Exception e)
        {
            // The record expires by itself, or the next start overwrites it.
            LogWithdrawFailed(logger, e, registration.GetType().Name);
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "The server stops: no new connections, draining the sessions for up to {Timeout}.")]
    private static partial void LogDraining(ILogger logger, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Information, Message = "The sessions are closed after {Elapsed}.")]
    private static partial void LogDrained(ILogger logger, TimeSpan elapsed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Left} sessions did not close within {Timeout} of the drain and are left.")]
    private static partial void LogDrainTimedOut(ILogger logger, int left, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Error, Message = "The drain failed.")]
    private static partial void LogDrainFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Registration} could not take its record back.")]
    private static partial void LogWithdrawFailed(ILogger logger, Exception exception, string registration);
}
