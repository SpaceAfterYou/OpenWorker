using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Hotspot;

public sealed class ServerContent : List<bool>
{
    public bool this[ServerContentIndex index]
    {
        get => this[(int)index];
        set => this[(int)index] = value;
    }

    public ServerContent(IEnumerable<ServerContentPersistent> contents) : base(5)
    {
        foreach (var content in contents)
        {
            this[content.Index] = content.State;
        }
    }
}