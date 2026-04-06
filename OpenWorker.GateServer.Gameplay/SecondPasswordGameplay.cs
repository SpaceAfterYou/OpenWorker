using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;

namespace OpenWorker.GateServer.Gameplay;

public sealed class SecondPasswordGameplay(World world)
{
    public Task CheckAsync(ServiceHandleContext context)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());

        session.Send(new CharacterSecondPasswordResponse
        {
            State = E_PASSWORD_STATE.ePASSWORD_STATE_AUTHENTICATED,
            ErrorCode = 0
        });
        return Task.CompletedTask;
    }
}