namespace OpenWorker.Hotspot.Modules.SoulMetry.Types;

public readonly struct SoulMetryValue(int identifier, short completionBit)
{
    public int Identifier { get; } = identifier;
    public short CompletionBit { get; } = completionBit;
}