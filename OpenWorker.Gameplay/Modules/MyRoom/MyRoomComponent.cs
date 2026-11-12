using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// The room of the account of the person, read when the person enters the server; empty while the account has none.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct MyRoomComponent
{
    public MyRoomRecord? Room { get; init; }
}
