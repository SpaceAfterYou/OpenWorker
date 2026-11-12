using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// The broaches set into the gear with this serial: three groups of five items, as the client reads them.
/// </summary>
[WireMessage(WireMethods.Write)]
public partial struct ST_ITEM_BROACH
{
    public const int ItemCount = 15;

    public SerialValue biSerial { get; set; }

    [WireArray(Count = ItemCount)]
    public int[] dwItemID { get; set; }
}
