using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Boosters.Types;

public readonly struct BoosterLoadEntry(BinaryReader reader) : IWritableData
{
#region Message: Body

    public short Id { get; init; } = reader.ReadInt16();
    public TimeSpan Remaining { get; init; } = reader.ReadTimeInSeconds32();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Remaining);
    }

#endregion Interface: IWritableData
}
