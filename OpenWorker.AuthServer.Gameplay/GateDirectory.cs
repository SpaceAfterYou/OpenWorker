using System.Collections.Immutable;
using System.Runtime.InteropServices;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.AuthServer.App;

/// <summary>
/// The gates the login shows. The sync of the gates makes a new list every round and puts it here whole; the world loop
/// reads whichever list was put last, and a list once put never changes, so a round of the sync cannot change it under a
/// login that is going through it.
/// </summary>
public sealed class GateDirectory
{
    private GateInfo[] _gates = [];

    /// <summary>
    /// The gates as the last round of the sync saw them.
    /// </summary>
    public ImmutableArray<GateInfo> Gates => ImmutableCollectionsMarshal.AsImmutableArray(Volatile.Read(ref _gates));

    /// <summary>
    /// Puts the new list in place of the old one.
    /// </summary>
    public void Publish(IEnumerable<GateInfo> gates)
    {
        Volatile.Write(ref _gates, gates.ToArray());
    }
}
