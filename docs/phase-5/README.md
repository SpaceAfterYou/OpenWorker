# Phase 5: the simulation of the mazes

Phase 5 of `REFACTORING.md` ("Масштабирование симуляции"): the mazes wait on the simulation clock instead of timers of
their own, step without copies, find the boxes of their map by id, survive their scripts, enter faster, and the way to
more capacity is more maze processes. Branch `refactor/phase-5`, 2026-11-09.

## What was already done when the phase started

The merges of 2026-11-08 ("carry a maze to its end, run the timers of its scripts and keep skills going while their key is
held", the AI of monsters on the navigation mesh, the items) had already moved part of section 2.2 of the plan:

| Item of 2.2 / step | State at the start |
|---|---|
| 2.2.1, step 1: timers through `Task.Delay` | **Not done.** `SimulationClock` existed and stepped the monsters (`LuaCreatureManager.Advance`), and the projectiles of monsters flew in that step (`MonsterBody.Fly`). Everything else waited on `Task.Delay`: `LuaMaze.RunLaterAsync` (8 callers: `SectorClear`, `TerminateSpawn`, `TerminateSpawnBox`, `SetSectorStepStop`, `AllDestroySectorMonster`, `ExecuteDestroy`, the end of `WaitToDie`, `SetNpcRotation`), the playback of a skill (`PlayAsync`, woken by a `CancellationTokenSource` when the key changed), the repeated hits of a trap, the flight of a projectile of the player (`FlyAsync`), a chain (`ChainAsync`), the timers of the script (`RunTimersAsync`, a 50 ms loop around the new `MazeTimers`) and the creatures that appear later (`ShowLaterAsync`). `Close()` cleared `MazeTimers` and set `Closed`; the tasks only checked the flag when they woke. |
| 2.2.2, step 3: linear searches | **Half.** `First(...)` that threw on a missing box had become `FirstOrDefault` with a log line (`MazeRequestGuardTest`), so a client could no longer throw on the loop with a box id; the searches were still linear, 24 of them in `MazeService`, `LuaMaze`, `LuaCreatureManager` and `WorldManager.TryGetStart`, and a missing box was ignored, not answered. |
| 2.2.3: `Get<LuaMaze>` without `Has` | **Done** (`MazeService.TryGetMaze`, `MazeRequestGuardTest`). |
| 2.2.4, step 4: scripts throw | **Half.** No `[LuaMember]` threw `NotImplementedException` any more: each was written or logged as ignored at `Debug` on every call, and `Console.WriteLine` had gone (`LuaLog`). An error inside a `LuaState` call still went where it happened: into the handler (counted against the error budget of the client), into a timer (logged, the timer loop going on) or into a delayed task (logged). No test went over the members. |
| 2.2.5, step 5: one `LuaState` per player | **Not done.** `DoFileAsync` read and compiled the script at every entry, on the loop. Not measured. |
| 2.2.6, step 6: the Maze server type | **Not done.** The maze registered `EntityComponentService.District`; six components carried `[EntityComponent(Maze)]` as well, which nothing selected; `HotspotHandlerAttribute` was on 33 services and read by nothing. |
| Step 2: steps without copies | **Not done.** `SimulationClock.Tick` stepped `_simulations.ToArray()`, `LuaCreatureManager.Advance` stepped `Bodies.Values.ToArray()`, `MonsterBody.Fly` walked `_flights.ToArray()`. |
| Step 7: horizontal scaling | `MazeCache`, `MazeRegistrationService` and the choice of the least busy maze server of a gate existed; compose runs one maze server. |

## Step 1: the queue of the simulation clock

`SimulationClock` (`OpenWorker.Hotspot`) keeps the actions of the game that wait for a moment.

- **The queue.** `PriorityQueue<Scheduled, (long Due, long Order)>`: the due time in ticks of game time, and a sequence number
  so that actions due at the same tick run in the order they were scheduled (a `PriorityQueue` is not stable, and a plain
  `long` priority would leave the order of two actions scheduled with the same delay to chance).
- **Game time.** `Now` is the game time of the last tick. Each tick adds the time since the previous one rounded to whole
  periods, at least one and at most `MaxStep / Period` (4): a delay of one period is always the next tick, even when the
  timer of the process fires a little early, and after a stall the game time moves by at most `MaxStep`, as the steps of
  the simulations do. `Time` is `Now` plus the real time since the tick (at most a step): the moment a handler between two
  ticks reads.
- **The API.** `clock.Schedule(TimeSpan delay, Action)` runs the action at the first tick whose `Now` is at least `delay`
  past the current `Now`, never on the tick that is running (a delay of zero is the next tick, and an action that schedules
  another cannot keep the drain going). It gives a `ScheduledAction`, a handle with `Cancel()` and `IsPending`.
  `clock.CreateGroup()` gives a `ScheduleGroup` for one owner: `Schedule`, `ScheduleAt(TimeSpan due)` (a moment read from
  `Time`), `Count`, `CancelAll()` and `Close()`, which cancels everything of the group and makes it refuse new actions.
- **A tick.** `Tick()` moves `Now`, runs every action that is due, in order, then steps the simulations. An action that
  throws is logged (`An action scheduled on the simulation clock failed.`) and the others run.
- **Cancelling.** A cancelled action stays in the queue, with its delegate dropped (so it holds nothing of its maze), until
  it comes up; when more than 256 cancelled actions make more than half of the queue, the queue is rebuilt without them.
  The entries are reused (at most 4096 kept spare), so scheduling allocates nothing but the delegate; the handle carries the
  version of its entry, and a handle of an action that has run or been cancelled no longer reaches the entry's next action.
- **Tests** (`SimulationClockTest`, on a `FakeTimeProvider`): an action due in three periods runs at the third tick and not
  before, a delay of zero is the next tick, a cancelled one never runs; a simulation removed by another during the tick is
  not stepped.

### What moved onto it

`LuaMaze` is the simulation of a maze now: it goes on the clock when the client has loaded it (`TryStart`) and off it in
`Close()`, and its `Advance` steps, in this order, the timers of its script, the projectiles of the player, the chains of
the player, and the monsters (`LuaCreatureManager.Step`, which is no longer a simulation of its own). Everything else it
waits for goes into its `ScheduleGroup`, which it shares with its creature manager.

| Before | Now |
|---|---|
| `RunLaterAsync(seconds, action)`, `Task.Delay` | `Later(seconds, action)`: `ScheduleGroup.Schedule`, the async work started at the tick and logged if it fails |
| `PlayAsync`: a loop of `Task.Delay` until the next moment of the skill, woken by the key | `Play(run)`: runs at the start, at every key and at the tick of the next moment (`ScheduleAt(began + next)`); the moments are counted on `clock.Time` |
| `PlaceTrapAsync`: `Task.Delay(ContinuousTime)` between hits | `StrikeTrap`: the next hit scheduled after `ContinuousTime` while the life of the trap lasts |
| `FlyAsync`: a 50 ms `Task.Delay` loop | `StepFlights` in the step of the maze, each flight advanced by the time since its own last step |
| `ChainAsync`: a 50 ms `Task.Delay` loop | `StepChains` in the step of the maze |
| `RunTimersAsync`: a 50 ms `Task.Delay` loop around `MazeTimers.UpdateAsync` | `StepTimers` in the step of the maze; a round that waits for a script holds the next ones back and they are added to it |
| `LuaCreatureManager.ShowLaterAsync`: `Task.Delay(WaitCreationDelayTime)` | `ShowLater`: scheduled in the group of the maze |

`Close()` closes the group (every action of the maze is called off and the group takes no more), takes the maze off the
clock, clears the timers, flights, chains and the skill, and closes the creature manager. `MazeScheduleTest`: two mazes on
one clock each schedule a timer, a timer with a function and the turn of a townsman; one is closed; after two seconds of
game time the script of the open one has heard all three and the closed one nothing, its group is empty, the clock steps
one maze, and a call on the closed maze schedules nothing.

No `Task.Delay` and no `System.Threading.Timer` are left in `OpenWorker.Lua` and `OpenWorker.MazeServer` (the registration
of the maze server keeps its `PeriodicTimer`: it is a background service, not game logic). `eng/check-forbidden.sh` has a
new list, `SCOPED`, of rules checked under one path only; the rule `Task\.Delay\(|new +(System\.Threading\.)?Timer\(|System\.Threading\.Timer\b`
holds for those two projects. A file with a `Task.Delay` in `OpenWorker.MazeServer` makes it fail (tried, then removed).

### Timings

The pure combat classes (`SkillPlayback`, `AttackArea`, `ProjectileFlight`, `ChainLightning`, `ActionFile`) are unchanged,
and `ActionFileTest`, `AttackAreaTest`, `ProjectileFlightTest`, `SkillPlaybackTest`, `ChainLightningTest` pass as they were,
with their timings. What changed is when the server delivers:

- **Attacks of the player's skills** land at the first tick at or after their moment: 0 to 50 ms later than before, 25 ms on
  average. `Task.Delay` woke within about a millisecond. The monsters' attacks have landed this way since the clock came
  (`MonsterBody.ResolveHits` in their step), so both sides of a fight are now on the same grid. The decision of what an
  attack hits is made with the moment of the playback, as before.
- **Projectiles and chains of the player** step every tick by the time since their own last step (on `clock.Time`), as the
  50 ms loops did; the first step of one fired between two ticks covers the time since it was fired, not a whole tick.
- **Follow-ups a script asks for** (`SectorClear`, `TerminateSpawn`, `TerminateSpawnBox`, `SetSectorStepStop(…, false)`,
  `AllDestroySectorMonster`, `ExecuteDestroy`, the end of `WaitToDie`) used to run inside the script call that asked for
  them, because `Task.Delay(0)` completes at once: `OnCompleteSector` ran inside the call of `SectorClear`, before the next
  line of the script. They now run at the next tick, after the script has returned, so a script never re-enters itself
  through them. `WaitToDie` is visible to scripts for one tick instead of none.
- **Script timers** count with the real time of each tick, as the 50 ms loop did; a timer added between two ticks gets the
  whole next tick, as one added while the loop ran got the next round.

No dump of a fight was compared: no test timing changed, and the changes above are in the delivery, by at most a tick.

## Step 2: steps without copies, and pooling

- `StepList<T>` (`OpenWorker.Hotspot`): a list walked in place. An item removed during a step is set to nothing and the
  places are compacted when the step ends; an item added during a step is past the end the step walks to and starts with the
  next one. `SimulationClock` steps its simulations through it, and `LuaCreatureManager.Step` its monsters (`Minds`). Before,
  a simulation removed by another during the tick was still stepped from the copy; now it is not.
- `MonsterBody.Fly` walks its flights in place and drops the finished ones after the round (`RemoveAll` with a static lambda).
- **Bodies of monsters** are kept per AI script (`SpareBodies`, at most 32 per script and maze): a monster that dies gives its
  body to the next monster of the same script, with its lists (`_pending`, `_route`, `_found`, `_flights`) and the handler of
  its state changes kept and everything it knew reset (`MonsterBody.Revive`). The mind (`AiMachine`) is made new: it carries
  about thirty fields and six dictionaries of what the monster has seen, and a reset that forgot one would carry the mind of
  a dead monster into a new one.
- **Monster entities** are not pooled by hand: Arch reuses the slot and the id of a destroyed entity of the same archetype,
  without a managed allocation, so `Create` after `Destroy` is already the pool. Keeping dead monsters as live entities would
  put them into every query of the world.

## Step 3: indexes of the map

`EventBox` is a class now, read once with its `VBatchFile` and shared by every maze on the map (it was a struct of 18
lists, copied at every `Batch.EventBox`). Every list is a `BoxList<T>`: the boxes in the order of the file and a dictionary
by id built when the map is read (`Find(id)`, `TryGet(id, out box)`; with two boxes of one id the first is found, as the
search found it). `SpawnsOf(sector)` and `StartOf(sector)` find the spawn boxes and the start of a sector through a lookup
built the first time it is asked for.

All 24 searches by id moved: 8 in `LuaMaze`, 2 in `LuaCreatureManager`, 13 in `MazeService`, and `WorldManager.TryGetStart`
(one call site in `OpenWorker.Channel`). What stays linear is geometric: the sector that holds a position.

A request that names a box the map does not have gets the error answer of the request (`sub | 0x80`, code 55006, the code the
world already gives for the box of a sector it cannot find) and nothing else happens: `MazeEventSpawnBoxRequest`,
`MazeLuaFunctionRequest`, `MazeCheckEventSpawnBoxRequest`, `MazeQuestMoveCheckRequest`. `MazeRequestGuardTest` checks the bytes
the client gets for an unknown Lua function box (it reads nothing and times out without the change).

## Step 4: safe scripts

- **Members.** Scripts can reach 64 `[LuaMember]`s: 49 of `LuaMaze`, 8 of `LuaGameHelper`, `LuaTrap.SetLifeTime` and the six
  coordinates of `VVector3`. None threw `NotImplementedException` when the phase started. 43 do what the scripts and the notes
  say; 21 are no-ops: `ChangeMonster`, `ChangeNpcAnimation`, `ExcutEventSpawnGroupLua`, `ExecuteDestroy` for anything but
  monsters of a table, `InitPartyQuest`, `SetEscortCondition`, `SetEscortMonster`, `SetMonsterAllowPassiveType`,
  `SetMonsterDefenceType`, `SetTrapLifeTime`, `StartDefenceMode`, `StartOperationMode`, `StartSurvivalMode`,
  `MoveNpcToWayPoint` (it only checks that the creature is there), and the seven `CreateTrap*`, `CreateEntity`,
  `CreateProjectile` of the helper. Each no-op logs a warning once per script and function
  (`Script {Script} called {Function}, which the server does not do`), however many mazes call it how often; the details of
  each call stay at `Debug`.
- **The test** (`LuaMemberTest`) finds every type with `[LuaObject]` and every member with `[LuaMember]` by reflection, calls
  each method, getter and setter with the default of each argument (a maze for a maze, an empty vector, an empty string) on
  a maze with no tables and an empty map, and fails on `NotImplementedException` and on any other exception. It needs none of
  the client's scripts. It found one: `SetLuaValue(nil, nil)` threw `Cannot convert LuaValueType.Nil to System.Int32`; a
  value under something that is neither a key nor a number is now logged and not stored, and a value that is no number is 0.
- **An error inside a script call** ends the maze for its player. Every call of the server into a script goes through one
  method (`LuaStateExtensions.CallAsync`), which does nothing for a closed maze and catches what the call throws
  (`OperationCanceledException` aside): it is logged with the function and the script
  (`The script {Script} failed in {Function}; the maze ends for the player`) and `LuaMaze.Fail` closes the maze (nothing of it
  runs any more), takes the `LuaMaze` component off the player (its requests are refused as those of a player who is in no
  maze) and tells the player in a notice (`ChatNoticeResponse`) that the maze has stopped and how to leave. The way out
  (`MazeExitRequest`) does not need the maze and stays open. The error reaches neither the loop, nor a timer, nor the error
  budget of the client, which it is no fault of. A script that fails to load at the entry is logged and the maze runs without
  it, as a maze whose script is missing. `MazeScheduleTest.ScriptThatFailsEndsTheMazeOfItsPlayerAndNotTheLoop`: a timer whose
  function calls `error`; after the tick the player has no maze, the clock steps none, and the event the script had waiting
  never comes.
- **The answer to the client.** The client has a message for a maze mode that fails (`eSUB_CMD_MAZE_MODE_FAIL` in
  `MazeOpcode`), but its layout is not known, so it is not sent.
  The notice is a message the client is known to show. When the layout is known, `LuaMaze.Fail` is where it goes.

## Step 5: the cost of entry

Measured with the 167 scripts of client 1.4.2.3 by `docs/phase-5/entry-cost` (a scratch program, not part of the solution:
`dotnet run -c Release -- <folder of the maze scripts>`), Release, workstation GC, on the machine of the baseline
(Ryzen 9 5950X), 2026-11-09. A state is made as `PersonService` makes it: the standard libraries, `LuaCompatibility`, the
`Soulworker` table.

| What | Time per entry | Allocated per entry | Memory kept |
|---|---:|---:|---:|
| A new state, nothing run | 21 µs | 29.8 KB | 26.5 KB |
| A new state and `DoFileAsync` (what the entry did) | 154 µs | 54.1 KB | 32.6 KB with `M04_RSQUARE_EP_01_Normal` (2 KB of source), 87.2 KB with `C01_LASTCARNIVAL` (41 KB, the largest) |
| A new state and the chunk compiled once | 16 µs | 32.6 KB | as above |
| One shared state, a table of globals per instance | 5 µs | 4.1 KB | 3.3 KB per instance of `M04_RSQUARE_EP_01_Normal` |

Times are the mean over 10 rounds of all 167 scripts, after a round that warms the JIT. The difference between "a new state"
and "a new state and the chunk" is within the noise of the measure: running the chunk of a maze script only defines its
functions.

**Done: the chunks are cached.** `MazeScripts` (`OpenWorker.Lua`, a singleton of the maze server) reads and compiles each
script once, on the thread pool (`Task.Run`), and every entry runs the compiled chunk in its new state
(`new LuaClosure(state, chunk, state.Environment)`): the work of the entry on the loop goes from about 154 µs and a file read
to about 20 µs, and the file is read once per script and process instead of once per entry. A compiled chunk does not change
when it runs; `MazeScriptsTest` runs one chunk in two states and each has its own globals.

**Not done: one state per template.** A shared state would save about 29 KB per maze (6 MB at 200 mazes) and some 15 µs per
entry. Against it: every maze of a script would run on one Lua call stack, and a call that fails or waits in one maze would
leave the state of the others in the middle of it; the scripts' writes to the shared library tables (`table`, `string`, the
`Vision` and `Soulworker` tables) would leak from one maze to the next; and the per-instance globals depend on an `__index`
fallback the scripts were never written for. 6 MB for 200 mazes is not a cost the maze server feels (its working set was
235 to 355 MB in the baseline), so the gain is not worth the change; one state per maze stays.

