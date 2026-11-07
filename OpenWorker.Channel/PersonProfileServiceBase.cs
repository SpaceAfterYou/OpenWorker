using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel;

/// <summary>
/// What a person says about themselves and what the others can see of it.
/// </summary>
public abstract class PersonProfileServiceBase(
    World world,
    ReadOnlyCollection<TitleInfoRow> titles,
    IDbContextFactory<PersistenceContext> factory) :
    IHotspotHandler<PersonLoadTitleRequest>,
    IHotspotHandler<PersonUpdateTitleRequest>,
    IHotspotHandler<PersonCommunityRequest>,
    IHotspotHandler<PersonOtherInfoRequest>
{
    private const byte PrefixTitle = 0;
    private const byte SuffixTitle = 1;

    private const int OtherInfoRefused = 59200;

    /// <summary>
    /// The place of this setting among the options a player has saved, as a digit.
    /// </summary>
    private const int OtherInfoOption = 26;
    private const byte OnlyFriends = (byte)'1';
    private const byte Nobody = (byte)'2';

    private const int MaxCommentCharacters = PersonModuleDefines.MaxCommunityCommentLength - 1;
    private const int MaxMemoCharacters = PersonModuleDefines.MaxCommunityMemoLength - 1;
    private const int AchievementCategories = 7;

    /// <summary>
    /// Titles are not earned yet, so a person has the prefixes that do not depend on the class. With suffixes the list would not fit: its size is a single byte.
    /// </summary>
    private Dictionary<int, byte> OwnedTitles { get; } = titles
        .Where(e => e is { Type: PrefixTitle, Class: 0 })
        .ToDictionary(e => e.Id, e => e.Type);

    protected abstract void BroadcastToOthers(Entity player, IResponseHotspotMessage message);

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonLoadTitleRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var title = world.Get<TitleComponent>(context.Player);

        session.Send(new PersonLoadTitleResponse { TitleList = OwnedTitles.Keys, OpenList = [], Result = true });

        // Nothing else tells the client which titles are chosen.
        session.Send(CreateTitleResponse(title, true));

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonUpdateTitleRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var current = world.Get<TitleComponent>(context.Player);

        if (!IsValid(request.Ability) || !IsValid(request.Represent))
        {
            session.Send(CreateTitleResponse(current, false));
            return;
        }

        var chosen = new TitleComponent
        {
            AbilityPrimary = request.Ability.Primary,
            AbilitySecondary = request.Ability.Secondary,
            Primary = request.Represent.Primary,
            Secondary = request.Represent.Secondary
        };

        world.Set(context.Player, chosen);

        session.Send(CreateTitleResponse(chosen, true));

        if (chosen.Primary != current.Primary || chosen.Secondary != current.Secondary)
        {
            BroadcastToOthers(context.Player, new PersonUpdateTitleBroadcastResponse
            {
                Title = request.Represent,
                Actor = world.Get<ActorComponent>(context.Player).Identifier
            });
        }

        if (chosen != current)
        {
            await factory.SaveTitlesAsync(
                world.Get<ActorComponent>(context.Player).Identifier,
                chosen.AbilityPrimary,
                chosen.AbilitySecondary,
                chosen.Primary,
                chosen.Secondary);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonCommunityRequest request)
    {
        // The client has no room for more, so this one did not come from it.
        if (request.Comment.Length > MaxCommentCharacters ||
            request.Memo.Length > MaxMemoCharacters ||
            !Enum.IsDefined(request.State))
        {
            return;
        }

        var person = world.Get<ActorComponent>(context.Player).Identifier;

        world.Set(context.Player, new CommunityComponent
        {
            State = request.State,
            Comment = request.Comment,
            Memo = request.Memo
        });

        world.Get<ServerSessionComponent>(context.Player).Send(CreateCommunityResponse(world.Get<CommunityComponent>(context.Player)));

        await factory.SaveCommunityAsync(person, (byte)request.State, request.Comment, request.Memo);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonOtherInfoRequest request)
    {
        var viewer = world.Get<ActorComponent>(context.Player).Identifier;
        var profile = await factory.LoadOtherPersonAsync(request.Person, viewer);

        // The connection can be gone by the time the database answers.
        if (!world.IsAlive(context.Player))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        if (profile is not { } target || (request.CheckOption && Refuses(target)))
        {
            session.Send(new PersonOtherInfoRefuseResponse { Error = OtherInfoRefused, Name = profile?.Name ?? string.Empty });
            return;
        }

        // The characteristics of another player are not worked out here.
        session.Send(new PersonOtherInfoResponse
        {
            CalculateStat = request.CalculateStat,
            Target = new OtherPersonValue
            {
                Level = target.Level,
                EchelonExperience = 0,
                State = (CommunityState)target.CommunityState,
                Comment = target.CommunityComment,
                BaseStats = [],
                Stats = [],
                Equipment = (target.Gear ?? [])
                    .Select(e => new StorageValue { Storage = (StorageGroup)e.Storage, Slot = e.Slot, Item = ItemInstance.From(e).ToValue() })
                    .ToArray(),
                Memo = target.CommunityMemo,
                Class = target.Hero,
                Name = target.Name,
                Ability = new TitleValue(target.TitleAbilityPrimary, target.TitleAbilitySecondary),
                Represent = new TitleValue(target.TitlePrimary, target.TitleSecondary),
                Sockets = [],
                Broaches = []
            },
            Categories = new ushort[AchievementCategories]
        });
    }

    public static PersonCommunityResponse CreateCommunityResponse(CommunityComponent community)
    {
        return new PersonCommunityResponse
        {
            State = community.State,
            Comment = community.Comment,
            Memo = community.Memo
        };
    }

    private static PersonUpdateTitleResponse CreateTitleResponse(TitleComponent title, bool result)
    {
        return new PersonUpdateTitleResponse
        {
            Ability = new TitleValue(title.AbilityPrimary, title.AbilitySecondary),
            Represent = new TitleValue(title.Primary, title.Secondary),
            Result = result
        };
    }

    private static bool Refuses(OtherPersonProfile target)
    {
        if (target.OptionList.Length <= OtherInfoOption)
        {
            return false;
        }

        return target.OptionList[OtherInfoOption] switch
        {
            Nobody => true,
            OnlyFriends => !target.IsFriend,
            _ => false
        };
    }

    private bool IsValid(TitleValue title)
    {
        return IsValid(title.Primary, PrefixTitle) && IsValid(title.Secondary, SuffixTitle);
    }

    private bool IsValid(int title, byte type)
    {
        return title <= 0 || (OwnedTitles.TryGetValue(title, out var owned) && owned == type);
    }
}
