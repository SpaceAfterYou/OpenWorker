using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A stat and its value, as the client reads the stat lists: the value first.
/// </summary>
[WireMessage(WireMethods.Write)]
public partial struct ST_STAT
{
    public float fValue { get; set; }
    public short wStatID { get; set; }
}
