using System.Collections.ObjectModel;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

/// Central quest progress plus network sync. State lives in <see cref="QuestProgressComponent"/> on the player entity
/// (up to <see cref="OpenWorker.Domain.Components.QuestProgressDefines.MaxActiveEpisodes"/> simultaneous episodes).
public sealed class QuestManager(
    World world,
    ReadOnlyCollection<QuestEpisodeRow> questEpisodes,
    ReadOnlyCollection<QuestConditionRow> questConditions)
{
    private readonly Dictionary<int, QuestEpisodeRow> _episodeById = BuildEpisodeIndex(questEpisodes);

    public void EnsureInitialized(Entity player)
    {
        if (player.Has<QuestProgressComponent>())
        {
            return;
        }

        if (questEpisodes.Count == 0)
        {
            world.Add(player, default(QuestProgressComponent));
            return;
        }

        var row = questEpisodes[0];
        var state = new ActiveQuestEpisodeState
        {
            EpisodeId = row.Id,
            IsAddHelper = 0,
            IsFailed = 0,
            Steps = default
        };

        world.Add(player, new QuestProgressComponent { ActiveCount = 1, E0 = state });
    }

    public QuestListResponse BuildQuestListResponse(Entity player)
    {
        EnsureInitialized(player);

        var q = world.Get<QuestProgressComponent>(player);
        var list = new List<QuestEpisodeEntry>(q.ActiveCount);

        for (var i = 0; i < q.ActiveCount; i++)
        {
            var slot = GetSlotCopy(in q, i);
            if (!_episodeById.TryGetValue(slot.EpisodeId, out var row))
            {
                continue;
            }

            list.Add(ToEpisodeEntry(in slot, row));
        }

        return new QuestListResponse { List = list };
    }

    /// Applies progress across every active episode that references <paramref name="conditionId"/>,
    /// sends one <see cref="QuestUpdateResponse"/>; returns false if the condition id is unknown or step invalid.
    public bool TryApplyConditionProgress(Entity player, int conditionId, byte step)
    {
        EnsureInitialized(player);

        if (!TryGetMaxStep(conditionId, out var maxStep) || step > maxStep)
        {
            return false;
        }

        var q = world.Get<QuestProgressComponent>(player);
        var applied = false;

        for (var ei = 0; ei < q.ActiveCount; ei++)
        {
            var slot = GetSlotCopy(in q, ei);
            if (!_episodeById.TryGetValue(slot.EpisodeId, out var row))
            {
                continue;
            }

            var touched = false;
            for (var ci = 0; ci < QuestModuleDefines.ConditionsPerEpisode; ci++)
            {
                if (row.GetConditionId(ci) != conditionId)
                {
                    continue;
                }

                slot.Steps.Set(ci, step);
                touched = true;
            }

            if (touched)
            {
                SetSlot(ref q, ei, slot);
                applied = true;
            }
        }

        if (!applied)
        {
            return false;
        }

        world.Set(player, q);

        var session = world.Get<ServerSessionComponent>(player);
        session.Send(new QuestUpdateResponse
        {
            ConditionList =
            [
                new QuestCondition
                {
                    Condition = conditionId,
                    Step = step
                }
            ]
        });

        return true;
    }

    public void AcceptEpisode(Entity player, int episodeId)
    {
        EnsureInitialized(player);

        if (!_episodeById.ContainsKey(episodeId))
        {
            return;
        }

        var q = world.Get<QuestProgressComponent>(player);
        if (FindEpisodeIndex(in q, episodeId) >= 0)
        {
            return;
        }

        if (q.ActiveCount >= QuestProgressDefines.MaxActiveEpisodes)
        {
            return;
        }

        var slot = new ActiveQuestEpisodeState
        {
            EpisodeId = episodeId,
            IsAddHelper = 0,
            IsFailed = 0,
            Steps = default
        };

        SetSlot(ref q, q.ActiveCount, slot);
        q.ActiveCount++;
        world.Set(player, q);

        var session = world.Get<ServerSessionComponent>(player);
        session.Send(new QuestAcceptResponse { Episode = episodeId, IsShowHelper = 1 });
    }

    public void CompleteEpisode(Entity player, int episodeId)
    {
        EnsureInitialized(player);

        if (!world.Has<QuestProgressComponent>(player))
        {
            return;
        }

        var q = world.Get<QuestProgressComponent>(player);
        var idx = FindEpisodeIndex(in q, episodeId);
        if (idx < 0)
        {
            return;
        }

        RemoveEpisodeAt(ref q, idx);
        world.Set(player, q);

        var session = world.Get<ServerSessionComponent>(player);
        session.Send(new QuestEpisodeCompleteResponse
        {
            Episode = episodeId,
            Exp = 0,
            Money = 0,
            BattlePoints = 0,
            Ether = 0,
            Title = new TitleValue(0, 0),
            NpcHelper = 0u,
            Item = default
        });
    }

    public void GiveUpEpisode(Entity player, int episodeId)
    {
        if (!world.Has<QuestProgressComponent>(player))
        {
            return;
        }

        var q = world.Get<QuestProgressComponent>(player);
        var idx = FindEpisodeIndex(in q, episodeId);
        if (idx < 0)
        {
            return;
        }

        RemoveEpisodeAt(ref q, idx);
        world.Set(player, q);

        world.Get<ServerSessionComponent>(player).Send(new QuestGiveUpResponse { Episode = episodeId });
    }

    public void FailEpisode(Entity player, int episodeId)
    {
        EnsureInitialized(player);

        var q = world.Get<QuestProgressComponent>(player);
        var idx = FindEpisodeIndex(in q, episodeId);
        if (idx < 0)
        {
            return;
        }

        var slot = GetSlotCopy(in q, idx);
        slot.IsFailed = 1;
        SetSlot(ref q, idx, slot);
        world.Set(player, q);

        world.Get<ServerSessionComponent>(player).Send(new QuestFailResponse { Episode = episodeId });
    }

    public void SetHelper(Entity player, QuestHelperType type, int episodeId)
    {
        EnsureInitialized(player);

        var q = world.Get<QuestProgressComponent>(player);
        var idx = FindEpisodeIndex(in q, episodeId);
        if (idx < 0)
        {
            return;
        }

        var slot = GetSlotCopy(in q, idx);
        slot.IsAddHelper = type == QuestHelperType.Add ? (byte)1 : (byte)0;
        SetSlot(ref q, idx, slot);
        world.Set(player, q);

        world.Get<ServerSessionComponent>(player).Send(new QuestHelperResponse { Type = type, Episode = episodeId });
    }

    public void AckEventUpdate(Entity player)
    {
        if (!world.Has<QuestProgressComponent>(player))
        {
            return;
        }

        world.Get<ServerSessionComponent>(player).Send(new QuestEventUpdateResponse());
    }

    public bool HasAnyActiveEpisode(Entity player)
    {
        return world.Has<QuestProgressComponent>(player) && world.Get<QuestProgressComponent>(player).ActiveCount > 0;
    }

    public bool HasActiveEpisode(Entity player, int episodeId)
    {
        EnsureInitialized(player);

        if (!world.Has<QuestProgressComponent>(player))
        {
            return false;
        }

        var q = world.Get<QuestProgressComponent>(player);
        return FindEpisodeIndex(in q, episodeId) >= 0;
    }

    public bool ActiveEpisodeUsesCondition(Entity player, int conditionId)
    {
        EnsureInitialized(player);

        if (conditionId == 0 || !world.Has<QuestProgressComponent>(player))
        {
            return false;
        }

        var q = world.Get<QuestProgressComponent>(player);
        for (var ei = 0; ei < q.ActiveCount; ei++)
        {
            var slot = GetSlotCopy(in q, ei);
            if (!_episodeById.TryGetValue(slot.EpisodeId, out var row))
            {
                continue;
            }

            for (var ci = 0; ci < QuestModuleDefines.ConditionsPerEpisode; ci++)
            {
                if (row.GetConditionId(ci) == conditionId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void RemoveAllActiveEpisodes(Entity player)
    {
        EnsureInitialized(player);

        if (!world.Has<QuestProgressComponent>(player))
        {
            return;
        }

        world.Set(player, default(QuestProgressComponent));

        SendQuestEpisodeList(player);
    }

    private QuestEpisodeEntry ToEpisodeEntry(in ActiveQuestEpisodeState slot, QuestEpisodeRow row)
    {
        var arr = new QuestCondition[QuestModuleDefines.ConditionsPerEpisode];
        for (var i = 0; i < QuestModuleDefines.ConditionsPerEpisode; i++)
        {
            arr[i] = new QuestCondition
            {
                Condition = row.GetConditionId(i),
                Step = slot.Steps.Get(i)
            };
        }

        return new QuestEpisodeEntry
        {
            Episode = slot.EpisodeId,
            Info = new QuestInfoEntry
            {
                IsAddHelper = slot.IsAddHelper != 0,
                CompleteBit = ComputeCompleteBitMask(row, in slot.Steps),
                IsFailed = slot.IsFailed != 0,
                Condition = arr
            }
        };
    }

    private short ComputeCompleteBitMask(QuestEpisodeRow row, in QuestConditionStepBlock steps)
    {
        short mask = 0;

        for (var i = 0; i < QuestModuleDefines.ConditionsPerEpisode; i++)
        {
            var cid = row.GetConditionId(i);
            if (cid == 0)
            {
                continue;
            }

            if (!TryGetMaxStep(cid, out var max))
            {
                continue;
            }

            if (steps.Get(i) >= max)
            {
                mask |= (short)(1 << i);
            }
        }

        return mask;
    }

    private bool TryGetMaxStep(int conditionId, out byte maxStep)
    {
        foreach (var row in questConditions)
        {
            if (row.Id == conditionId)
            {
                maxStep = row.Field21;
                return true;
            }
        }

        maxStep = 0;
        return false;
    }

    private static int FindEpisodeIndex(in QuestProgressComponent q, int episodeId)
    {
        for (var i = 0; i < q.ActiveCount; i++)
        {
            if (GetSlotCopy(in q, i).EpisodeId == episodeId)
            {
                return i;
            }
        }

        return -1;
    }

    private static void RemoveEpisodeAt(ref QuestProgressComponent q, int index)
    {
        for (var i = index; i < q.ActiveCount - 1; i++)
        {
            SetSlot(ref q, i, GetSlotCopy(in q, i + 1));
        }

        q.ActiveCount--;
    }

    private static ActiveQuestEpisodeState GetSlotCopy(in QuestProgressComponent q, int index) => index switch
    {
        0 => q.E0,
        1 => q.E1,
        2 => q.E2,
        3 => q.E3,
        4 => q.E4,
        5 => q.E5,
        6 => q.E6,
        7 => q.E7,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    private static void SetSlot(ref QuestProgressComponent q, int index, ActiveQuestEpisodeState value)
    {
        switch (index)
        {
            case 0: q.E0 = value; break;
            case 1: q.E1 = value; break;
            case 2: q.E2 = value; break;
            case 3: q.E3 = value; break;
            case 4: q.E4 = value; break;
            case 5: q.E5 = value; break;
            case 6: q.E6 = value; break;
            case 7: q.E7 = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private static Dictionary<int, QuestEpisodeRow> BuildEpisodeIndex(ReadOnlyCollection<QuestEpisodeRow> episodes)
    {
        var d = new Dictionary<int, QuestEpisodeRow>();
        foreach (var e in episodes)
        {
            d[e.Id] = e;
        }

        return d;
    }

    public void SendQuestEpisodeList(Entity player)
    {
        ref readonly var session = ref player.Get<ServerSessionComponent>();
        session.Send(BuildQuestListResponse(player));
    }
}
