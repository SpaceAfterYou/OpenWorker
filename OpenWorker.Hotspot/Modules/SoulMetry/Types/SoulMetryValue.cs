using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Types;

public readonly struct SoulMetryValue(int identifier, short completionBit) : IWritableData
{
#region Message: Body

    public int Identifier { get; } = identifier;
    public short CompletionBit { get; } = completionBit;

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Identifier);
        writer.Write(CompletionBit);
    }

#endregion Interface: IWritableData
}
