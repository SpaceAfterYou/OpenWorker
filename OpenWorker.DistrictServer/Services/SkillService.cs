using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class SkillService(World world, ReadOnlyCollection<SkillRow> skills, ReadOnlyCollection<CharacterInfoRow> characterInfo) :
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
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillActiveResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAddDeckSlotRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);
        ref readonly var skillDeck = ref world.Get<SkillDeckComponent>(context.Player);

        world.Set(context.Player, skillDeck with { SlotCount = request.SlotCount });

        session.Send(new SkillAddDeckSlotResponse
        {
            SlotCount = request.SlotCount
        });

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

        session.Send(new SkillDeckBonusResponse
        {
            Values = request.Values,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDivergenceLearnRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);
        ref readonly var learned = ref world.Get<SkillLearnedComponent>(context.Player);

        var index = Array.FindIndex(learned.Values, s => s.Skill == request.Info.Skill);

        if (index == -1)
        {
            return ValueTask.CompletedTask;
        }

        learned.Values[index] = request.Info;

        session.Send(new SkillDivergenceLearnResponse
        {
            Info = request.Info,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillLearnRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);

        ref readonly var skillPoint = ref world.Get<SkillPointComponent>(context.Player);
        ref readonly var skillsCmp = ref world.Get<SkillLearnedComponent>(context.Player);

        var nextSkill = skills.FirstOrDefault(s => s.Id == request.Info.Skill);

        if (nextSkill.Id == 0)
        {
            session.Send(new SkillLearnResponse());
            return ValueTask.CompletedTask;
        }

        if (skillPoint.FreeSkillPoint < nextSkill.Field9)
        {
            session.Send(new SkillLearnResponse());
            return ValueTask.CompletedTask;
        }

        var currentIndex = Array.FindIndex(skillsCmp.Values, s => s.Skill == request.Info.Skill);

        if (currentIndex == -1)
        {
            var freeIndex = Array.FindIndex(skillsCmp.Values, s => s.Skill == 0);

            if (freeIndex == -1)
            {
                session.Send(new SkillLearnResponse());
                return ValueTask.CompletedTask;
            }

            skillsCmp.Values[freeIndex] = request.Info with
            {
                Skill = request.Info.Skill,
            };
        }

        else
        {
            skillsCmp.Values[currentIndex] = request.Info with
            {
                Skill = request.Info.Skill,
            };
        }

        world.Set(context.Player, skillPoint with
        {
            FreeSkillPoint = unchecked((short)(skillPoint.FreeSkillPoint - nextSkill.Field9))
        });

        session.Send(new SkillLearnResponse
        {
            Info = request.Info,
            IsSuccessful = true
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillPassiveResponse
        {
            Error = 0,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPreTargetListRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillResetRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);

        ref readonly var actor = ref world.Get<ActorComponent>(context.Player);
        ref readonly var personInfo = ref world.Get<PersonInfoComponent>(context.Player);

        ref readonly var skillPoint = ref world.Get<SkillPointComponent>(context.Player);
        ref readonly var skillLearned = ref world.Get<SkillLearnedComponent>(context.Player);
        ref readonly var skillDeck = ref world.Get<SkillDeckComponent>(context.Player);
        ref readonly var skillDeckBonus = ref world.Get<SkillDeckBonusComponent>(context.Player);

        var defaultSkills = GetDefaultSkills(personInfo.Hero);

        var spentPoints = skillLearned.Values.Length;
        var newFreePoints = (short)(skillPoint.FreeSkillPoint + spentPoints);

        world.Set(context.Player, skillLearned with { Values = defaultSkills });
        world.Set(context.Player, skillDeck with { SlotList = [] });
        world.Set(context.Player, skillPoint with { FreeSkillPoint = newFreePoints });

        session.Send(new SkillResetResponse
        {
            State = new SkillSnapshotValue
            {
                Target = actor,
                SkillPoint = new SkillPointValue
                {
                    TotalSkillPoint = skillPoint.TotalSkillPoint,
                    FreeSkillPoint = newFreePoints,
                },
                DeckSlotCount = skillDeck.SlotCount,
                DeckBonus = skillDeckBonus.Values,
                SkillList = defaultSkills,
                DeckSlotList = []
            }
        });

        return ValueTask.CompletedTask;
    }

    private SkillInfoValue[] GetDefaultSkills(Hero hero)
    {
        var charInfo = characterInfo.FirstOrDefault(c => c.Character == (byte)hero);
        if (charInfo.Id == 0)
            return [];

        return charInfo
            .GetSkillList()
            .Where(id => id > 0)
            .Select(id => new SkillInfoValue { Skill = id, Divergence = 0 })
            .ToArray();
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

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillUpdateDeckRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);

        ref readonly var skillDeck = ref world.Get<SkillDeckComponent>(context.Player);
        ref readonly var skillLearned = ref world.Get<SkillLearnedComponent>(context.Player);

        foreach (var deck in request.SlotList)
        {
            foreach (var skillId in deck.GetList().Where(e => e != 0))
            {
                if (!skillLearned.Values.Any(s => s.Skill == skillId))
                {
                    session.Send(new SkillUpdateDeckResponse());
                    return ValueTask.CompletedTask;
                }
            }
        }

        var updatedDecks = skillDeck.SlotList.ToList();
        foreach (var newDeck in request.SlotList)
        {
            var existingIndex = updatedDecks.FindIndex(d => d.Position == newDeck.Position);
            if (existingIndex >= 0)
            {
                updatedDecks[existingIndex] = newDeck;
            }
            else
            {
                updatedDecks.Add(newDeck);
            }
        }

        world.Set(context.Player, skillDeck with { SlotList = updatedDecks.ToArray() });

        session.Send(new SkillUpdateDeckResponse { IsSuccessed = true });
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request) =>
        ValueTask.CompletedTask;
}
