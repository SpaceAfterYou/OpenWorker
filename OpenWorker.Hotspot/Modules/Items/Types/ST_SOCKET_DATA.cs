namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A socket of the gear with this serial and the five options it gives, as the client reads them.
/// </summary>
public struct ST_SOCKET_DATA
{
    public SerialValue biEquipSerial;
    public int dwSocketID;
    public byte bySocketPos;
    public List<ItemOption> stExtendOption; // must be 5
}