## Step 6: the Maze server type

Removed instead of filled in. `HotspotHandlerAttribute` and `HotspotHandlerType` were read by nothing: `HandlerTableBuilder`
takes every handler of the loaded assemblies, and each server process loads only its own services (and the shared ones of
`OpenWorker.Channel`), so there is nothing to filter. The attribute is gone from the 33 services that carried it.
`EntityComponentService.Maze` is gone too, with the six `[EntityComponent(Maze)]` marks, which all stood next to a `District`
mark: a player in a maze is the person of the district, read from the same tables and saved by the same code, with the same
components, and the maze keeps registering `EntityComponentService.District` (a comment in its `Program.cs` says why). A set of
its own would have to name every component the shared code of `Gameplay` and `Channel` reads from a player, and one
forgotten would be an exception on the loop; what it would save is a few unused components per player in a maze.

## Step 7: two maze servers

Nothing in the code needs to change: each maze server registers itself in `MazeCache` with a new instance id at every start,
its gate (`Gate__Id`), its public address (`Instance__PublicHost`, `Instance__PublicPort`, or `Maze__Port` when the public port
is not set) and its player count, renewed every 10 s with a lifetime of 30 s; a player entering a maze goes to the maze
server of the gate with the fewest players (`WorldManager.GetMazeAsync`). To run two in compose:

