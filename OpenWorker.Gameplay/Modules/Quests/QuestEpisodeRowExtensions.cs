using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

internal static class QuestEpisodeRowExtensions
{
    public static int GetConditionId(this QuestEpisodeRow row, int slotIndex) => slotIndex switch
    {
        0 => row.Field96,
        1 => row.Field97,
        2 => row.Field98,
        3 => row.Field99,
        4 => row.Field100,
        5 => row.Field101,
        6 => row.Field102,
        7 => row.Field103,
        8 => row.Field104,
        9 => row.Field105,
        _ => 0
    };
}
