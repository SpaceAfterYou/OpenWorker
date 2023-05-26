namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IPostService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">rect, sent, account recv</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask Check(byte type, CancellationToken ct);
}
