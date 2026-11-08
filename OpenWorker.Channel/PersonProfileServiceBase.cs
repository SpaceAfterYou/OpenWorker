using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Channel.Community;
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

namespace OpenWorker.Channel;

/// <summary>
/// What a person says about themselves and what the others can see of it.
/// </summary>
public abstract class PersonProfileServiceBase(
    World world,
    TitleBook titles,
    IDbContextFactory<PersistenceContext> factory,
    OtherPersonService others,
    FriendNotifier friends) :
    IHotspotHandler<PersonLoadTitleRequest>,
    IHotspotHandler<PersonUpdateTitleRequest>,
    IHotspotHandler<PersonCommunityRequest>,
    IHotspotHandler<PersonOtherInfoRequest>
{
    private const int MaxCommentCharacters = PersonModuleDefines.MaxCommunityCommentLength - 1;
    private const int MaxMemoCharacters = PersonModuleDefines.MaxCommunityMemoLength - 1;

    protected abstract void BroadcastToOthers(Entity player, IResponseHotspotMessage message);

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonLoadTitleRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var title = world.Get<TitleComponent>(context.Player);

        titles.Send(context.Player);

        // Nothing else tells the client which titles are chosen.
        session.Send(CreateTitleResponse(title, true));

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonUpdateTitleRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var current = world.Get<TitleComponent>(context.Player);

        if (!CanWear(context.Player, request.Ability) || !CanWear(context.Player, request.Represent))
        {
            session.Send(CreateTitleResponse(current, false));
            return;
        }

        var chosen = current with
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

        // The friends see it in their lists.
        if (world.IsAlive(context.Player))
        {
            await friends.ChangedAsync(context.Player);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonOtherInfoRequest request)
    {
        await others.AskAsync(context.Player, request.Person, OtherInfoWindow.Character, request.CheckOption, request.CalculateStat);
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

    private bool CanWear(Entity player, TitleValue title)
    {
        return titles.CanWear(player, title.Primary, TitleKind.Prefix) && titles.CanWear(player, title.Secondary, TitleKind.Suffix);
    }
}
