using StackExchange.Redis;

namespace OpenWorker.Extensions;

/// <summary>
/// The setting of <see cref="RedisOptions"/> turned into a connection of StackExchange.Redis. It lives with the connection,
/// so that the options, which every layer reads, carry no Redis package.
/// </summary>
public static class RedisOptionsExtensions
{
    private const int DefaultPort = 6379;

    /// <summary>
    /// The same Redis for a StackExchange.Redis connection. A server that starts before Redis is ready keeps trying.
    /// </summary>
    public static ConfigurationOptions ToConfigurationOptions(this RedisOptions redis)
    {
        if (!RedisAddressAttribute.TryParse(redis.ConnectionString, out var address))
        {
            throw new InvalidOperationException($"ConnectionStrings:Redis is not an address of Redis: '{redis.ConnectionString}'.");
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
