using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where Redis is, in <c>ConnectionStrings:Redis</c> (or <c>ConnectionStrings__Redis</c> in the environment).
/// One setting serves both users of Redis: the cache (Redis.OM, which takes the address as it is)
/// and the notifications between servers (StackExchange.Redis, which <c>ToConfigurationOptions</c> of
/// <c>OpenWorker.Infrastructure.Redis</c> builds).
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// The name under <c>ConnectionStrings</c>.
    /// </summary>
    public const string ConnectionName = "Redis";

    /// <summary>
    /// An address like <c>redis://redis:6379</c>, with <c>redis://user:password@host:port</c> for a Redis that wants a login
    /// and <c>rediss://</c> for TLS.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ConnectionStrings:Redis must be set to the address of Redis, for example redis://redis:6379.")]
    [RedisAddress(ErrorMessage = "ConnectionStrings:Redis must be an address like redis://host:6379 (or rediss:// for TLS).")]
    public string ConnectionString { get; set; } = string.Empty;
}

[OptionsValidator]
internal sealed partial class RedisOptionsValidator : IValidateOptions<RedisOptions>;
