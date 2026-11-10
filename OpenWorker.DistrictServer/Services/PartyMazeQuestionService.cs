using OpenWorker.Hotspot;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// Ends the questions a party is asked before it enters a maze when nobody answers them.
/// </summary>
public sealed partial class PartyMazeQuestionService(WorldLoop loop, PartyMazeEntry entry, ILogger<PartyMazeQuestionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await loop.InvokeAsync(entry.ExpireQuestions);
            }

            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogExpiryFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Ending the unanswered questions of the parties failed")]
    private static partial void LogExpiryFailed(ILogger logger, Exception exception);
}
