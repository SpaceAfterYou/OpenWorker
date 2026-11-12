using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Registers the settings of the servers as options that are checked when the host starts: a server with a missing
/// or wrong setting stops before it serves anybody, and says which setting it is. Every method may be called any
/// number of times, so each part of a server asks for the settings it reads.<br/>
/// The defaults live in the <c>appsettings.json</c> of each server project.
/// </summary>
public static class OptionsServiceCollectionExtensions
{
    /// <summary>
    /// The port the server listens on, the game version, the packet dumps and the time a connection has to enter, the
    /// errors a session may cause, the packets it may send, the time the server takes to stop and where the metrics go:
    /// what every server has.
    /// </summary>
    public static IServiceCollection AddServerOptions(this IServiceCollection services)
    {
        services.AddValidated<ServerOptions, ServerOptionsValidator>(ServerOptions.SectionName);
        services.AddValidated<GameOptions, GameOptionsValidator>(GameOptions.SectionName);
        services.AddValidated<ShutdownOptions, ShutdownOptionsValidator>(ShutdownOptions.SectionName);
        services.AddHandlerErrorBudgetOptions();
        services.AddPacketRateLimitOptions();
        services.AddTelemetryOptions();

        return services.AddValidated<HotspotOptions, HotspotOptionsValidator>(HotspotOptions.SectionName);
    }

    /// <summary>
    /// The port of the health endpoint and the time its checks may take: every server that serves one has it,
    /// the relay included.
    /// </summary>
    public static IServiceCollection AddHealthOptions(this IServiceCollection services)
    {
        services.AddValidated<HealthOptions, HealthOptionsValidator>(HealthOptions.SectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<HealthOptions>, HealthPortValidator>());

        return services;
    }

    /// <summary>
    /// How many packets of a session may fail before the server closes the connection: what the handlers read.
    /// </summary>
    public static IServiceCollection AddHandlerErrorBudgetOptions(this IServiceCollection services)
    {
        return services.AddValidated<HandlerErrorBudgetOptions, HandlerErrorBudgetOptionsValidator>(HandlerErrorBudgetOptions.SectionName);
    }

    /// <summary>
    /// How many packets of each group a session may send: what the handlers read.
    /// </summary>
    public static IServiceCollection AddPacketRateLimitOptions(this IServiceCollection services)
    {
        return services.AddValidated<PacketRateLimitOptions, PacketRateLimitOptionsValidator>(PacketRateLimitOptions.SectionName);
    }

    /// <summary>
    /// Where the metrics of the process are sent. The servers that run a world loop get it with <see cref="AddServerOptions"/>;
    /// the relay asks for it itself.
    /// </summary>
    public static IServiceCollection AddTelemetryOptions(this IServiceCollection services)
    {
        return services.AddValidated<TelemetryOptions, TelemetryOptionsValidator>(TelemetryOptions.SectionName);
    }

    /// <summary>
    /// Who the process is and where clients reach it: what the district and maze servers have.
    /// </summary>
    public static IServiceCollection AddInstanceOptions(this IServiceCollection services)
    {
        return services.AddValidated<InstanceOptions, InstanceOptionsValidator>(InstanceOptions.SectionName);
    }

    /// <summary>
    /// A district also needs the location it serves and the port clients reach it at.
    /// </summary>
    public static IServiceCollection AddDistrictOptions(this IServiceCollection services)
    {
        services.AddInstanceOptions();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InstanceOptions>, DistrictInstanceValidator>());

