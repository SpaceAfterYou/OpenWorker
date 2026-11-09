namespace OpenWorker.LoadTest.Reporting;

/// <summary>
/// The names under which the report lists the latencies, and what each of them measures. The same names are the keys
/// of the JSON file.
/// </summary>
internal static class LatencyNames
{
    public const string MoveEcho = "move.echo";
    public const string MoveDelivery = "move.delivery";
    public const string Ping = "ping.after-move";
    public const string ChatEcho = "chat.echo";
    public const string ChatDelivery = "chat.delivery";
    public const string MazeMoveEcho = "maze.move.echo";
    public const string SkillResponse = "maze.skill.response";
    public const string SendLag = "bot.send-lag";

    public static IReadOnlyList<(string Name, string Meaning)> All { get; } =
    [
        (MoveEcho, "A move sent, to the broadcast of that move coming back to the same bot (a district)."),
        (MoveDelivery, "A move sent by one bot, to the broadcast of it reaching another bot of the same channel."),
        (Ping, "A ping sent in the same write behind a move, to its answer. The server handles the packets of a connection in order, so this bounds the time the server takes to handle the move."),
        (ChatEcho, "A line said, to the same bot hearing it."),
        (ChatDelivery, "A line said by one bot, to another bot hearing it."),
        (MazeMoveEcho, "A move sent in a maze, to its broadcast, which in a maze reaches the bot alone."),
        (SkillResponse, "A skill used in a maze, to the answer of the server."),
        (SendLag, "How late the bot sent a move compared to when it was due. This is the lag of the bot, not of the server.")
    ];
}
