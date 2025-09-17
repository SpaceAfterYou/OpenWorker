namespace OpenWorker.Hotspot.Messages.Abstractions;

public interface IResponseHotspotMessage : IHotspotMessage
{
    void ToBinary(BinaryWriter writer);
}