using Arch.Core;
using Lua;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Gameplay.Messages.Response.Person;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// What the script has stored with <c>SetLuaValue</c>: numbers that stay for as long as the maze does.
    /// </summary>
    private Dictionary<int, int> LuaValues { get; } = [];

    private List<string> LuaKeys { get; } = [];

    [LuaMember]
    public void AddLuaValue(int index, int value)
    {
        LuaValues[index] = LuaValues.GetValueOrDefault(index) + value;
    }

    /// <summary>
    /// A message to one player, or to everyone when the number is zero.
    /// </summary>
    [LuaMember]
    public void ChatMessage(int user, string message)
    {
        var notice = new ChatNoticeResponse { Type = 0, Message = message, Color = "" };

        if (user == 0)
        {
            creatureManager.Broadcast(notice);

            return;
        }

        foreach (var member in _members)
        {
            if (TrySession(member.Player, out var session) && user == (int)Ecs.Get<ActorComponent>(member.Player))
            {
                session.Send(notice);
            }
        }
    }

    [LuaMember]
    public VVector3 GetCommonPositionBox(int box)
    {
        Logger.CallGetCommonPositionBox(box);

        // A script that names a box the batch does not have keeps running, with the origin.
        if (Batch.EventBox.CommonPositions.Find(box) is not { } position)
        {
            Logger.UnknownPositionBox(box);

            return new VVector3();
        }

        return position.GetRandomPosition();
    }

    [LuaMember]
    public int GetLuaValue(int index)
    {
        return LuaValues.GetValueOrDefault(index);
    }

    /// <summary>
    /// What the script told the clients in a sector, kept for a player whose client loads the maze later, until its time is up.
    /// </summary>
    private readonly Dictionary<int, List<(int Type, int Value, TimeSpan Until)>> _sectorSyncs = [];

    /// <summary>
    /// The client runs a function of its own script when it hears this. A sync that names a sector of the map is kept
    /// there, for good or for the seconds given.
    /// </summary>
    [LuaMember]
    public void LuaClientSync(int type, int value, int sector = 0, float time = 0)
    {
        creatureManager.Broadcast(new MazeLuaClientSyncResponse { Type = type, Value = value });

        if (sector <= 0 || Batch.EventBox.Sectors.Find(sector) is null)
        {
            return;
        }

        if (!_sectorSyncs.TryGetValue(sector, out var kept))
        {
            kept = [];
            _sectorSyncs[sector] = kept;
        }

        var now = Clock.Time;

        kept.RemoveAll(e => e.Until <= now);
        kept.Add((type, value, time > 0 ? now + Wait(time) : TimeSpan.MaxValue));
    }

    /// <summary>
    /// A player whose client has loaded the maze hears again what the script told the clients in the sector it stands in.
    /// </summary>
    [LuaIgnoreMember]
    public void ShowClientSyncsTo(Entity player)
    {
        if (!TrySession(player, out var session) || !Ecs.TryGet(player, out WorldComponent place) ||
            SectorAt(place.Position) is not { } sector || !_sectorSyncs.TryGetValue(sector.Id, out var kept))
        {
            return;
        }

        var now = Clock.Time;

        foreach (var (type, value, until) in kept)
        {
            if (until > now)
            {
                session.Send(new MazeLuaClientSyncResponse { Type = type, Value = value });
            }
        }
    }

    /// <summary>
    /// A key stays in the maze. A number goes under an index: <c>SetLuaValue(index, value)</c>.
    /// </summary>
    [LuaMember("SetLuaValue")]
    public void SetLuaValue(LuaValue keyOrIndex, LuaValue value)
    {
        if (keyOrIndex.TryRead<string>(out var key))
        {
            LuaKeys.Add(key);
            return;
        }

        // A script that gives no number under no index stores nothing; a value that is no number is zero, as in the scripts
        // that compare what they stored with numbers.
        if (!keyOrIndex.TryRead<int>(out var index))
        {
            Logger.UnreadableLuaValue();
            return;
        }

        LuaValues[index] = value.TryRead<int>(out var number) ? number : 0;
    }

    /// <summary>
    /// Lua-CSharp is not supports enum as parameter right now
    /// </summary>
    [LuaMember]
    public void SetMazeState(int state)
    {
        if (Enum.IsDefined(typeof(MazeState), state))
        {
            SetMazeState((MazeState)state);
        }
        else
        {
            Logger.UnknownMazeState(state);
        }
    }

    /// <summary>
    /// The state only tells scripts what the maze is doing. The client does not read it, and a maze waiting
    /// to die goes on by itself.
    /// </summary>
    [LuaIgnoreMember]
    public void SetMazeState(MazeState state)
    {
        Logger.CallSetMazeState(state);

        MazeState = state;

        if (state == MazeState.WaitToDie)
        {
            Later(0f, () =>
            {
                if (MazeState == MazeState.WaitToDie)
                {
                    MazeState = MazeState.Run;
                }

                return ValueTask.CompletedTask;
            });
        }
    }
}
