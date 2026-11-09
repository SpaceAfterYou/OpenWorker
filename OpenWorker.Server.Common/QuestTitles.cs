using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Server.Common;

/// <summary>
/// The titles an episode rewards go to the titles of the person, which keeps them and tells the client.
/// </summary>
public sealed class QuestTitles(TitleBook titles) : IQuestTitles
{
    public async ValueTask GrantAsync(Entity player, int title)
    {
        await titles.GrantAsync(player, title, TitleSource.Quest);
    }
}
