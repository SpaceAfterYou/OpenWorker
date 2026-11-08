using OpenWorker.LoadTest.Cli;

namespace OpenWorker.LoadTest;

/// <summary>
/// The only place of the tool that touches the console: the report is what the tool is for, and it goes to the
/// standard output. The progress goes to the standard error, so the report can be piped. Everything else writes to the
/// streams it is given. <c>eng/check-forbidden.sh</c> lets this file use the console for that reason.
/// </summary>
internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        using var stop = new CancellationTokenSource();

        var presses = 0;

        Console.CancelKeyPress += (_, e) =>
        {
            // The first press stops the run and lets it write its report; the second ends the process.
            if (Interlocked.Increment(ref presses) > 1)
            {
                return;
            }

            e.Cancel = true;

            stop.Cancel();
        };

        try
        {
            return await CommandDispatcher.RunAsync(args, Console.Out, Console.Error, Console.Error, stop.Token);
        }

        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            return 130;
        }
    }
}
