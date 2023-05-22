using Gate.Infrastructure.Gameplay.Abstractions;
using Gate.Infrastructure.Gameplay.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gate.Infrastructure.Gameplay.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddGateGameplay(this IServiceCollection @this) => @this
        .AddScoped<IAuthService, AuthService>()
        .AddScoped<IAccountService, AccountService>()
        .AddScoped<IPersonService, PersonService>();
}
