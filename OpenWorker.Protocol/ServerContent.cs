using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot;

/// <summary>
/// Which contents of the game the server offers, one flag per <see cref="ServerContentIndex"/>; a content with no row is off.
/// A server builds it empty and fills it from the database when it starts, after the migration (<see cref="Load"/>).
/// </summary>
public sealed class ServerContent : List<bool>
{
    public bool this[ServerContentIndex index]
    {
        get => this[(int)index];
        set => this[(int)index] = value;
    }

    public ServerContent()
        : base(Enumerable.Repeat(false, Enum.GetValues<ServerContentIndex>().Length))
    {
    }

    public ServerContent(IEnumerable<ServerContentPersistent> contents)
        : this()
    {
        Load(contents);
    }

    /// <summary>
    /// Replaces every flag with the stored rows: a content with no row is off.
    /// </summary>
    public void Load(IEnumerable<ServerContentPersistent> contents)
    {
        for (var i = 0; i < Count; i++)
        {
            this[i] = false;
        }

        foreach (var content in contents)
        {
            this[content.Index] = content.State;
        }
    }

    /// <summary>
    /// The flags as the client reads them: one byte each, in the order of <see cref="ServerContentIndex"/>, no count.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        foreach (var flag in this)
        {
            writer.WriteBoolean(flag);
        }
    }
}
