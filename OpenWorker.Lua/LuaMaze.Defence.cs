using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.World.Enums;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// The defence mode: a monster the players must keep alive stands in the maze, the players are put in a place the table names,
/// and the spawn boxes of the table send waves at the monster. The mode is won when the last wave is dead and a player stands, and
/// lost when the defended monster falls or no player stands.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// A call for help goes only to monsters within this height of the defended monster.
    /// </summary>
    private const float DefenceCallHeight = 100f;

    /// <summary>
    /// The script starts the defence of the table of that number. A number the server has no row for starts nothing.
    /// </summary>
    [LuaIgnoreMember]
    internal void BeginDefence(int table)
    {
        if (Closed || creatureManager.Services?.Modes is not { } modes || !modes.TryGetDefence(table, out var row))
        {
            Logger.UnknownMode(nameof(BeginDefence), table);

            return;
        }

        var mode = new DefenceMode(this, row);

        Begin(mode);
        mode.Start();

        Notice(ModeNotice.DefenceStarted, 0);
    }

    private sealed class DefenceMode(LuaMaze maze, DefenceModeRow row) : GameMode(maze)
    {
        private Entity? _defended;
        private int _sector;
        private float _passed;
        private float _lastWave;
        private float _call;

        public override int EndNotice => ModeNotice.DefenceEnded;

        /// <summary>
        /// The defended monster is the first monster of the maze that is of the kind the table names. Every player is put in the
        /// box the table names, and the waves of its boxes start.
        /// </summary>
        public void Start()
        {
            foreach (var creature in Maze.Creatures.Living)
            {
                if (Maze.Ecs.IsAlive(creature) && Maze.Ecs.Get<ActorComponent>(creature).Type == ActorType.Monster
                    && Maze.Ecs.Get<CreatureComponent>(creature).Identifier == row.DefendedMonster)
                {
                    _defended = creature;

                    break;
                }
            }

            Gather();

            if (_defended is { } defended)
            {
                Maze.Creatures.Helpers.Defended.Add(defended);
            }

            foreach (var box in row.SpawnBoxes)
            {
                if (box == 0)
                {
                    break;
                }

                var duration = Maze.Waves.Duration(box);

                if (duration <= 0f)
                {
                    continue;
                }

                Maze.Waves.Start(box, false);

                _lastWave = Math.Max(_lastWave, duration);
            }
        }

        /// <summary>
        /// The players come to a place in the box of the table, each to a place of its own in it, facing the way the box does.
        /// </summary>
        private void Gather()
        {
            if (Maze.ExitOf((int)row.StartBox) is not { } box)
            {
                return;
            }

            foreach (var member in Maze._members)
            {
                if (member.Gone || !Maze.Ecs.IsAlive(member.Player) || !Maze.Ecs.Has<WorldComponent>(member.Player))
                {
                    continue;
                }

                var spot = box.GetRandomPosition();
                var position = new Vector3(spot.X, spot.Y, box.PosTopLeft.Z);

                Maze.Ecs.Set(member.Player, Maze.Ecs.Get<WorldComponent>(member.Player) with { Position = position, Rotation = box.Rotation });

                if (member.Loaded)
                {
                    member.Player.Get<ServerSessionComponent>().Send(new WorldWarpResponse
                    {
                        Actor = Maze.Ecs.Get<ActorComponent>(member.Player),
                        Position = position,
                        Rotation = box.Rotation,
                        Result = WorldWrapResult.Ok
                    });
                }

                _sector = Maze.SectorAt(position)?.Id ?? _sector;
            }
        }

        public override void Tick(float seconds)
        {
            _passed += seconds;

            if (_defended is null)
            {
                return;
            }

            _call -= seconds;

            if (_call > 0f)
            {
                return;
            }

            _call += row.HateIntervalMilliseconds * 0.001f;

            CallMonsters();
        }

        /// <summary>
        /// The defended monster draws the monsters around it: each within the reach of the call grows angry at it.
        /// </summary>
        private void CallMonsters()
        {
            if (_defended is not { } defended || !Maze.Ecs.IsAlive(defended))
            {
                return;
            }

            var centre = Maze.Ecs.Get<WorldComponent>(defended).Position;
            var reach = (float)row.HateRange;
            var hate = (Maze.Creatures.Services?.Aggro ?? SkillAggro.None).Hate(0, (int)row.HateValue, 0);

            var callers = Maze.Creatures.FindMonsters((position, _) =>
                Math.Abs(position.Z - centre.Z) <= DefenceCallHeight && Vector2.DistanceSquared(new Vector2(position.X, position.Y), new Vector2(centre.X, centre.Y)) < reach * reach);

            foreach (var monster in callers)
            {
                Maze.Creatures.Provoke(monster, defended, hate);
            }
        }

        public override async ValueTask<bool> CheckEndAsync()
        {
            if (_defended is { } defended && Maze.Creatures.Helpers.Defended.Health(defended) <= 0)
            {
                await EndAsync(2);

                return true;
            }

            var anyoneStands = Maze.AnyoneStands();

            if (!anyoneStands)
            {
                await EndAsync(3);

                return true;
            }

            if (_passed < _lastWave || FoesRemain())
            {
                return false;
            }

            await EndAsync(1);

            return true;
        }

        /// <summary>
        /// Whether a monster that has to die for its sector is alive in the sector of the players.
        /// </summary>
        private bool FoesRemain()
        {
            foreach (var creature in Maze.Creatures.Living)
            {
                if (!Maze.Ecs.IsAlive(creature) || creature == _defended || Maze.Ecs.Get<ActorComponent>(creature).Type != ActorType.Monster)
                {
                    continue;
                }

                if (Maze.Ecs.Get<NonPlayableCreatureComponent>(creature).Sector == _sector
                    && Maze.Ecs.Get<MonsterCombatComponent>(creature).HoldsSector
                    && Maze.Ecs.Get<CreatureComponent>(creature).Health > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async ValueTask EndAsync(byte result)
        {
            Result = result;

            // The sector of the defended monster counts as done whatever came of the defence.
            var held = _defended is { } defended && Maze.Ecs.IsAlive(defended) ? Maze.Ecs.Get<NonPlayableCreatureComponent>(defended).Sector : 0;

            if (held != 0)
            {
                await Maze.CompleteSectorAsync(held);
            }

            if (Maze.Closed)
            {
                return;
            }

            await Maze.State.OnModeEnded(result == 1 ? "OnDefenceModeSuccess" : "OnDefenceModeFail", _sector, Maze);

            if (Maze.Closed)
            {
                return;
            }

            if (result == 1)
            {
                await Maze.ReportModeWonAsync(QuestConditionType.Protect);
            }

            // A monster that fell is gone from the maze.
            else if (_defended is { } fallen && Maze.Ecs.IsAlive(fallen))
            {
                Maze.Creatures.Helpers.Defended.Remove(fallen);
                Maze.Creatures.Remove(fallen);
            }
        }
    }

    /// <summary>
    /// The monsters come in waves at a monster the players must keep alive (<c>LuaMaze.Defence.cs</c>).
    /// </summary>
    [LuaMember]
    public void StartDefenceMode(int table)
    {
        BeginDefence(table);
    }
}
