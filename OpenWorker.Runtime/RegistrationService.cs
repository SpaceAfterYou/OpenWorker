using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenWorker.Hotspot;

/// <summary>
/// A background service that keeps a record of the server in Redis and takes it back when the server stops.
/// The loop of the service ends first, so that a late renewal cannot write the record again, and then
/// <see cref="RemoveAsync"/> removes it. That happens when the drain asks (<see cref="WithdrawAsync"/>), before the
/// sessions close, or at the latest when the host stops the service; whichever comes first does it, once.
/// </summary>
public abstract class RegistrationService : BackgroundService, IServerRegistration
{
    private TaskCompletionSource? _withdrawal;

    public Task WithdrawAsync(CancellationToken cancellationToken)
    {
        var mine = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (Interlocked.CompareExchange(ref _withdrawal, mine, null) is { } first)
        {
            return first.Task;
        }

        _ = WithdrawCoreAsync(mine, cancellationToken);

        return mine.Task;
    }

    /// <summary>
    /// The host stops the service the same way the drain does, so a server that stops without a drain still withdraws.
    /// </summary>
    public sealed override Task StopAsync(CancellationToken cancellationToken)
    {
        return WithdrawAsync(cancellationToken);
    }

    /// <summary>
    /// Removes the record. The loop of the service has ended by now. Failures go to the log: the record expires on its own.
    /// </summary>
    protected abstract Task RemoveAsync(CancellationToken cancellationToken);

    private async Task WithdrawCoreAsync(TaskCompletionSource completion, CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken);

            await RemoveAsync(cancellationToken);

            completion.SetResult();
        }

        catch (Exception e)
        {
            completion.SetException(e);
        }
    }
}

public static class RegistrationServiceExtensions
{
    /// <summary>
    /// Registers a service that is both a hosted service and a registration the drain withdraws.
    /// </summary>
    public static IServiceCollection AddRegistrationService<TService>(this IServiceCollection services) where TService : RegistrationService
    {
        services.AddSingleton<TService>();
        services.AddSingleton<IServerRegistration>(e => e.GetRequiredService<TService>());

        return services.AddHostedService(e => e.GetRequiredService<TService>());
    }
}
