namespace OpenWorker.Domain.Relay.Responses;

public sealed record GateListResponse(IReadOnlyCollection<GateRelay> Values);