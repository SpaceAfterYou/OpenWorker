namespace OpenWorker.Lua;

/// <summary>
/// The countdown window a maze has shown its players, kept on the game time of the maze so that a player who comes in
/// later is shown it too, with the time that is left. Only the window is kept here: what happens when the time is up is
/// the script's own timer.
/// </summary>
public sealed class CasualRaidCountdown
{
    /// <summary>
    /// How long after a player has loaded the maze the countdown is shown again, so that the client has its window ready.
    /// </summary>
    public const float ShowAgainAfter = 5f;

    private int _value;
    private float _left;
    private float _showIn;

    /// <summary>
    /// The text the window shows; meaningful while <see cref="Left"/> is above zero.
    /// </summary>
    public int Value => _value;

    /// <summary>
    /// The seconds left of the countdown; zero or less when none runs.
    /// </summary>
    public float Left => _left;

    /// <summary>
    /// The maze has shown a countdown, or hidden it with a time below zero.
    /// </summary>
    public void Shown(int value, float time)
    {
        _value = value;
        _left = float.IsFinite(time) ? time : 0f;
    }

    /// <summary>
    /// A player has loaded the maze: a countdown that runs is shown to the players again a little later.
    /// </summary>
    public void Arrived()
    {
        if (_left > 0f)
        {
            _showIn = ShowAgainAfter;
        }
    }

    /// <summary>
    /// Counts the game time down. True when the countdown is to be shown again now, with <see cref="Value"/> and
    /// <see cref="Left"/>.
    /// </summary>
    public bool Step(float seconds)
    {
        if (_left > 0f)
        {
            _left -= seconds;

            // The time is up: nothing is left to show to anybody who comes in.
            if (_left <= 0f)
            {
                Reset();
            }
        }

        if (_showIn <= 0f)
        {
            return false;
        }

        _showIn -= seconds;

        if (_showIn > 0f)
        {
            return false;
        }

        _showIn = 0f;

        return _left > 0f;
    }

    private void Reset()
    {
        _value = 0;
        _left = 0f;
        _showIn = 0f;
    }
}
