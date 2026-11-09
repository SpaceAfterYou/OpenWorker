using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;

namespace OpenWorker.GateServer.Gameplay;

/// <summary>
/// The selection of a character that has not played the prologue: instead of its district, the character goes to the first
/// tutorial maze, and the gate answers with <see cref="PersonEnterPrologueResponse"/>. The client connects to the maze like it
/// connects to a district; the maze clears the flag when the character leaves the tutorial.
/// </summary>
public sealed partial class PrologueEntry(
    World world,
    IOptions<GameOptions> game,
    IOptions<GateOptions> gate,
    IOptions<MazeEndpointOptions> mazeEndpoint,
    IPrologueStore store,
    BatchManager batches,
    ReadOnlyCollection<CharacterStartRow> starts,
    MazeDirectory mazes,
    IRedisCollection<MazeReserveCache> reservations,
    ILogger<PrologueEntry> logger)
{
    /// <summary>
    /// The first tutorial maze, for a hero the server's table names no maze for.
    /// </summary>
    public const short FirstTutorialMaze = 21111;

    private readonly RowIndex<ushort, CharacterStartRow> _starts = starts.IndexBy(e => e.Id);

    private short Gate { get; } = gate.Value.Id;

    /// <summary>
    /// Sends the character into the prologue when the server plays it and the character has it ahead, and says whether it did.
    /// False leaves the selection to the district.
    /// </summary>
    public async ValueTask<bool> TryEnterAsync(Entity player, int person)
    {
        if (!await IsAheadAsync(player, person) || !world.IsAlive(player) || !TryGetHero(player, person, out var hero))
        {
            return false;
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var (location, box) = MazeOf(hero);

        if (!batches.TryGetAndCache(location, out var batch, out var type) || type != BatchType.Maze || !TryGetStart(batch, location, box, out var start))
        {
            LogNoTutorialMaze(location, person);

            return false;
        }

        var maze = await mazes.PickAsync(Gate) ?? (mazeEndpoint.Value.Host, (short)mazeEndpoint.Value.Port);

        // The player may have left while the servers were looked up: the selection is over either way.
        if (!world.IsAlive(player))
        {
            return true;
        }

        var map = new MapValue { Location = location, Server = Gate };
        var position = start.GetPosition();

        await reservations.InsertAsync(new MazeReserveCache
        {
            Account = account,
            Person = person,
            Location = location,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            R = start.Rotation,
            Jump = start.Id
        }, WorldManager.ReservationLifetime);

        if (!world.IsAlive(player))
        {
            return true;
        }

        LogPrologue(person, location);

        world.Get<ServerSessionComponent>(player).Send(new PersonEnterPrologueResponse
        {
            Zone = new EnterMapResultValue
            {
                Zone = new ZoneValue
                {
                    Actor = person,
                    Account = account,
                    Jump = start.Id,
                    Map = map,
                    Address = maze.Host,
                    Port = maze.Port,
                    World = new WorldValue { Location = location, Position = position, Rotation = start.Rotation, Map = map },
                    Type = EnterMapType.Maze
                },
                ChangeServer = true,
                ChangeType = ChangeServerType.EnterMap
            }
        });

        return true;
    }

    /// <summary>
    /// Whether the selected character goes to the prologue: the server plays it, the account lists the character on this
    /// gate, and the character has not played it.
    /// </summary>
    public async ValueTask<bool> IsAheadAsync(Entity player, int person)
    {
        if (!game.Value.Prologue || !TryGetHero(player, person, out _))
        {
            return false;
        }

        return await store.IsPendingAsync(person, world.Get<ClaimsComponent>(player).Account, Gate);
    }

    /// <summary>
    /// The maze and the start box of the prologue of a hero: the server's own table names them, and the first tutorial
    /// maze with its default start stands for a hero it does not.
    /// </summary>
    private (short Maze, int Box) MazeOf(Domain.Enums.Hero hero)
    {
        var row = (int)hero * 1000 is var id and >= 0 and <= ushort.MaxValue ? _starts.GetOrDefault((ushort)id) : default;

        return row.Maze > 0 ? (row.Maze, row.MazeStart) : (FirstTutorialMaze, 0);
    }

    /// <summary>
    /// The start box the table names, or the default one of the maze when the map has no such box.
    /// </summary>
    private static bool TryGetStart(VBatchFile batch, short location, int box, [NotNullWhen(true)] out StartEventBox? start)
    {
        var events = batch.EventBox.StartEvents;

        return (box != 0 && events.TryGet(box, out start)) || events.TryGet(location * 100 + 1, out start);
    }

    /// <summary>
    /// The hero of a character the account lists on this gate; a character it does not list is left to the selection,
    /// which refuses it.
    /// </summary>
    private bool TryGetHero(Entity player, int person, out Domain.Enums.Hero hero)
    {
        hero = default;

        if (!world.TryGet<GatePersonComponent>(player, out var persons) || persons.SlotList is null)
        {
            return false;
        }

        foreach (var slot in persons.SlotList)
        {
            if (slot != Entity.Null && world.IsAlive(slot) && world.Get<ActorComponent>(slot).Identifier == person)
            {
                hero = world.Get<PersonInfoComponent>(slot).Hero;
                return true;
            }
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Person {Person} has the prologue ahead and goes to the tutorial maze {Maze}")]
    private partial void LogPrologue(int person, short maze);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The tutorial maze {Maze} has no map with a start box, person {Person} goes to its district without the prologue")]
    private partial void LogNoTutorialMaze(short maze, int person);
}
