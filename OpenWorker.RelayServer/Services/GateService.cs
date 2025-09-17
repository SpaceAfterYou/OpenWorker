using MassTransit;
using OpenWorker.Domain.Relay;
using OpenWorker.Domain.Relay.Requests;
using OpenWorker.Domain.Relay.Responses;

namespace OpenWorker.RelayServer.Services;

public sealed class GateService : IConsumer<UpdateGateRequest>, IConsumer<ListGateRequest>
{
    private readonly List<GateRelay> _gates = [];

    public Task Consume(ConsumeContext<ListGateRequest> context)
    {
        return context.RespondAsync(new GateListResponse(_gates));
    }

    public Task Consume(ConsumeContext<UpdateGateRequest> context)
    {
        var index = _gates.FindIndex(e => e.Id == context.Message.Id);

        if (index != -1)
        {
            _gates[index] = new GateRelay(context.Message);
        }

        else
        {
            _gates.Add(new GateRelay(context.Message));
        }

        return Task.CompletedTask;
    }
}