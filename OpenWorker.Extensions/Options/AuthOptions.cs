namespace OpenWorker.Extensions;

/// <summary>
/// How the auth server logs players in, in the <c>Auth</c> section.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Signs in as the first account without checking the ticket. Local development only.
    /// </summary>
    public bool AllowNextHumanNetworkLogin { get; set; }
}
