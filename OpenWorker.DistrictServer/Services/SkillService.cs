using OpenWorker.Channel;
using OpenWorker.Gameplay.Messages.Response.Person;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class SkillService(World world, SkillLearning learning, PersonSkills book) :
    IHotspotHandler<SkillDeckBonusRequest>,
    IHotspotHandler<SkillAddDeckSlotRequest>,
    IHotspotHandler<SkillAkashicRecordRequest>,
    IHotspotHandler<SkillChainRequest>,
    IHotspotHandler<SkillChargingStartRequest>,
    IHotspotHandler<SkillActiveSkillRequest>,
    IHotspotHandler<SkillDivergenceLearnRequest>,
    IHotspotHandler<SkillKeypressRequest>,
    IHotspotHandler<SkillLearnRequest>,
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillPassiveRequest>,
    IHotspotHandler<SkillPreTargetListRequest>,
    IHotspotHandler<SkillProjectileRequest>,
    IHotspotHandler<SkillResetRequest>,
    IHotspotHandler<SkillResetTagetRequest>,
    IHotspotHandler<SkillSubInputRequest>,
    IHotspotHandler<SkillSyncPositionRequest>,
    IHotspotHandler<SkillTargetChangeRequest>,
    IHotspotHandler<SkillTrapPosUpdateRequest>,
    IHotspotHandler<SkillUpdateDeckRequest>,
    IHotspotHandler<SkillWarpPositionRequest>
{
    /// <summary>
    /// The deck has every slot it can have.
    /// </summary>
    private const ushort DeckSlotsFull = 56013;


    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillActiveResponse());
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// One more skill in each position of the deck, up to four. The count the client asks for is not taken: it can only grow
    /// by one.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAddDeckSlotRequest request)
    {
        var count = learning.AddDeckSlot(context.Player);

        if (count == 0)
        {
            SendError(context.Player, SkillOpcode.SkillAddDeckSlot, DeckSlotsFull);

            return ValueTask.CompletedTask;
        }

        world.Get<ServerSessionComponent>(context.Player).Send(new SkillAddDeckSlotResponse { SlotCount = count });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAkashicRecordRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChainRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChargingStartRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDeckBonusRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        world.Set(context.Player, new SkillDeckBonusComponent
        {
            Values = request.Values
        });
        world.MarkDirty(context.Player, PersonSections.Skills);

        session.Send(new SkillDeckBonusResponse
        {
            Values = request.Values,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDivergenceLearnRequest request)
    {
        var refusal = learning.LearnDivergence(context.Player, request.Info.Skill, request.Info.Divergence);

        if (refusal != SkillRefusal.None)
        {
            SendError(context.Player, SkillOpcode.SkillDivergenceLearn, refusal);

            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillDivergenceLearnResponse { Info = request.Info });
        SendPoints(context.Player);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// Learns a skill or its next level, by the rules of <see cref="SkillLearning"/>. A refusal is the error answer with the
    /// code of the original server.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillLearnRequest request)
    {
        var (refusal, replaced) = learning.Learn(context.Player, request.Info.Skill);

        if (refusal != SkillRefusal.None)
        {
            SendError(context.Player, SkillOpcode.SkillLearn, refusal);

            return ValueTask.CompletedTask;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillLearnResponse
        {
            Info = new SkillInfoValue { Skill = request.Info.Skill },
            IsSuccessful = true
        });

        SendPoints(context.Player);

        // A new level takes the place of the old one in the deck.
        if (replaced != 0)
        {
            session.Send(new SkillUpdateDeckResponse { IsSuccessed = true });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var place = request.Skill.Position;

        if (Place.IsValid(place.Position, place.Angle))
        {
            world.Set(context.Player, world.Get<WorldComponent>(context.Player) with { Position = place.Position, Rotation = place.Angle });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request)
    {
        world.Get<ServerSessionComponent>(context.Player).Send(new SkillPassiveResponse());

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPreTargetListRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// Forgets the skills learned: the default skills of the hero stay, every point comes back and the decks empty.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillResetRequest request)
    {
        var person = world.Get<PersonInfoComponent>(context.Player);

        learning.Reset(context.Player, book.DefaultSkills(person.Hero));

        var points = world.Get<SkillPointComponent>(context.Player);
        var deck = world.Get<SkillDeckComponent>(context.Player);

        world.Get<ServerSessionComponent>(context.Player).Send(new SkillResetResponse
        {
            State = new SkillSnapshotValue
            {
                Target = world.Get<ActorComponent>(context.Player),
                SkillPoint = new SkillPointValue
                {
                    TotalSkillPoint = points.TotalSkillPoint,
                    FreeSkillPoint = points.FreeSkillPoint
                },
                DeckSlotCount = deck.SlotCount,
                DeckBonus = world.Get<SkillDeckBonusComponent>(context.Player).Values ?? [],
                SkillList = world.Get<SkillLearnedComponent>(context.Player).Values ?? [],
                DeckSlotList = []
            }
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillResetTagetRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSubInputRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSyncPositionRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTargetChangeRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTrapPosUpdateRequest request) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// Puts learned skills into the positions of the deck; a skill not learned or a place outside the deck changes nothing.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillUpdateDeckRequest request)
    {
        var changed = learning.UpdateDeck(context.Player, request.SlotList ?? []);

        world.Get<ServerSessionComponent>(context.Player).Send(new SkillUpdateDeckResponse { IsSuccessed = changed });

        return ValueTask.CompletedTask;
    }

    private void SendPoints(Entity player)
    {
        var points = world.Get<SkillPointComponent>(player);

        world.Get<ServerSessionComponent>(player).Send(new SkillUpdatePointResponse
        {
            SkillPoint = new SkillPointValue { TotalSkillPoint = points.TotalSkillPoint, FreeSkillPoint = points.FreeSkillPoint }
        });
    }

    private void SendError(Entity player, SkillOpcode command, ushort code)
    {
        world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Skill, command), code));
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request) =>
        ValueTask.CompletedTask;
}