```yaml
  openworker.mazeserver-2:
    <<: *openworker-server
    container_name: "OpenWorker-MazeServer-2"
    build:
      args:
        - BUILD_CONFIGURATION=Debug
      context: .
      dockerfile: OpenWorker.MazeServer/Dockerfile
    environment:
      # ... the same as openworker.mazeserver, and:
      - Gate__Id=1
      - Instance__PublicPort=10102   # the port the clients are sent to, published below
    depends_on: # the same as openworker.mazeserver
    ports:
      - "10102:10000"                # Server__Port stays 10000 inside the container
    networks:
      - eva-01
    volumes: # the same five client folders as openworker.mazeserver
```

- **Ports.** Each server listens on `Server__Port` (10000) inside its container; the published port must differ and be the
  one in `Instance__PublicPort`, because that is what the record tells the clients. The health endpoint (8080) is not
  published and needs no change.
- **Registration.** Both use the same `Gate__Id`. The first server may leave `Instance__PublicPort` unset: its published port,
  10101, is `Maze__Port` of `appsettings.json`, which the record falls back on. `Maze__Host` and `Maze__Port` are also where
  a district sends a player when no maze server is registered at all.
- **Reservations.** `MazeReserveCache` does not name the server it was written for, so a client can take its reservation to
  either maze server of the gate; that is harmless while both serve the same gate, and pinning it would mean writing the
  instance id into the reservation.
