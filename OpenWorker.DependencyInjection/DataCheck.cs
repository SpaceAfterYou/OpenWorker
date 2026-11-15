using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Extensions.Data;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Checks, once when the server starts, that every folder of game data it needs holds files of its kind. A missing or
/// empty folder is an error line that names it and what will not work; a folder the server cannot play without stops
/// the start.
/// </summary>
public static partial class DataCheck
{
    /// <summary>
    /// Checks the needs of a server against the folders under <paramref name="root"/>: logs every missing folder and throws
    /// when one of them is a folder the server does not start without.
    /// </summary>
    /// <exception cref="InvalidOperationException">A folder the server does not start without is missing.</exception>
    public static void Run(IEnumerable<DataNeed> needs, IConfiguration configuration, ILogger logger, string root = "")
    {
        var refused = new List<string>();

        foreach (var need in needs)
        {
            if (need.When is { } when && !when(configuration))
            {
                continue;
            }

            if (need.Folder.IsFilledIn(root))
            {
                continue;
            }

            var path = Path.GetFullPath(Path.Join(root, need.Folder.Path));

            if (need.Refuse)
            {
                LogRefused(logger, path, need.Folder.Holds, need.Without);
                refused.Add(path);
            }

            else
            {
                LogMissing(logger, path, need.Folder.Holds, need.Without);
            }
        }

        if (refused.Count > 0)
        {
            throw new InvalidOperationException($"The server does not start without its game data: {string.Join(", ", refused)}.");
        }
    }

    /// <summary>
    /// Checks the needs of the server when it starts, before the services after this call.
    /// </summary>
    public static IServiceCollection AddDataCheck(this IServiceCollection services, IReadOnlyList<DataNeed> needs)
    {
        return services.AddHostedService(provider => new DataCheckService(
            needs,
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataCheck))));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The folder {Folder} holds no {Holds}: {Without}. The server does not start")]
    private static partial void LogRefused(ILogger logger, string folder, string holds, string without);

    [LoggerMessage(Level = LogLevel.Error, Message = "The folder {Folder} holds no {Holds}: {Without}")]
    private static partial void LogMissing(ILogger logger, string folder, string holds, string without);

    private sealed class DataCheckService(IReadOnlyList<DataNeed> needs, IConfiguration configuration, ILogger logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Run(needs, configuration, logger);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
