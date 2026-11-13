using Lua;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Lua.Enums;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// The script hears what happens to the quests of the player (<c>OnUpdateQuest</c>): the episodes taken and finished
    /// and the conditions done, from whichever part of the game they come. A script without the function is not told.
    /// </summary>
    [LuaIgnoreMember]
    public void FollowQuests(QuestCatalog catalog)
    {
        if (!State.Environment["OnUpdateQuest"].TryRead<LuaFunction>(out _))
        {
            return;
        }

        _questCatalog = catalog;

        foreach (var member in _members)
        {
            member.Quests ??= new QuestWatch(Ecs, member.Player, catalog);
        }
    }

    /// <summary>
    /// The quests of every member are followed, and each change names the member it is of.
    /// </summary>
    private QuestCatalog? _questCatalog;

    private void StepQuests()
    {
        if (_questCatalog is null || !Started)
        {
            return;
        }

        foreach (var member in _members)
        {
            if (member.Quests is not { } quests || !Ecs.IsAlive(member.Player))
            {
                continue;
            }

            var changes = quests.Look();

            if (changes.Count > 0)
            {
                Run(TellQuestsAsync((int)Ecs.Get<ActorComponent>(member.Player).Identifier, changes.ToArray()));
            }
        }
    }

    private async ValueTask TellQuestsAsync(int user, (QuestUpdateType Type, int Identifier)[] changes)
    {
        foreach (var (type, identifier) in changes)
        {
            if (Closed)
            {
                return;
            }

            await State.OnUpdateQuest(user, type, identifier, this);
        }
    }

    [LuaMember]
    public void AcceptQuest(int episode)
    {
        foreach (var member in _members)
        {
            questManager.AcceptEpisode(member.Player, episode);
        }
    }

    [LuaMember]
    public void CompleteCondition(int condition)
    {
        foreach (var member in _members)
        {
            questManager.CompleteCondition(member.Player, condition);
        }
    }

    [LuaMember]
    public void CompleteQuest(int episode)
    {
        foreach (var member in _members)
        {
            questManager.CompleteEpisode(member.Player, episode);
        }
    }

    /// <summary>
    /// A quest of the party. There is no party in a maze of one.
    /// </summary>
    [LuaMember]
    public void InitPartyQuest(int quest, int condition, int sector)
    {
        NotDone("InitPartyQuest");
        Logger.IgnoredInitPartyQuest(quest, condition, sector);
    }

    /// <summary>
    /// Whether anybody in the maze has a quest that counts the condition.
    /// </summary>
    [LuaMember]
    public bool IsHaveCondition(int conditionId)
    {
        return _members.Exists(e => questManager.ActiveEpisodeUsesCondition(e.Player, conditionId));
    }

    /// <summary>
    /// Whether anybody in the maze has the quest.
    /// </summary>
    [LuaMember]
    public bool IsHaveQuest(int episodeId)
    {
        return _members.Exists(e => questManager.HasActiveEpisode(e.Player, episodeId));
    }

    [LuaMember]
    public void RemoveQuestAll()
    {
        foreach (var member in _members)
        {
            questManager.RemoveAllActiveEpisodes(member.Player);
        }
    }
}
