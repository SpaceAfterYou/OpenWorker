using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Persistent;

[PrimaryKey(nameof(Index))]
public sealed class ServerContentPersistent : BasicPersistent
{
    public required ServerContentIndex Index { get; init; }
    public required bool State { get; init; }
}