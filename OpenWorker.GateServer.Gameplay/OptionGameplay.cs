using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;

namespace OpenWorker.GateServer.Gameplay;

public sealed class OptionGameplay
{
    public Task PushAsync(ServiceHandleContext context, LoginOptionUpdateRequest message) => Task.CompletedTask;
}