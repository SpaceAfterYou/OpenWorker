using System.Threading.Channels;

namespace OpenWorker.Channel;

/// <summary>
/// Says that the number of players of some channel has changed. The loop notifies at every join and leave, without
/// waiting and without allocating; the service that publishes the counts waits for it. Notifications that come while
/// one is pending are one.
/// </summary>
internal sealed class ChannelChanges
{
    private readonly Channel<bool> _signal = System.Threading.Channels.Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public void Notify()
    {
        _signal.Writer.TryWrite(true);
    }

    /// <summary>
    /// Waits for a change and takes it.
    /// </summary>
    public async ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        await _signal.Reader.ReadAsync(cancellationToken);
    }
}
