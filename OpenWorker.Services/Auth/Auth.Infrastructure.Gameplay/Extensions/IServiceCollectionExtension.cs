﻿using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Services;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddAuthGameplay(this IServiceCollection @this) => @this
        .AddScoped<IAuthService, AuthService>()
        .AddScoped<IGateService, GateService>();
}