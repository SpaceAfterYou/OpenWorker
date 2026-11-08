namespace OpenWorker.Wire;

/// <summary>
/// A value that reads itself from a packet. The generator implements it for a <see cref="WireMessageAttribute"/> struct;
/// a value whose format is not declarative implements it by hand.
/// </summary>
public interface IWireReadable<TSelf> where TSelf : IWireReadable<TSelf>
{
    /// <summary>
    /// Reads the value at the position of <paramref name="reader"/> and moves past it.
    /// </summary>
    static abstract TSelf Read(ref PacketReader reader);
}

/// <summary>
/// A value that writes itself in the layout the client reads. The generator implements it for a
/// <see cref="WireMessageAttribute"/> struct; a value whose format is not declarative implements it by hand.
/// </summary>
public interface IWireWritable
{
    void Write(ref PacketWriter writer);
}
