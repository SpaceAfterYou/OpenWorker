namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// The broaches set into the gear with this serial: three groups of five items, as the client reads them.
/// </summary>
public struct ST_ITEM_BROACH
{
    public const int ItemCount = 15;

    public SerialValue biSerial;
    public int[] dwItemID;
}
