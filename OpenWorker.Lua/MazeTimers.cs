namespace OpenWorker.Lua;

/// <summary>
/// What the timers of a maze need from it when one of them has something to say.
/// </summary>
public interface IMazeTimerHost
{
    /// <summary>
    /// Calls a function of the script: a step of a timer or a timer with parameters.
    /// </summary>
    ValueTask CallAsync(string function, int first, int second, int third);

    /// <summary>
    /// Calls the function the script has for every plain timer.
    /// </summary>
    ValueTask CompleteAsync(int timer);

    /// <summary>
    /// Shows the countdown to the player, or hides it with a negative value.
    /// </summary>
    void ShowTimer(int value, float time, float next);
}

/// <summary>
/// A timer a script has asked for. A plain one counts down and tells the script. One with steps counts down
/// a waiting time before each step and the time of the step itself, and the script can finish a step early.
/// </summary>
public sealed class MazeTimer
{
    public const int Plain = 0;
    public const int Stepped = 1;

    public int Id { get; init; }

    /// <summary>
    /// The function a script names the timer by. Timers started with a number have none.
    /// </summary>
    public string Name { get; init; } = "";

    public int Type { get; init; }
    public string Ready { get; init; } = "";
    public string Event { get; init; } = "";
    public float Origin { get; init; }
    public int Steps { get; init; }
    public int Wait { get; init; }
    public int First { get; init; }
    public int Second { get; init; }
    public int Third { get; init; }

    /// <summary>
    /// How long the timer counts for now.
    /// </summary>
    public float Time { get; set; }

    public float Elapsed { get; set; }
    public bool Enabled { get; set; }
    public int Step { get; set; } = 1;
    public bool IsFinished { get; set; }
    public bool IsWaiting { get; set; } = true;
    public bool IsPaused { get; set; }
    public bool IsReadyCalled { get; set; }
    public int NextLeft { get; set; }
    public int NextTotal { get; set; }

    public float Left => Math.Max(Time - Elapsed, 0f);
    public bool IsLastStep => Step == Steps;

    /// <summary>
    /// True once the time is up. A timer that is up does not count any more.
    /// </summary>
    public bool Tick(float seconds)
    {
        if (!Enabled)
        {
            return false;
        }

        Elapsed += seconds;

        if (Elapsed < Time)
        {
            return false;
        }

        Enabled = false;

        return true;
    }

    public void Restart(float time)
    {
        Enabled = true;
        Time = time;
        Elapsed = 0f;
    }
}

/// <summary>
/// The timers of one maze. A timer added while others are being run starts with the next round.
/// </summary>
public sealed class MazeTimers(IMazeTimerHost host)
{
    private readonly List<MazeTimer> _added = [];
    private readonly List<MazeTimer> _running = [];

    public bool IsEmpty => _added.Count == 0 && _running.Count == 0;

    public void Add(MazeTimer timer)
    {
        _added.Add(timer);
    }

    /// <summary>
    /// A timer that counts once, and tells the script which one it was.
    /// </summary>
    public void AddPlain(int id, float time)
    {
        Add(new MazeTimer { Id = id, Type = MazeTimer.Plain, Enabled = true, Time = time });
    }

    /// <summary>
    /// A timer that calls a function of the script with three numbers.
    /// </summary>
    public void AddPlain(string function, float time, int first, int second, int third)
    {
        Add(new MazeTimer { Name = function, Type = MazeTimer.Plain, Enabled = true, Time = time, First = first, Second = second, Third = third });
    }

    /// <param name="finish">Called when the time of a step is up.</param>
    /// <param name="step">Called when the script finishes a step early.</param>
    /// <param name="ready">Called when the waiting before a step begins.</param>
    /// <param name="time">How long a step takes.</param>
    /// <param name="steps">How many steps there are.</param>
    /// <param name="wait">How long it waits before each step.</param>
    public void AddStepped(string finish, string step, string ready, float time, int steps, int wait)
    {
        Add(new MazeTimer
        {
            Name = finish,
            Event = step,
            Ready = ready,
            Type = MazeTimer.Stepped,
            Enabled = true,
            Time = wait,
            Origin = time,
            Steps = steps,
            Wait = wait,
            NextTotal = (int)time
        });
    }

