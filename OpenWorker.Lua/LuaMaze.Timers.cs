using Lua;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// A timer of the script that fails must not stop the others, so errors end here.
    /// </summary>
    private sealed class TimerHost(LuaMaze maze) : IMazeTimerHost
    {
        public async ValueTask CallAsync(string name, int first, int second, int third)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerExComplete(name, first, second, third, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFunctionFailed(e, name);
            }
        }

        public async ValueTask CompleteAsync(int timer)
        {
            if (maze.Closed)
            {
                return;
            }

            try
            {
                await maze.State.OnTimerComplete(timer, maze);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                maze.Logger.TimerFailed(e, timer);
            }
        }

        public void ShowTimer(int value, float time, float following)
        {
            maze.ShowTimer(value, time, following);
        }
    }

    private MazeTimers? _timers;

    /// <summary>
    /// A round of the timers waits for a script that has not finished; the steps until then are added to the next round.
    /// </summary>
    private bool _timersBusy;
    private float _timersOwed;

    private MazeTimers Timers => _timers ??= new MazeTimers(new TimerHost(this));

    private void StepTimers(float seconds)
    {
        if (_timers is null || _timers.IsEmpty)
        {
            return;
        }

        if (_timersBusy)
        {
            _timersOwed += seconds;

            return;
        }

        seconds += _timersOwed;
        _timersOwed = 0f;

        var round = _timers.UpdateAsync(seconds);

        if (!round.IsCompletedSuccessfully)
        {
            _timersBusy = true;

            _ = FinishTimersAsync(round);
        }
    }

    private async Task FinishTimersAsync(ValueTask round)
    {
        try
        {
            await round;
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            Logger.TimersFailed(e);
        }

        finally
        {
            _timersBusy = false;
        }
    }

    /// <summary>
    /// Gives a timer with steps more time.
    /// </summary>
    [LuaMember]
    public void AddEventMaxTime(string function, float time)
    {
        Timers.Extend(function, time);
    }

    /// <param name="function">Called when the time of a step is up. It also names the timer.</param>
    /// <param name="step">Called when the script finishes a step early.</param>
    /// <param name="ready">Called when the waiting before a step begins.</param>
    /// <param name="type">Only timers with steps are known.</param>
    /// <param name="time">How long a step takes.</param>
    /// <param name="steps">How many steps there are.</param>
    /// <param name="wait">How long it waits before each step.</param>
    [LuaMember]
    public void AddEventTimer(string function, string step, string ready, int type, float time, int steps, int wait)
    {
        if (type != MazeTimer.Stepped)
        {
            Logger.UnknownEventTimerType(type, function);
            return;
        }

        Timers.AddStepped(function, step, ready, time, steps, wait);
    }

    [LuaMember]
    public void AddTimer(int identifier, float time)
    {
        Logger.CallAddTimer(identifier, time);

        Timers.AddPlain(identifier, time);
    }

    [LuaMember]
    public void AddTimerEx(string function, float time, int param1, int param2, int param3)
    {
        Logger.CallAddTimerEx(function, time, param1, param2, param3);

        Timers.AddPlain(function, time, param1, param2, param3);
    }

    [LuaMember]
    public void ClearAllTimers()
    {
        Timers.Clear();
    }

    /// <summary>
    /// The script says a step of a timer is done before its time is up.
    /// </summary>
    [LuaMember]
    public void CompleteTimerStep(string function, int step)
    {
        Timers.Complete(function, step);
    }

    /// <summary>
    /// Stops a timer without telling the script, and says how long it had left.
    /// </summary>
    [LuaMember]
    public float RemoveTimerEx(string function)
    {
        return Timers.Cancel(function);
    }

    /// <summary>
    /// A countdown on the screen: nothing counts without the client's own clock, so the number is the time to show.
    /// A negative value hides it.
    /// </summary>
    [LuaMember]
    public void ShowCasualRaidTimer(int value, float time, float next = 0f)
    {
        ShowTimer(value, time, next);
    }

    [LuaMember]
    public void StartEventTimer(string function)
    {
        Timers.Start(function);
    }

    private void ShowTimer(int value, float time, float next)
    {
        creatureManager.Broadcast(new MazeTimerResponse { Value = value, Time = time, Next = next });
    }
}
