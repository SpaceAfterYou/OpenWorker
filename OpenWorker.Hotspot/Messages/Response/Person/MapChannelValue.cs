namespace OpenWorker.Hotspot.Messages.Response.Person;

public struct MapChannelValue
{
    private byte Channel { get; init; }
    
    public static implicit operator MapChannelValue(byte value) => new() { Channel = value };
    public static implicit operator MapChannelValue(short value) => new() { Channel = (byte)value };

    public static implicit operator ulong(MapChannelValue value) => value.Channel;
}