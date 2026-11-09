using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Types;

/// <summary>
/// The endurance an item has now, by its serial.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial record struct ItemSetupEnduranceEntry(long Serial, byte Endurance);
