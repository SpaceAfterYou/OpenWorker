using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Attendance;

namespace OpenWorker.Commands;

/// <summary>
/// Counts seconds as played today at once, as <c>/playtime 1800</c>, so a step of the play time reward comes to its time
/// without waiting.
/// </summary>
[CommandTrigger("playtime")]
[CommandDescription("Count seconds as played today for the play time reward")]
internal sealed partial class PlayTimeCommand : ICommand
{
    private const int MaxSeconds = 86_400;

    [ExternalDependency]
    private AttendanceManager Attendance { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            seconds is <= 0 or > MaxSeconds)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(Attendance.AddPlayTime(player, seconds));
    }
}