- **Metrics.** Prometheus tells them apart by the label `instance`; `simulation.count` and `simulation.step.duration` are per
  process.

Several world loops in one process are not built, as the plan says: one process per core is the unit of scale.

## The benchmark

`OpenWorker.Benchmarks/MazeStepBenchmark`: one step of the simulation of one maze of `M04_RSQUARE_EP_01` with K monsters that
think (one spawn box whose monsters all have an AI script, spawned until there are K), standing round a player who cannot die
(so they see him, walk and fight), and S timers of its script counting. It reads the client tables and AI scripts
(`OPENWORKER_TEST_RES`, `OPENWORKER_TEST_AI`); run it with
`dotnet run -c Release --project OpenWorker.Benchmarks -- --filter '*MazeStep*'`.

- **Before** (commit `705f5fc`, the code of `dev`): `clock.Tick()`, which stepped the creature manager, then one round of
  `MazeTimers.UpdateAsync(0.05)` with the S timers, which is what the 50 ms timer loop of the maze did.
- **After**: `clock.Tick()`, which runs what is due and steps the maze (its S timers, then its monsters); the maze also has S
  actions waiting in the queue (the turn of a townsman, scheduled with each timer), none due.

BenchmarkDotNet 0.15.8, .NET 10.0.14, Ryzen 9 5950X, default job (the raw reports: `bench-before.md`, `bench-after.md`).

