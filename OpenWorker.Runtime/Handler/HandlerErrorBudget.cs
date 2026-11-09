using Microsoft.Extensions.Options;
using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// Why a packet failed. <see cref="HandlerErrorBudget.Tag" /> is the value of the <c>reason</c> tag of the
/// <c>handler.errors</c> metric.
/// </summary>
public enum HandlerErrorReason
{
    /// <summary>
    /// The packet is not what the client should send: it ends early or carries a length that cannot be right.
    /// </summary>
    Malformed,

    /// <summary>
    /// The handler failed on a packet that parsed.
    /// </summary>
    HandlerFault,

    /// <summary>
    /// The packet parsed and the handler refused what it asks for as impossible: a move faster than the player can run.
    /// </summary>
    Violation
}

/// <summary>
/// What one more error means for its session.
/// </summary>
/// <param name="Count">Errors in the current window, this one included.</param>
/// <param name="IsFirst">This error opened the window.</param>
/// <param name="Exceeded">The window holds as many errors as the budget allows; the session has to close.</param>
public readonly record struct HandlerErrorVerdict(int Count, bool IsFirst, bool Exceeded);

/// <summary>
/// Counts the packets of a session that failed to parse or to handle, in a window that opens with the first
/// error and lasts <see cref="HandlerErrorBudgetOptions.Window" />. A window is fixed, not sliding: a client
/// can fit nearly twice the limit around the edge of two windows, which is still a bounded trickle.
/// </summary>
public sealed class HandlerErrorBudget
{
    private readonly TimeProvider _time;
    private readonly HandlerErrorBudgetOptions _options;

    public HandlerErrorBudget(TimeProvider time, IOptions<HandlerErrorBudgetOptions> options)
    {
        var value = options.Value;

        ArgumentOutOfRangeException.ThrowIfLessThan(value.MaxErrors, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value.Window, TimeSpan.Zero);

        _time = time;
        _options = value;
    }

    public HandlerErrorBudgetOptions Options => _options;

    /// <summary>
    /// A packet that cannot be read is the client's fault; any other exception is the handler's.
    /// Both count, only the reason in the log differs.
    /// </summary>
    public static HandlerErrorReason Classify(Exception exception)
    {
        // EndOfStreamException is an IOException too.
        return exception is IOException or ArgumentOutOfRangeException or InvalidDataException
            ? HandlerErrorReason.Malformed
            : HandlerErrorReason.HandlerFault;
    }

    public static string Tag(HandlerErrorReason reason)
    {
        return reason switch
        {
            HandlerErrorReason.Malformed => "malformed",
            HandlerErrorReason.Violation => "violation",
            _ => "handler_fault"
        };
    }

    /// <summary>
    /// Counts an error of the session. When the verdict says <see cref="HandlerErrorVerdict.Exceeded" />
    /// the state is marked closed too.
    /// </summary>
    public HandlerErrorVerdict Record(ref HandlerErrorsComponent state)
    {
        var now = _time.GetTimestamp();

        var first = state.Count == 0 || _time.GetElapsedTime(state.WindowStart, now) >= _options.Window;

        if (first)
        {
            state.WindowStart = now;
            state.Count = 0;
        }

        state.Count++;

        var exceeded = state.Count >= _options.MaxErrors;

        if (exceeded)
        {
            state.Closed = true;
        }

        return new HandlerErrorVerdict(state.Count, first, exceeded);
    }
}
