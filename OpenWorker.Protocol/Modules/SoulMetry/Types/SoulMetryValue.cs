using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Types;

/// <summary>
/// A soul metry in progress: its id and the bits of the objects already done.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct SoulMetryValue(int Identifier, short CompletionBit);