        return services;
    }

    public static IServiceCollection AddGateOptions(this IServiceCollection services)
    {
        return services.AddValidated<GateOptions, GateOptionsValidator>(GateOptions.SectionName);
    }

    /// <summary>
    /// Where clients reach the gate server: only the gate server has it.
    /// </summary>
    public static IServiceCollection AddGateEndpointOptions(this IServiceCollection services)
    {
        return services.AddValidated<GateEndpointOptions, GateEndpointOptionsValidator>(GateOptions.SectionName);
    }

    public static IServiceCollection AddAuthOptions(this IServiceCollection services)
    {
        return services.AddBound<AuthOptions>(AuthOptions.SectionName);
    }

    /// <summary>
    /// Where clients reach the auth server: the servers that send a player back to the server list answer with it.
    /// </summary>
    public static IServiceCollection AddAuthEndpointOptions(this IServiceCollection services)
    {
        return services.AddValidated<AuthEndpointOptions, AuthEndpointOptionsValidator>(AuthOptions.SectionName);
    }

    public static IServiceCollection AddMazeEndpointOptions(this IServiceCollection services)
    {
        return services.AddValidated<MazeEndpointOptions, MazeEndpointOptionsValidator>(MazeEndpointOptions.SectionName);
    }

    public static IServiceCollection AddChannelOptions(this IServiceCollection services)
    {
        return services.AddValidated<ChannelOptions, ChannelOptionsValidator>(ChannelOptions.SectionName);
    }

    /// <summary>
    /// What the players of a channel see of each other: what the district and the maze have.
    /// </summary>
    public static IServiceCollection AddWorldOptions(this IServiceCollection services)
    {
        return services.AddValidated<WorldOptions, WorldOptionsValidator>(WorldOptions.SectionName);
    }

    /// <summary>
    /// Who may use the test commands of the chat: what the district and the maze have.
    /// </summary>
    public static IServiceCollection AddCommandOptions(this IServiceCollection services)
    {
        return services.AddValidated<CommandOptions, CommandOptionsValidator>(CommandOptions.SectionName);
    }

    /// <summary>
    /// How generous the game servers are with experience and drops: what the district and the maze have.
    /// </summary>
    public static IServiceCollection AddRewardsOptions(this IServiceCollection services)
    {
        return services.AddValidated<RewardsOptions, RewardsOptionsValidator>(RewardsOptions.SectionName);
    }

    /// <summary>
    /// How a new character starts: what the gate, which creates characters, has.
    /// </summary>
    public static IServiceCollection AddPersonCreationOptions(this IServiceCollection services)
    {
        return services.AddValidated<PersonCreationOptions, PersonCreationOptionsValidator>(PersonCreationOptions.SectionName);
    }

    /// <summary>
    /// When the day of the attendance rewards starts: what the servers that give them have.
    /// </summary>
    public static IServiceCollection AddAttendanceOptions(this IServiceCollection services)
    {
        return services.AddValidated<AttendanceOptions, AttendanceOptionsValidator>(AttendanceOptions.SectionName);
    }

    /// <summary>
    /// When the special daily missions are drawn again: what the servers that give them have.
    /// </summary>
    public static IServiceCollection AddDailyMissionOptions(this IServiceCollection services)
    {
        return services.AddValidated<DailyMissionOptions, DailyMissionOptionsValidator>(DailyMissionOptions.SectionName);
    }

    /// <summary>
    /// The secrets of the relay server.
    /// </summary>
    public static IServiceCollection AddCredentialsOptions(this IServiceCollection services)
    {
        return services.AddValidated<CredentialsOptions, CredentialsOptionsValidator>(CredentialsOptions.SectionName);
    }

    /// <summary>
    /// <c>ConnectionStrings:Redis</c>, shared by the cache and the notifications between servers.
    /// </summary>
    public static IServiceCollection AddRedisOptions(this IServiceCollection services)
    {
        return services.AddValidated<RedisOptions, RedisOptionsValidator>(
            section: null,
            builder => builder.Configure<IConfiguration>((options, configuration) =>
                options.ConnectionString = configuration.GetConnectionString(RedisOptions.ConnectionName) ?? string.Empty));
    }

    /// <summary>
    /// <c>ConnectionStrings:Postgres</c> and the <c>Persistence</c> section.
    /// </summary>
    public static IServiceCollection AddPersistenceOptions(this IServiceCollection services)
    {
        return services.AddValidated<PersistenceOptions, PersistenceOptionsValidator>(
            PersistenceOptions.SectionName,
            builder => builder.Configure<IConfiguration>((options, configuration) =>
                options.ConnectionString = configuration.GetConnectionString(PersistenceOptions.ConnectionName) ?? string.Empty));
    }

    /// <summary>
    /// Options that have nothing to check: any value is a value.
    /// </summary>
    private static IServiceCollection AddBound<TOptions>(this IServiceCollection services, string section) where TOptions : class
    {
        if (!services.Any(e => e.ServiceType == typeof(IConfigureOptions<TOptions>)))
        {
            services.AddOptions<TOptions>().BindConfiguration(section);
        }

        return services;
    }

    private static IServiceCollection AddValidated<TOptions, TValidator>(
        this IServiceCollection services,
        string? section,
        Action<OptionsBuilder<TOptions>>? configure = null)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        if (services.Any(e => e.ServiceType == typeof(IValidateOptions<TOptions>) && e.ImplementationType == typeof(TValidator)))
        {
            return services;
        }

        var builder = services.AddOptions<TOptions>();

        if (section is not null)
        {
            builder.BindConfiguration(section);
        }

        configure?.Invoke(builder);

        builder.ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>, TValidator>());

        return services;
    }
}
