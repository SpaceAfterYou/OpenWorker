using Arch.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Server.Common;

/// <summary>
/// Asks a player that has gone quiet to answer, and disconnects the one that never does, in a district and in a maze.<br/>
/// Every <see cref="Frequency"/> a player that has not answered during the last period is sent a keep-alive
/// and counts one more miss; an answer (see <c>SystemService</c>) forgives all misses.
/// A player that missed <see cref="MaxMissedAnswers"/> keep-alives in a row is disconnected at the next check.
/// The client sends its keep-alive only in answer to the server's.
/// </summary>
public sealed partial class KeepAliveService(
    World world,
    WorldLoop loop,
    TimeProvider time,
    ILogger<KeepAliveService> logger
) :
    BackgroundService
{
    private static readonly QueryDescription Players = new QueryDescription().WithAll<KeepAliveComponent, ServerSessionComponent>();

    private readonly List<Entity> _players = [];

    public static TimeSpan Frequency { get; } = TimeSpan.FromMinutes(1);

    public const byte MaxMissedAnswers = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Frequency, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await loop.InvokeAsync(CheckPlayers);
                }

                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    LogCheckFailed(logger, e);
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    /// <summary>
    /// The check of every player that has entered the game. Runs on the world loop.
    /// </summary>
    public void CheckPlayers()
    {
        _players.Clear();

        world.Query(in Players, (Entity entity, ref KeepAliveComponent component) =>
        {
            if (component.Started)
            {
                _players.Add(entity);
            }
        });

        foreach (var player in _players)
        {
            CheckPlayer(player);
        }

        _players.Clear();
    }

    /// <summary>
    /// One step of the check for one player. Runs on the world loop.
    /// </summary>
    public void CheckPlayer(Entity player)
    {
        var component = world.Get<KeepAliveComponent>(player);

        if (time.GetElapsedTime(component.AnsweredAt) < Frequency)
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(player);

        if (component.PenaltyCount >= MaxMissedAnswers)
        {
            LogDisconnected(logger, player.Id, component.PenaltyCount);

            session.Disconnect(SessionCloseReason.Timeout);
            return;
        }

        session.Send(new SystemKeepAliveResponse { Time = new TimeSpan(component.ClientTickCount) });

        world.Set(player, component with { PenaltyCount = (byte)(component.PenaltyCount + 1) });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Keep-alive check failed.")]
    private static partial void LogCheckFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Player {Player} did not answer {Missed} keep-alives and is disconnected.")]
    private static partial void LogDisconnected(ILogger logger, int player, byte missed);
}
