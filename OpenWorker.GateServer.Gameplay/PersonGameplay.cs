using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Gameplay.Modules.Login;
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
    SecondPasswordGameplay secondPassword,
    LeaveManager leaving,
    WorldManager worldManager,
    PersonCreationCatalog catalog)
{
    private short Gate { get; } = configuration.GetGate();

    public async Task CreateAsync(ServiceHandleContext context, PersonCreateRequest message)
    {
        var slotIndex = registry.FindFreeSlotIndex(context.Player);

        if (slotIndex == -1)
        {
            logger.LogDebug("Slot not found.");

            SendListError(context.Player, PersonErrorCode.NoEmptySlot);
            return;
        }

        if (!LoginRules.IsValidPersonName(message.Person.InfoValue.Name))
        {
            SendListError(context.Player, PersonErrorCode.InvalidName);
            return;
        }

        var hero = message.Person.InfoValue.Hero;

        if (!Enum.IsDefined(hero) ||
            !catalog.TryGetCharacter(hero, out var character) ||
            !catalog.IsOffered(hero, message.Person.InfoValue.AppearanceShape) ||
            !PersonCreationCatalog.IsOffered(character, message.Outfit))
        {
            SendListError(context.Player, PersonErrorCode.CreateFailed);
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var claims = world.Get<ClaimsComponent>(context.Player);

        var name = message.Person.InfoValue.Name;

        if (await database.Persons.AnyAsync(x => x.Name == name, context.CancellationToken))
        {
            SendListError(context.Player, PersonErrorCode.NameInUse);
            return;
        }

        var account = await database.Accounts
            .FirstAsync(x => x.Id == claims.Account, context.CancellationToken);

        var gate = await database.Gates
            .FirstAsync(x => x.Id == Gate, context.CancellationToken);

        var person = new PersonPersistent
        {
            Name = message.Person.InfoValue.Name,
            Hero = hero,

            DefaultHairStyle = message.Person.InfoValue.AppearanceShape.HairStyle,
            DefaultHairColor = message.Person.InfoValue.AppearanceShape.HairColor,
            DefaultEyeColor = message.Person.InfoValue.AppearanceShape.EyeColor,
            DefaultSkinColor = message.Person.InfoValue.AppearanceShape.SkinColor,

            // A new character has no items that change its look.
            EquippedHairStyle = 0,
            EquippedHairColor = 0,
            EquippedEyeColor = 0,
            EquippedSkinColor = 0,

            Outfit = message.Outfit,
            LastSelectedAt = DateTime.UtcNow,

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
            .SaveChangesAsync(context.CancellationToken);

        registry.CreatePerson(context.Player, slotIndex, person);

        var session = world.Get<ServerSessionComponent>(context.Player);
        session.Send(PersonSnapshotMapper.CreateCharacterListResponse(world, context.Player));
    }

    public Task ListAsync(ServiceHandleContext context, PersonListRequest message)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(PersonSnapshotMapper.CreateCharacterListResponse(world, context.Player));

        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ServiceHandleContext context, PersonDeleteReqRequest message)
    {
        var claims = world.Get<ClaimsComponent>(context.Player);
        var person = message.Actor.Identifier;

        if (claims.Account == 0 || !secondPassword.IsSatisfied(context.Player))
        {
            SendListError(context.Player, PersonErrorCode.Failed);
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var persistent = await database.Persons
            .FirstOrDefaultAsync(
                x => x.Id == person && x.Account.Id == claims.Account && x.Gate.Id == Gate,
                context.CancellationToken);

        if (persistent is null)
        {
            SendListError(context.Player, PersonErrorCode.Failed);
            return;
        }

        var isLeagueMaster = await database
            .Set<LeaguePersistent>()
            .AnyAsync(x => x.MasterId == person, context.CancellationToken);

        if (isLeagueMaster)
        {
            SendListError(context.Player, PersonErrorCode.LeagueMaster);
            return;
        }

        database.Persons.Remove(persistent);

        try
        {
            await database
                .SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Failed to delete person {Person}.", person);

            SendListError(context.Player, PersonErrorCode.Failed);
            return;
        }

        registry.RemovePerson(context.Player, person);

        world.Get<ServerSessionComponent>(context.Player)
            .Send(PersonSnapshotMapper.CreateCharacterListResponse(world, context.Player));
    }

    public async Task ChangeServerAsync(ServiceHandleContext context, PersonChangeServerRequest message)
    {
        // The character selection is already here, only the server list can be left for.
        if (message.Destination is LeaveDestination.ServerList)
        {
            await leaving.LeaveAsync(context.Player, message.Destination);
        }
    }

    public async Task SelectAsync(ServiceHandleContext context, PersonSelectRequest message)
    {
        if (!secondPassword.IsSatisfied(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(new CharacterSelectResponse
            {
                Zone = EnterMapResultValue.Failed(EnterMapResultValue.CannotEnter)
            });
            return;
        }

        var selected = await worldManager
            .SelectPerson(context.Player, message.Actor.Identifier);

        if (selected)
        {
            await RememberSelectedAsync(context, message.Actor.Identifier);
        }
    }

    /// <summary>
    /// The character list highlights this person on the next visit.
    /// </summary>
    private async Task RememberSelectedAsync(ServiceHandleContext context, int identifier)
    {
        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var person = await database.Persons
            .FirstOrDefaultAsync(e => e.Id == identifier, context.CancellationToken);

        if (person is null)
        {
            return;
        }

        person.LastSelectedAt = DateTime.UtcNow;

        await database
            .SaveChangesAsync(context.CancellationToken);

        registry.MarkLastPerson(context.Player, identifier);
    }

    private void SendListError(Entity player, PersonErrorCode code)
    {
        world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(
            new MessageOpcode(GroupOpcode.Character, CharacterOpcode.ListRes),
            code));
    }
}