    public void Clear()
    {
        _added.Clear();
        _running.Clear();
    }

    /// <summary>
    /// Makes a timer run out at once without a word to the script, and says how long it had left.
    /// </summary>
    public float Cancel(string function)
    {
        var left = 0f;

        foreach (var timer in _running.Where(e => e.Name == function).Take(1))
        {
            left = timer.Left;

            timer.Restart(0f);
        }

        return left;
    }

    public void Cancel(int id)
    {
        foreach (var timer in _added.Concat(_running).Where(e => e.Name.Length == 0 && e.Id == id))
        {
            timer.Restart(0f);
        }
    }

    /// <summary>
    /// A stepped timer waits for the script to start it again after a step.
    /// </summary>
    public void Start(string function)
    {
        foreach (var timer in _running.Where(e => e.Name == function).Take(1))
        {
            timer.IsPaused = false;
        }
    }

    public void Complete(string function, int step)
    {
        foreach (var timer in _running.Where(e => e.Name == function).Take(1))
        {
            if (step == 0 || step == timer.Step)
            {
                timer.IsFinished = true;
            }
        }
    }

    /// <summary>
    /// Gives a stepped timer more time, and shows the new time.
    /// </summary>
    public void Extend(string function, float seconds)
    {
        foreach (var timer in _running.Where(e => e.Name == function).Take(1))
        {
            timer.Time += seconds;

            host.ShowTimer(0, (int)timer.Left, seconds);
        }
    }

    public async ValueTask UpdateAsync(float seconds)
    {
        _running.AddRange(_added);
        _added.Clear();

        foreach (var timer in _running.ToArray())
        {
            // The script may have cleared the timers in the meantime.
            if (!_running.Contains(timer) || timer.IsPaused)
            {
                continue;
            }

            var done = timer.Type == MazeTimer.Plain
                ? await UpdatePlainAsync(timer, seconds)
                : await UpdateSteppedAsync(timer, seconds);

            if (done)
            {
                _running.Remove(timer);
            }
        }
    }

    private async ValueTask<bool> UpdatePlainAsync(MazeTimer timer, float seconds)
    {
        var time = timer.Time;

        if (!timer.Tick(seconds))
        {
            return false;
        }

        // A timer that was cancelled has no time left, and nothing to tell.
        if (time > 0f)
        {
            if (timer.Name.Length > 0)
            {
                await host.CallAsync(timer.Name, timer.First, timer.Second, timer.Third);
            }
            else
            {
                await host.CompleteAsync(timer.Id);
            }
        }

        return true;
    }

    private async ValueTask<bool> UpdateSteppedAsync(MazeTimer timer, float seconds)
    {
        if (timer.IsWaiting && !timer.IsReadyCalled)
        {
            timer.IsReadyCalled = true;

            await host.CallAsync(timer.Ready, timer.Step, 0, 0);
        }

        var time = timer.Time;

        if (timer.Tick(seconds))
        {
            if (timer.IsWaiting)
            {
                // The waiting is over, the step itself begins, with the time the last one left over.
                timer.Restart(timer.NextTotal);
                timer.IsWaiting = false;

                host.ShowTimer(0, timer.NextTotal, timer.NextLeft);

                return false;
            }

            timer.IsPaused = true;

            if (time > 0f)
            {
                await host.CallAsync(timer.Name, timer.Step, 0, 0);
            }

            host.ShowTimer(-1, -1f, 0f);

            if (timer.IsLastStep)
            {
                return true;
            }

            Next(timer, 0);

            return false;
        }

        if (!timer.IsFinished || timer.IsWaiting)
        {
            return false;
        }

        // The script has finished the step: whatever time was left is added to the next one.
        timer.IsPaused = true;

        await host.CallAsync(timer.Event, timer.Step, 0, 0);

        host.ShowTimer(-1, -1f, 0f);

        if (timer.IsLastStep)
        {
            return true;
        }

        Next(timer, (int)timer.Left);

        return false;
    }

    private static void Next(MazeTimer timer, int left)
    {
        timer.Step++;
        timer.IsWaiting = true;
        timer.IsReadyCalled = false;
        timer.IsFinished = false;
        timer.NextLeft = left;
        timer.NextTotal = left + (int)timer.Origin;
        timer.Restart(timer.Wait);
    }
}
