using System.Numerics;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Domain.Persistent;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Gestures.Types;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Persistence;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Gameplay;

public sealed class PersonGameplay(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    IConfiguration configuration,
    ILogger<PersonGameplay> logger,
    WorldManager worldManager)
{
    private short Gate { get; } = configuration.GetGate();

    public async Task CreateAsync(ServiceHandleContext context, PersonCreateRequest message)
    {
        var slotIndex = registry.FindFreeSlotIndex(context.GetPlayerEntity());
            
        if (slotIndex == -1)
        {
            logger.LogDebug("Slot not found.");
            return;
        }
        
        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var claims = world.Get<ClaimsComponent>(context.GetPlayerEntity());

        var account = await database.Accounts
            .FirstAsync(x => x.Id == claims.Account, context.CancellationToken)
            .ConfigureAwait(false);

        var gate = await database.Gates
            .FirstAsync(x => x.Id == Gate, context.CancellationToken)
            .ConfigureAwait(false);

        var person = new PersonPersistent
        {
            Name = message.Person.InfoValue.Name,
            Hero = message.Person.InfoValue.Hero,

            DefaultHairStyle = message.Person.InfoValue.AppearanceShape.HairStyle,
            DefaultHairColor = message.Person.InfoValue.AppearanceShape.HairColor,
            DefaultEyeColor = message.Person.InfoValue.AppearanceShape.EyeColor,
            DefaultSkinColor = message.Person.InfoValue.AppearanceShape.SkinColor,

            EquippedHairStyle = message.Person.InfoValue.AppearanceLook.HairStyle,
            EquippedHairColor = message.Person.InfoValue.AppearanceLook.HairColor,
            EquippedEyeColor = message.Person.InfoValue.AppearanceLook.EyeColor,
            EquippedSkinColor = message.Person.InfoValue.AppearanceLook.SkinColor,
        
            Location = 10003,
            PositionX = 10228.605f,
            PositionY = 10058.951f,
            PositionZ = 90.452f,
            RotationX = 90.0f,
            
            FatiguePointCommon = 200,
            
            GestureList = Enumerable
                .Repeat(0, GesturesModuleDefines.MaxGestureCount)
                .ToArray(),
            
            OptionList = Enumerable
                .Repeat((byte)'1', LoginModuleDefines.PersonOptionCount)
                .ToArray(),
        
            Account = account,
            Gate = gate
        };

        account.Persons.Add(person);

        await database
            .SaveChangesAsync(context.CancellationToken)
            .ConfigureAwait(false);
        
        registry.CreatePerson(context.GetPlayerEntity(), slotIndex, person);
        
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        session.Send(PersonSnapshotMapper.CreateCharacterListResponse(world, context.GetPlayerEntity()));
    }

    public Task ListAsync(ServiceHandleContext context, PersonListRequest message)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        
        session.Send(PersonSnapshotMapper.CreateCharacterListResponse(world, context.GetPlayerEntity()));
        
        return Task.CompletedTask;
    }
    
    public async Task SelectAsync(ServiceHandleContext context, PersonSelectRequest message)
    {
        await worldManager
            .SelectPerson(context.GetPlayerEntity(), message.Actor.Identifier)
            .ConfigureAwait(false);

        // session.Send(new LoginOptionLoadResponse(world, person));
    }
}