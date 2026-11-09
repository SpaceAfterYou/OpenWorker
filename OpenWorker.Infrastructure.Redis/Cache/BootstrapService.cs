using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// Makes the Redis indexes the servers search by, when the relay starts. It is safe to run while the other servers run,
/// and as often as the relay starts: an index that is there and has the schema of this build is left alone; a missing one
/// is created; one whose schema changed is dropped without its records (<c>FT.DROPINDEX</c> without <c>DD</c>) and created
/// again, and Redis indexes the records that are there. No record is ever deleted. The schema of each index is kept as a
/// signature in <see cref="SchemaKey"/>.
/// </summary>
internal sealed partial class BootstrapService(
    IRedisConnectionProvider provider,
    IConnectionMultiplexer redis,
    ILogger<BootstrapService> logger
) :
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogBootstrapping(logger);

        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<DocumentAttribute>() is not null)
            .ToArray();

        await RedisIndexes.EnsureAsync(provider, redis, types, logger);

        LogBootstrapped(logger);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Making the Redis indexes")]
    private static partial void LogBootstrapping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The Redis indexes are ready")]
    private static partial void LogBootstrapped(ILogger logger);
}

/// <summary>
/// The indexes of the documents of the cache and the version of their schema.
/// </summary>
public static partial class RedisIndexes
{
    /// <summary>
    /// A hash: the full name of a document type, the signature of the schema its index was made with.
    /// </summary>
    public const string SchemaKey = "ow:index-schema";

    /// <summary>
    /// Raised by hand when the indexes have to be made again for a reason the attributes do not show (a new version of
    /// Redis.OM that writes them differently).
    /// </summary>
    private const int Version = 1;

    /// <summary>
    /// Creates the missing indexes and makes again the ones whose schema changed, keeping every record.
    /// </summary>
    public static async Task EnsureAsync(IRedisConnectionProvider provider, IConnectionMultiplexer redis, IReadOnlyCollection<Type> types,
        ILogger logger)
    {
        var database = redis.GetDatabase();

        foreach (var type in types)
        {
            var name = type.FullName ?? type.Name;
            var signature = Signature(type);
            var stored = await database.HashGetAsync(SchemaKey, name);

            if (stored == signature)
            {
                // The index of this schema was made before; made again only if somebody dropped it.
                if (await provider.Connection.CreateIndexAsync(type))
                {
                    LogCreated(logger, name);
                }

                continue;
            }

            // A new schema, or one made before the schema was kept: the index goes, the records stay.
            var dropped = await provider.Connection.DropIndexAsync(type);

            await provider.Connection.CreateIndexAsync(type);
            await database.HashSetAsync(SchemaKey, name, signature);

            if (dropped)
            {
                LogRebuilt(logger, name);
            }

            else
            {
                LogCreated(logger, name);
            }
        }
    }

    /// <summary>
    /// What the index of the type is made of: the document attribute and every attribute of Redis.OM on its properties,
    /// with their values, in a fixed order.
    /// </summary>
    public static string Signature(Type type)
    {
        var text = new StringBuilder();

        text.Append(Version.ToString(CultureInfo.InvariantCulture)).Append('|').Append(type.FullName);

        if (type.GetCustomAttribute<DocumentAttribute>() is { } document)
        {
            Describe(text, document);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var attributes = property.GetCustomAttributes()
                .Where(e => e.GetType().Namespace?.StartsWith("Redis.OM", StringComparison.Ordinal) == true)
                .OrderBy(e => e.GetType().Name, StringComparer.Ordinal);

            foreach (var attribute in attributes)
            {
                text.Append('|').Append(property.Name).Append(':').Append(property.PropertyType.FullName);
                Describe(text, attribute);
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private static void Describe(StringBuilder text, Attribute attribute)
    {
        text.Append(':').Append(attribute.GetType().Name);

        foreach (var property in attribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(e => e.Name != nameof(Attribute.TypeId) && e.GetIndexParameters().Length == 0)
                     .OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var value = property.GetValue(attribute) switch
            {
                null => string.Empty,
                string one => one,
                System.Collections.IEnumerable many => string.Join(',', many.Cast<object?>().Select(Format)),
                var one => Format(one)
            };

            text.Append(';').Append(property.Name).Append('=').Append(value);
        }
    }

    private static string Format(object? value)
    {
        return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the Redis index of {Type}")]
    private static partial void LogCreated(ILogger logger, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "The schema of the Redis index of {Type} changed: the index was made again over the records that are there")]
    private static partial void LogRebuilt(ILogger logger, string type);
}
