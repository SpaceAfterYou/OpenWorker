using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The quest that stands for the whole prologue of each hero, from the server's own start table (the row of
/// <c>1000 * hero</c>): leaving the tutorial finishes it, and the story goes on from it.
/// </summary>
public sealed class PrologueQuests(ReadOnlyCollection<CharacterStartRow> starts)
{
    private readonly RowIndex<ushort, CharacterStartRow> _starts = starts.IndexBy(e => e.Id);

    /// <summary>
    /// The quest of the prologue of the hero, or 0 when the table has none.
    /// </summary>
    public int Of(Hero hero)
    {
        return (int)hero * 1000 is var id and >= 0 and <= ushort.MaxValue ? _starts.GetOrDefault((ushort)id).SkipQuest : 0;
    }

    /// <summary>
    /// The quest a person enters with finished: the one of its hero once the prologue is behind it, none while it is ahead.
    /// </summary>
    public int Behind(Hero hero, bool prologueAhead)
    {
        return prologueAhead ? 0 : Of(hero);
    }
}
