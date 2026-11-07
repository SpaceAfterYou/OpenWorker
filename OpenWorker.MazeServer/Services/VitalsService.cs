using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player who has fallen in a maze asks to get up.
/// </summary>
public sealed class VitalsService(World world) : IHotspotHandler<PersonReviveReqRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonReviveReqRequest request)
    {
        if (world.Has<LuaMaze>(context.Player))
        {
            // The player is the one of the connection, whoever the request names.
            world.Get<LuaMaze>(context.Player).Creatures.Revive(context.Player, request.Type);
        }

        return ValueTask.CompletedTask;
    }
}
