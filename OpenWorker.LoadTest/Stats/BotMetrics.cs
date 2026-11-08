namespace OpenWorker.LoadTest.Stats;

/// <summary>
/// What one bot measured. Each field has one writer: the read loop of the bot's connection or the bot's own
/// script, whichever the field is named for. The report reads them from another thread, a little behind.
/// </summary>
internal sealed class BotMetrics
{
    /// <summary>
    /// From sending a move to receiving the broadcast of it, back to the same bot.
    /// </summary>
    public LatencyHistogram MoveEcho { get; } = new();

    /// <summary>
    /// From sending a move to the broadcast of it reaching another bot. Recorded by the receiver.
    /// </summary>
    public LatencyHistogram MoveDelivery { get; } = new();

    /// <summary>
    /// From sending a ping right behind a move to its answer.
    /// </summary>
    public LatencyHistogram Ping { get; } = new();

    /// <summary>
    /// From saying a line to hearing it back.
    /// </summary>
    public LatencyHistogram ChatEcho { get; } = new();

    /// <summary>
    /// From saying a line to another bot hearing it. Recorded by the hearer.
    /// </summary>
    public LatencyHistogram ChatDelivery { get; } = new();

    /// <summary>
    /// From sending a move in a maze to the broadcast of it, which in a maze reaches the bot alone.
    /// </summary>
    public LatencyHistogram MazeMoveEcho { get; } = new();

    /// <summary>
    /// From using a skill in a maze to the answer to it.
    /// </summary>
    public LatencyHistogram SkillResponse { get; } = new();

    /// <summary>
    /// How late the bot sent a move compared to when it was due: the bot's own lag.
    /// </summary>
    public LatencyHistogram SendLag { get; } = new();

    // Written by the script of the bot.

    public long MovesSent;
    public long MovesSentWhileMeasured;
    public long PingsSent;
    public long ChatsSent;
    public long SkillsSent;

    // Written by the read loop.

    public long FramesReceived;
    public long Unmatched;
    public long SkillsAnswered;
}
