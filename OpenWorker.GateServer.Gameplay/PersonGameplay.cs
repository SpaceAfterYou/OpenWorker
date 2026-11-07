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
    IRedisCollection<SessionCache> sessions,
    WorldManager worldManager)
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

        if (!Enum.IsDefined(message.Person.InfoValue.Hero))
        {
            SendListError(context.Player, PersonErrorCode.CreateFailed);
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var claims = world.Get<ClaimsComponent>(context.Player);

        var name = message.Person.InfoValue.Name;

        if (await database.Persons.AnyAsync(x => x.Name == name, context.CancellationToken).ConfigureAwait(false))
        {
            SendListError(context.Player, PersonErrorCode.NameInUse);
            return;
        }

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
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var persistent = await database.Persons
            .FirstOrDefaultAsync(x => x.Id == person && x.Account.Id == claims.Account, context.CancellationToken)
            .ConfigureAwait(false);

        if (persistent is null)
        {
            SendListError(context.Player, PersonErrorCode.Failed);
            return;
        }

        var isLeagueMaster = await database
            .Set<LeaguePersistent>()
            .AnyAsync(x => x.Master.Id == person, context.CancellationToken)
            .ConfigureAwait(false);

        if (isLeagueMaster)
        {
            SendListError(context.Player, PersonErrorCode.LeagueMaster);
            return;
        }

        database.Persons.Remove(persistent);

        try
        {
            await database
                .SaveChangesAsync(context.CancellationToken)
                .ConfigureAwait(false);
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
        var claims = world.Get<ClaimsComponent>(context.Player);

        // Only the way back to the server list goes through the gate.
        if (claims.Account == 0 || message.Type is not ChangeServerType.EnterMap)
        {
            return;
        }

        await sessions
            .InsertAsync(new SessionCache
            {
                Session = claims.Key,
                Account = claims.Account,
                UpdatedAt = DateTime.UtcNow,
                ReturnToAuth = true
            })
            .ConfigureAwait(false);

        world.Get<ServerSessionComponent>(context.Player).Send(new PersonGobackLobbyResponse
        {
            Actor = default,
            Account = claims.Account,
            Address = configuration.GetAuthHost(),
            Port = configuration.GetAuthPort(),
            Type = ChangeServerType.EnterMap,
            Result = true
        });
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

        await worldManager
            .SelectPerson(context.Player, message.Actor.Identifier)
            .ConfigureAwait(false);

        // session.Send(new LoginOptionLoadResponse(world, person));
    }

    private void SendListError(Entity player, PersonErrorCode code)
    {
        world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(
            new MessageOpcode(GroupOpcode.Character, CharacterOpcode.ListRes),
            code));
    }
}
