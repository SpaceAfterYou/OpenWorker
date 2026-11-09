using System.Collections.Concurrent;

namespace OpenWorker.Lua;

/// <summary>
/// Which scripts have been told about which of their calls the server does not do: once is enough for the log, and a
/// maze that calls such a function every second must not fill it.
/// </summary>
internal static class ScriptWarnings
{
    private static readonly ConcurrentDictionary<(string Script, string Function), byte> Warned = new();

    /// <summary>
    /// True the first time a script names the function, false after.
    /// </summary>
    public static bool First(string script, string function)
    {
        return Warned.TryAdd((script, function), 0);
    }
}
