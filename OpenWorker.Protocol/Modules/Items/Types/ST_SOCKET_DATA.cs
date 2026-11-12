using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A socket of the gear with this serial and the five options it gives, as the client reads them.
/// </summary>
[WireMessage(WireMethods.Write)]
public partial struct ST_SOCKET_DATA
{
    public SerialValue biEquipSerial { get; set; }
    public int dwSocketID { get; set; }
    public byte bySocketPos { get; set; }

    [WireArray(Count = ItemModuleDefines.OptionCount)]
    public List<ItemOption> stExtendOption { get; set; }
}
