using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Services;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddGateGameplay(this IServiceCollection @this) => @this
        .AddScoped<IAuthService, AuthService>()
        .AddScoped<IAccountService, AccountService>()
        .AddScoped<IPersonService, PersonService>();
}
