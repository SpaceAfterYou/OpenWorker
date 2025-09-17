namespace TestLang;

public readonly record struct ActorValue(int Identifier, byte Type);

public static class ActorValueExtensions
{
    public static void Write(this ActorValue value, BinaryWriter writer)
    {
        writer.Write(value.Identifier);
        writer.Write(value.Type);
    }
}

public readonly record struct Opcode(byte Group, byte Command);

public interface IMessage
{
    Opcode Opcode { get; }
}

public readonly record struct TestPacketResponse(ActorValue Actor) : IMessage
{
    public Opcode Opcode { get; } = new();
}

public readonly struct TestPacketRequest(BinaryReader reader)
{
    public ActorValue Actor { get; } = new(reader.ReadInt32(), reader.ReadByte());
}

public interface IMessageWriter
{
    void Write(BinaryWriter writer, in TestPacketResponse value);
}

public readonly record struct TestMessageWriter : IMessageWriter
{
    public void Write(BinaryWriter writer, in TestPacketResponse value)
    {
        value.Actor.Write(writer);
    }
}