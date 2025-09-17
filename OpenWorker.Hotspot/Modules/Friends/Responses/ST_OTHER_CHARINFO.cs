using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

public struct ST_OTHER_CHARINFO
{
    public short shLevel;
    public byte byState;
    public string szComment; // max 51;
    public List<ST_STAT> m_vecBaseStat;
    public List<ST_STAT> m_vecStat;
    public StorageValue m_vecEquipItem;
    public byte byEchelonLevel;
    public int nEchelonExp;
    public string szMemo; // max 31;
    public int nEquipMemorySlot;
    public byte byClass;
    public string szName; // max 21;
    public TitleValue stInsideTitle;
    public TitleValue stOutsideTitle;
    public List<ST_SOCKET_DATA> vecSocketList;
    public List<ST_ITEM_BROACH> vecBroachList;
}