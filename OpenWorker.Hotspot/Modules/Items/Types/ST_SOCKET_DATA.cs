namespace OpenWorker.Hotspot.Modules.Items.Types;

public struct ST_SOCKET_DATA
{
    public int dwSocketID;
    public byte bySocketPos;
    public List<ItemOption> stExtendOption; // must be 5
}