namespace OpenWorker.Hotspot.Handler;

/// <param name="Opcode">The name of the opcode, worked out once when the table is built: the tag of <c>handler.duration</c>.</param>
/// <param name="Instance">The place of the instance of <paramref name="Class"/> among the instances the executor keeps: one per
/// class, shared by every opcode the class handles.</param>
internal readonly record struct Handler(Type Class, HandlerDelegate Delegate, string Opcode, int Instance = 0)
{
    /// <summary>
    /// The tag of every opcode that has no handler. They share one, because the 68 requests nobody handles are not worth a series each.
    /// </summary>
    public const string Unhandled = "unhandled";
}

// https://youtu.be/ZJrEXthnycE