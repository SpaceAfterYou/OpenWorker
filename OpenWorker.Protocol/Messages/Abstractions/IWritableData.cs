namespace OpenWorker.Hotspot.Messages.Abstractions;

public interface IWritableData
{
    void Write(BinaryWriter writer);
}