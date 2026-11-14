using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The character has the prologue ahead: it came to the tutorial from the gate and has not left it yet.
/// </summary>
public readonly record struct PrologueComponent;

/// <summary>
/// The end of the prologue. A character that has it ahead plays the tutorial mazes; when it leaves one of them by its way
/// out (at the end of the last one, or by skipping the cutscenes), the quests of the tutorial give way to the one quest
/// that stands for the whole prologue, finished, and the flag of the character is cleared, once. The way out then takes it
/// to its district like any maze.
/// </summary>
public sealed partial class PrologueService(
    World world,
    IPrologueStore store,
    QuestManager quests,
    IPersonFlush persons,
    ReadOnlyCollection<MazeInfoRow> mazes,
    PrologueQuests prologueQuests,
    ILogger<PrologueService> logger)
{
    /// <summary>
    /// The type of the maze table that marks a tutorial maze.
    /// </summary>
    public const byte TutorialMaze = 1;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    /// <summary>
    /// The character enters a maze: whether it still has the prologue ahead is read from its row.
    /// </summary>
    public void Enter(Entity player, PersonPersistent person)
    {
        if (person.ProloguePending && !world.Has<PrologueComponent>(player))
        {
            world.Add(player, new PrologueComponent());
        }
    }

    /// <summary>
    /// The character takes the way out of its maze. Out of a tutorial maze with the prologue ahead, the prologue is over.
    /// </summary>
    public async ValueTask LeaveAsync(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<PrologueComponent>(player) || !IsTutorial(world.Get<WorldComponent>(player).Location))
        {
            return;
        }

        // Taken first, so a second request to leave while this one waits does not finish the prologue again.
        world.Remove<PrologueComponent>(player);

        var person = (int)world.Get<ActorComponent>(player).Identifier;
        var skip = prologueQuests.Of(world.Get<PersonInfoComponent>(player).Hero);

        // The quests of the tutorial go, and the quest of the whole prologue is taken and finished, so the story goes on
        // from it; what it gives is given now.
        quests.RemoveAllActiveEpisodes(player);

        if (skip != 0)
        {
            quests.AcceptEpisode(player, skip);
            quests.CompleteEpisode(player, skip);
        }

        // The quests are written before the flag: a character whose flag is cleared has its prologue quest too.
        await persons.FlushAsync(player);

        if (await store.FinishAsync(person))
        {
            LogFinished(person, skip);
        }
    }

    /// <summary>
    /// Whether the location is a tutorial maze.
    /// </summary>
    public bool IsTutorial(short location)
    {
        return _mazes.TryGet(location, out var maze) && maze.Type == TutorialMaze;
    }

    /// <summary>
    /// The quest the person enters a maze with finished: the one of the prologue once it is behind it.
    /// </summary>
    public int QuestBehind(PersonPersistent person)
    {
        return prologueQuests.Behind(person.Hero, person.ProloguePending);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Person {Person} has played the prologue, quest {Quest} stands for it")]
    private partial void LogFinished(int person, int quest);
}
