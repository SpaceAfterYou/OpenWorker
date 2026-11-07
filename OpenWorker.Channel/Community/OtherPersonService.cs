using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Which window asks about another player: they differ in the answer, not in what is shown.
/// </summary>
public enum OtherInfoWindow : byte
{
    Character = 0,
    Friend = 1,
    Awaiter = 2
}

/// <summary>
/// Shows a player to another. The server a player is connected to knows their equipment and what they chose, so it answers
/// for them wherever the one who looks is. Somebody who is not connected is shown as the database remembers them.
/// </summary>
public sealed class OtherPersonService(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    IPersonRelations relations,
    CombatTables combat,
    ILogger<OtherPersonService> logger
) :
    ICommunityModule
{
    public const string AskEvent = "person.other-info";

    /// <summary>
    /// The text the client shows when a player does not let others look at them.
    /// </summary>
    public const int Refused = 59200;

    private const int AchievementCategories = 7;

    public void Attach(CommunityNetwork community)
    {
        community.On(AskEvent, OnAskedAsync);
    }

    /// <summary>
    /// The viewer asks to see the person. The answer arrives from wherever the person is.
    /// </summary>
    public async ValueTask AskAsync(Entity viewer, int person, OtherInfoWindow window, bool checkOption, bool calculateStat)
    {
        var viewerPerson = world.Get<ActorComponent>(viewer).Identifier;

        // The person is here, or on another server.
        if (network.TryGet(person, out var local))
        {
            await AnswerAsync(viewerPerson, local, window, checkOption, calculateStat);
            return;
        }

        var online = await network.FindAsync(person);

        if (online is not null)
        {
            await network.PublishAsync([person], AskEvent, Encode(viewerPerson, window, checkOption, calculateStat));
            return;
        }

        await AnswerFromDatabaseAsync(viewer, viewerPerson, person, window, checkOption, calculateStat);
    }

    private async ValueTask OnAskedAsync(CommunityEvent value)
    {
        if (!TryDecode(value.Body.Span, out var viewer, out var window, out var check, out var calculate))
        {
            return;
        }

        foreach (var person in value.Persons)
        {
            if (network.TryGet(person, out var player))
            {
                await AnswerAsync(viewer, player, window, check, calculate);
            }
        }
    }

    private async ValueTask AnswerAsync(int viewer, Entity target, OtherInfoWindow window, bool checkOption, bool calculateStat)
    {
        var person = world.Get<ActorComponent>(target).Identifier;
        var name = world.Get<PersonInfoComponent>(target).Name;

        if (window == OtherInfoWindow.Character && checkOption && await RefusesAsync(viewer, person, world.Get<PersonOptionComponent>(target).Collection))
        {
            await network.SendAsync(viewer, new PersonOtherInfoRefuseResponse { Error = Refused, Name = name });
            return;
        }

        await network.SendAsync(viewer, Create(window, FromEntity(target), calculateStat));
    }

    private async ValueTask AnswerFromDatabaseAsync(Entity viewer, int viewerPerson, int person, OtherInfoWindow window, bool checkOption, bool calculateStat)
    {
        var profile = await factory.LoadOtherPersonAsync(person, viewerPerson);

        // The connection can be gone by the time the database answers.
        if (!world.IsAlive(viewer))
        {
            return;
        }

        if (profile is not { } target ||
            (window == OtherInfoWindow.Character && checkOption && Refuses(target.OptionList, target.IsFriend)))
        {
            await network.SendAsync(viewerPerson, new PersonOtherInfoRefuseResponse { Error = Refused, Name = profile?.Name ?? string.Empty });
            return;
        }

        await network.SendAsync(viewerPerson, Create(window, FromProfile(target), calculateStat));
    }

    private async ValueTask<bool> RefusesAsync(int viewer, int person, IReadOnlyList<byte> options)
    {
        var level = PersonOptions.Level(options, PersonOptions.OtherInfo);

        return level switch
        {
            PersonOptions.Nobody => viewer != person,
            PersonOptions.OnlyFriends => viewer != person && !await relations.IsFriendAsync(person, viewer),
            _ => false
        };
    }

    private static bool Refuses(byte[] options, bool isFriend)
    {
        return PersonOptions.Level(options, PersonOptions.OtherInfo) switch
        {
            PersonOptions.Nobody => true,
            PersonOptions.OnlyFriends => !isFriend,
            _ => false
        };
    }

    private static IResponseHotspotMessage Create(OtherInfoWindow window, OtherPersonValue value, bool calculateStat)
    {
        return window switch
        {
            OtherInfoWindow.Friend => new FriendInfoResponse { Person = value, Title = value.Represent },
            OtherInfoWindow.Awaiter => new PartyAwaiterInfoResponse { Person = value, Title = value.Represent },
            _ => new PersonOtherInfoResponse
            {
                CalculateStat = calculateStat,
                Target = value,
                Categories = new ushort[AchievementCategories]
            }
        };
    }

    /// <summary>
    /// A player who is connected: everything the server knows.
    /// </summary>
    public OtherPersonValue FromEntity(Entity target)
    {
        var info = world.Get<PersonInfoComponent>(target);
        var community = world.Get<CommunityComponent>(target);
        var title = world.Get<TitleComponent>(target);
        var rank = world.Get<RankComponent>(target);

        var stats = combat
            .GetPerson(info.Hero, PersonModuleDefines.DefaultLevel)
            .ToStatList()
            .Select(e => new ST_STAT { wStatID = (short)e.byIndex, fValue = e.statValue })
            .ToArray();

        return new OtherPersonValue
        {
            Level = PersonModuleDefines.DefaultLevel,
            State = community.State,
            Comment = community.Comment,
            BaseStats = stats,
            Stats = stats,
            Equipment = EquipmentOf(target),
            EchelonLevel = rank.Level,
            EchelonExperience = rank.Experience,
            Memo = community.Memo,
            Class = (byte)info.Hero,
            Name = info.Name,
            Ability = new TitleValue(title.AbilityPrimary, title.AbilitySecondary),
            Represent = new TitleValue(title.Primary, title.Secondary),
            Sockets = [],
            Broaches = []
        };
    }

    /// <summary>
    /// A player who is not connected: what is saved, so no equipment and no characteristics.
    /// </summary>
    public static OtherPersonValue FromProfile(OtherPersonProfile profile)
    {
        return new OtherPersonValue
        {
            Level = PersonModuleDefines.DefaultLevel,
            State = (CommunityState)profile.CommunityState,
            Comment = profile.CommunityComment,
            BaseStats = [],
            Stats = [],
            Equipment = [],
            Memo = profile.CommunityMemo,
            Class = profile.Hero,
            Name = profile.Name,
            Ability = new TitleValue(profile.TitleAbilityPrimary, profile.TitleAbilitySecondary),
            Represent = new TitleValue(profile.TitlePrimary, profile.TitleSecondary),
            Sockets = [],
            Broaches = []
        };
    }

    private StorageValue[] EquipmentOf(Entity target)
    {
        try
        {
            var storage = world.Get<StorageComponent>(target);

            return storage.Collection
                .Where(e => world.Get<StorageGroupComponent>(e).Group is StorageGroup.AbilityEquip or StorageGroup.ShapeEquip)
                .SelectMany(e =>
                {
                    var group = world.Get<StorageGroupComponent>(e).Group;

                    return world.Get<StorageContentComponent>(e).SlotList
                        .Select((item, slot) => (Item: item, Slot: slot))
                        .Where(x => x.Item != Entity.Null)
                        .Select(x => new StorageValue
                        {
                            Item = ItemDtoFactory.FromEntity(world, x.Item),
                            Storage = group,
                            Slot = (short)x.Slot
                        });
                })
                .ToArray();
        }

        catch (Exception e)
        {
            // What a player wears is not worth losing the answer for.
            logger.LogError(e, "Could not list the equipment of a player.");

            return [];
        }
    }

    private static byte[] Encode(int viewer, OtherInfoWindow window, bool check, bool calculate)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(viewer);
        writer.Write((byte)window);
        writer.Write(check);
        writer.Write(calculate);
        writer.Flush();

        return stream.ToArray();
    }

    private static bool TryDecode(ReadOnlySpan<byte> body, out int viewer, out OtherInfoWindow window, out bool check, out bool calculate)
    {
        viewer = 0;
        window = default;
        check = calculate = false;

        if (body.Length != 7 || !Enum.IsDefined((OtherInfoWindow)body[4]))
        {
            return false;
        }

        viewer = BitConverter.ToInt32(body);
        window = (OtherInfoWindow)body[4];
        check = body[5] != 0;
        calculate = body[6] != 0;

        return true;
    }
}
