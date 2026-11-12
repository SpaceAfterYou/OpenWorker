using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Renews the records of the rooms this server runs, on the world loop, so a room that runs long keeps its record and a room
/// whose record another instance took moves there.
/// </summary>
public sealed partial class MyRoomKeeper(MyRoomService rooms, WorldLoop loop, ILogger<MyRoomKeeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(MyRoomDirectory.RenewInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(rooms.KeepAllAsync);
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogKeepFailed(e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The records of the rooms could not be renewed")]
    private partial void LogKeepFailed(Exception exception);
}
