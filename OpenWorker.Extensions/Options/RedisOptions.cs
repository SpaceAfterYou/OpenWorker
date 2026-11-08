using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace OpenWorker.Extensions;

/// <summary>
/// Where Redis is, in <c>ConnectionStrings:Redis</c> (or <c>ConnectionStrings__Redis</c> in the environment).
/// One setting serves both users of Redis: the cache (Redis.OM, which takes the address as it is)
/// and the notifications between servers (StackExchange.Redis, which <see cref="ToConfigurationOptions"/> builds).
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// The name under <c>ConnectionStrings</c>.
    /// </summary>
    public const string ConnectionName = "Redis";

    private const int DefaultPort = 6379;

    /// <summary>
    /// An address like <c>redis://redis:6379</c>, with <c>redis://user:password@host:port</c> for a Redis that wants a login
    /// and <c>rediss://</c> for TLS.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ConnectionStrings:Redis must be set to the address of Redis, for example redis://redis:6379.")]
    [RedisAddress(ErrorMessage = "ConnectionStrings:Redis must be an address like redis://host:6379 (or rediss:// for TLS).")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The same Redis for a StackExchange.Redis connection. A server that starts before Redis is ready keeps trying.
    /// </summary>
    public ConfigurationOptions ToConfigurationOptions()
    {
        if (!RedisAddressAttribute.TryParse(ConnectionString, out var address))
        {
            throw new InvalidOperationException($"ConnectionStrings:Redis is not an address of Redis: '{ConnectionString}'.");
        }

        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Ssl = address.Scheme == "rediss"
        };

        options.EndPoints.Add(address.Host, address.Port > 0 ? address.Port : DefaultPort);

        if (address.UserInfo is { Length: > 0 } login)
        {
            var separator = login.IndexOf(':', StringComparison.Ordinal);

            if (separator < 0)
            {
                options.Password = Uri.UnescapeDataString(login);
            }

            else
            {
                options.User = Uri.UnescapeDataString(login[..separator]) is { Length: > 0 } user ? user : null;
                options.Password = Uri.UnescapeDataString(login[(separator + 1)..]);
            }
        }

        return options;
    }
}

[OptionsValidator]
internal sealed partial class RedisOptionsValidator : IValidateOptions<RedisOptions>;
