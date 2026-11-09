namespace OpenWorker.Hotspot;

/// <summary>
/// Counts the messages an encoder drops because their body does not fit one frame (<c>frames.dropped</c> of the
/// runtime's session metrics).
/// </summary>
internal interface IFrameDrops
{
    void Dropped(string opcode);
}
