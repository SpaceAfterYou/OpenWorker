namespace OpenWorker.DependencyInjection.Health;

/// <summary>
/// The tags that sort the checks into the two endpoints.
/// </summary>
internal static class HealthTags
{
    /// <summary>
    /// <c>/healthz</c>: the process is alive and worth keeping. A failure here means a restart.
    /// </summary>
    public const string Live = "live";

    /// <summary>
    /// <c>/readyz</c>: the server can take players now. A failure here means send nobody, it does not mean restart.
    /// </summary>
    public const string Ready = "ready";
}
