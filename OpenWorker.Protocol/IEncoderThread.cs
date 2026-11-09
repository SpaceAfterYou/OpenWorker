namespace OpenWorker.Hotspot;

/// <summary>
/// A synchronization context of one thread that encodes everything sent on it with one encoder: the world loop of the
/// runtime. <see cref="FrameEncoder.Current"/> takes that encoder on its thread, and a thread of its own anywhere else.
/// </summary>
internal interface IEncoderThread
{
    /// <summary>
    /// The calling thread is the thread of the context.
    /// </summary>
    bool IsCurrent { get; }

    FrameEncoder Encoder { get; }
}
