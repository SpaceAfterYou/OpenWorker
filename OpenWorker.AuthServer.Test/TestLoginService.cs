using System.Net;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.AuthServer.App;
using OpenWorker.AuthServer.Test.DependencyInjection;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.AuthServer.Test;

[TestClass]
public sealed class TestLoginService
{
    [TestMethod]
    public async Task TestLoginWithPassword()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<LoginGameplay>();
            services.AddSingleton<TestClient>();
            services.AddSingleton<TestServer>();

            services.AddSingleton(World.Create());

            services.AddTestPersistence();
            services.AddCache();
            services.AddSessionCache();
        });

        var host = builder.Build();

        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var factory = provider.GetRequiredService<IDbContextFactory<PersistenceContext>>();
        
        {
            await using var context = await factory.CreateDbContextAsync(TestContext.CancellationToken).ConfigureAwait(false);
            
            await context.Database.EnsureDeletedAsync(TestContext.CancellationToken).ConfigureAwait(false);
            await context.Database.EnsureCreatedAsync(TestContext.CancellationToken).ConfigureAwait(false);
            
            PasswordHash.Create("password", out var passwordHash, out var saltHash);
            
            await context.Accounts.AddAsync(new AccountPersistent
            {
                Id = 1,
                Username = "username",
                SaltHash = saltHash,
                PasswordHash = passwordHash
            }, TestContext.CancellationToken).ConfigureAwait(false);
            
            await context.SaveChangesAsync(TestContext.CancellationToken).ConfigureAwait(false);
        }
        
        var world = provider.GetRequiredService<World>();
        var entity = world.Create<ServerSessionComponent>();

        var server = provider.GetRequiredService<TestServer>();
        server.Start();
        
        var client = provider.GetRequiredService<TestClient>();
        await client.ConnectAsync((IPEndPoint)server.LocalEndpoint, TestContext.CancellationToken).ConfigureAwait(false);

        entity.Set(world, new ServerSessionComponent(client));
        
        var gameplay = provider.GetRequiredService<LoginGameplay>();
        
        await gameplay.TryJoinAsync(
            new ServiceHandleContext(entity, CancellationToken.None),
            new LoginAuthRequest("username", "password", "12-34-56-78-90-12")
        ).ConfigureAwait(false);
    }

    public TestContext TestContext { get; set; }
}