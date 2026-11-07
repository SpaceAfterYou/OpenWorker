using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Vaccum.Types;

/// <summary>
/// A cube that can be taken, as the client lists it: 9 bytes.
/// </summary>
public readonly record struct VaccumCubeValue(int Cube, int Value, byte State) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Cube);
        writer.Write(Value);
        writer.Write(State);
    }
}