| K monsters | S timers (and S waiting actions after) | before: mean | after: mean | before: allocated | after: allocated |
|---:|---:|---:|---:|---:|---:|
| 10 | 0 | 1.800 µs | 1.660 µs | 136 B | 0 B |
| 10 | 100 | 5.974 µs | 5.477 µs | 980 B | 836 B |
| 100 | 0 | 15.433 µs | 14.801 µs | 995 B | 168 B |
| 100 | 100 | 19.928 µs | 17.653 µs | 1819 B | 969 B |

The step of a maze with 100 monsters costs about 15 µs either way: the work is the minds of the monsters, which this phase did
not touch. What went is the copy of the list of minds (824 B for 100 monsters) and of the list of simulations; the 100 actions
waiting in the queue cost nothing to a tick that has nothing due (a peek). What is left of the allocations with timers is
`MazeTimers.UpdateAsync`, which copies its list of timers every round, and a few arrays the monsters build when they look for
the player. At 26% of a core for 100 mazes of one player each (the baseline), the cost of a maze server is not the step of its
mazes: 100 mazes of 100 monsters would be 1.5 ms a tick, 3% of the budget of 50 ms.

## What is left

- **The step p99 at twice the baseline** (criterion of the phase) is for the coordinator to measure after the merge with
  `eng/loadtest.sh`; the machine was shared, so no load test was run here.
- **The failure answer of a maze** (`MAZE_MODE_FAIL`): its layout is to be read from the client; until then the player gets a
  notice.
- **`AiMachine` is not pooled**: it would need its state gathered in one place so that a reset cannot forget a field.
- **Allocations of a step**: `GetFlightTargets`, `FindMonsters`, `PlayersTouching` build arrays with LINQ, and
  `MazeTimers.UpdateAsync` copies its list each round; they are visible in the "after" allocations and are the next thing to
  take out of the step.
- **Reservations do not name the maze server** (see step 7).
- **`VaccumManager`** (`OpenWorker.Gameplay`) still waits with `Task.Delay` for the end of a vacuum; it runs in the maze
  server but lives in `Gameplay`, outside the scope of the new rule.
