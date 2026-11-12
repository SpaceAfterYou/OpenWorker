using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Vaccum.Types;

/// <summary>
/// A cube that can be taken, as the client lists it: 9 bytes.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct VaccumCubeValue(int Cube, int Value, byte State) : IWritableData;
