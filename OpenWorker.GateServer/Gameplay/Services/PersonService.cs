using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Requests;

namespace OpenWorker.GateServer.Gameplay.Services;

[HotspotHandler(HotspotHandlerType.Gate)]
public sealed class PersonService(PersonGameplay person, SecondPasswordGameplay secondPassword) :
    IHotspotHandler<PersonListRequest>, 
    IHotspotHandler<PersonCreateRequest>,
    IHotspotHandler<PersonSecondPasswordRequest>,
    IHotspotHandler<PersonSelectRequest>,
    IHotspotHandler<PersonDeleteReqRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonDeleteReqRequest request)
    {
        await person
            .DeleteAsync(context, request);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeServerRequest request)
    {
        await person
            .ChangeServerAsync(context, request);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonCreateRequest request)
    {
        await person
            .CreateAsync(context, request);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonSecondPasswordRequest request)
    {
        await secondPassword
            .HandleAsync(context, request);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonSelectRequest request)
    {
        await person
            .SelectAsync(context, request);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonListRequest request)
    {
        await person
            .ListAsync(context, request);
    }
}