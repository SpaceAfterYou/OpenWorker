using System.Net.Sockets;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// A bot could not do what its script asked: the server refused, did not answer, or closed the connection. It
/// carries the step it happened in and a short kind that tells the causes apart in the report.
/// </summary>
internal sealed class BotFailure : Exception
{
    public BotFailure(BotStage stage, string kind, string message, Exception? inner = null)
        : base(message, inner)
    {
        Stage = stage;
        Kind = kind;
    }

    public BotStage Stage { get; }

    /// <summary>
    /// A short name for the cause, e.g. <c>timeout</c> or <c>kicked-UserWrongWorld</c>.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Whether the step that failed has counted it already, so the bot does not count it twice.
    /// </summary>
    public bool Counted { get; set; }

    /// <summary>
    /// A failure to open a connection, named for what the system said.
    /// </summary>
    public static BotFailure Connecting(BotStage stage, string host, int port, Exception error)
    {
        var kind = error switch
        {
            TimeoutException => "connect-timeout",
            SocketException { SocketErrorCode: SocketError.ConnectionRefused } => "connect-refused",
            SocketException socket => "connect-" + socket.SocketErrorCode,
            _ => "connect-failed"
        };

        return new BotFailure(stage, kind, $"Could not connect to {host}:{port}: {error.Message}", error);
    }
}
