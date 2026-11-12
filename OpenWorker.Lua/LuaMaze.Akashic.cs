using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// A blow of a card a player has called into the fight: it is the player's blow, shown as the card's, from where the card
/// stands and the way it faces.
/// </summary>
internal readonly record struct CardStrike(ActorValue Actor, Vector3 Place, float Yaw);

public partial class LuaMaze
{
    /// <summary>
    /// The akashic skill of a player with a card called: at the moment the hero's table names, the card comes in where the
    /// akashic file of the heroes puts it, everybody in the maze sees it come (06/7B), and it plays its own skill, whose blows
    /// are the player's, shown as the card's. A new skill of the player before the card comes, a fall or leaving the maze stops
    /// it. False when the skill is not the akashic one or no card is called: the skill plays as any other.
    /// </summary>
    private bool BringsCardIn(Fighter member, int skill, int generation)
    {
        if (creatureManager.Services?.Akashic is not { } calls || !calls.IsAkashicSkill(skill) ||
            !calls.TryTake(member.Player, out var card, out var yaw))
        {
            return false;
        }

        var delay = Wait(calls.SummonDelay(member.Player));

        foreach (var summon in actions.GetAkashicSummons(skill, card.ActionGroup))
        {
            _schedule.Schedule(delay, () => Summon(member, card, yaw, summon, generation));
        }

        return true;
    }

    private void Summon(Fighter member, AkashicRecordsRow card, float yaw, AkashicSummon summon, int generation)
    {
        if (Closed || member.Gone || member.SkillGeneration != generation || !creatureManager.Vitals.IsAlive(member.Player))
        {
            return;
        }

        var owner = Ecs.Get<WorldComponent>(member.Player);
        var (sin, cos) = MathF.SinCos(yaw * MathF.PI / 180f);
        var offset = summon.Offset;
        var place = owner.Position + new Vector3(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos, offset.Z);
        var actor = new ActorValue(creatureManager.NextActor(), ActorType.Akashic);

        // The clients put the card where it stands themselves, from its owner and the offset of the file.
        creatureManager.Broadcast(new SkillSummonAkashicResponse
        {
            Owner = Ecs.Get<ActorComponent>(member.Player),
            Creature = actor,
            AlphaValuea = summon.Alpha,
            BlendingTime = summon.Blend,
            Position = summon.Offset,
            ApplyRotation = summon.Rotate
        });

        // The card plays its skill from its own action file; the column of the card holds that skill.
        var strike = new CardStrike(actor, place, yaw);
        var position = default(SkillPositionValue) with { Position = place, Angle = yaw };

        foreach (var hit in actions.GetHits(card.Buff, card.CodeName))
        {
            _schedule.Schedule(Wait(hit.Time), () => Strike(member, card.Buff, hit.Trigger, strike, position));
        }
    }

    /// <summary>
    /// One blow of a card. A card whose player has fallen or left strikes no more.
    /// </summary>
    private void Strike(Fighter member, int skill, AttackTrigger trigger, CardStrike strike, SkillPositionValue position)
    {
        if (Closed || member.Gone || !creatureManager.Vitals.IsAlive(member.Player))
        {
            return;
        }

        var area = new AttackArea(trigger, strike.Place, strike.Yaw);
        var targets = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

        Run(ApplyDamageAsync(member, skill, trigger, targets, position, origin: strike.Place, card: strike));
    }
}
