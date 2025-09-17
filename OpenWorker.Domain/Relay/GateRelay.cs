using OpenWorker.Domain.Relay.Requests;

namespace OpenWorker.Domain.Relay;

public readonly record struct GateRelay(int Id, string Name)
{
    public GateRelay(UpdateGateRequest request) : this(request.Id, request.Name)
    {
    }
}