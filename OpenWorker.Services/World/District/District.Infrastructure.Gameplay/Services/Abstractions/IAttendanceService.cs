namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IAttendanceService
{
    ValueTask SendCurrentState(CancellationToken ct = default);
}


internal sealed record AttendanceService : IAttendanceService
{
    public ValueTask SendCurrentState(CancellationToken ct = default)
    {
        // current table
        // timer init
        // received reward
        // future reward

        throw new NotImplementedException();
    }
}

