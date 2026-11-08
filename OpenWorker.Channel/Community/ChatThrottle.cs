using Arch.Core;

namespace OpenWorker.Channel.Community;

/// <summary>
/// A person may say only so much in a short time, so that a flood does not reach everybody else.
/// </summary>
public sealed class ChatThrottle : ICommunityListener
{
    private const int MaxMessages = 5;
    private const long WindowMilliseconds = 3000;

    private Dictionary<int, Queue<long>> Recent { get; } = [];

    public bool TryAcquire(int person, long now)
    {
        if (!Recent.TryGetValue(person, out var times))
        {
            Recent[person] = times = new Queue<long>();
        }

        while (times.Count > 0 && now - times.Peek() >= WindowMilliseconds)
        {
            times.Dequeue();
        }

        if (times.Count >= MaxMessages)
        {
            return false;
        }

        times.Enqueue(now);

        return true;
    }

    public bool TryAcquire(int person) => TryAcquire(person, Environment.TickCount64);

    public ValueTask OnEnteredAsync(Entity player, PresenceRecord person)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnLeftAsync(Entity player, PresenceRecord person)
    {
        Recent.Remove(person.Person);

        return ValueTask.CompletedTask;
    }
}
