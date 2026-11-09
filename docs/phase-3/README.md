# Phase 3: interest management and channels

Scope: steps 1 to 6 of phase 3 in `REFACTORING.md` (branch `refactor/phase-3`). The goal of the phase: the cost of a move
grows with the number of players who see the mover, not with the number of players of the channel. The phase 1 baseline
showed the opposite (`docs/phase-1/baseline/README.md`, "Channels"): in one channel of 450 bots every move went to every
bot, the district loop fell behind by 473 callbacks and a move took 4.7 s to come back.

## Step 1. The visibility grid

**Cells.** Every `ServiceChannel` cuts its map into square cells of `World:VisibilityRadius` game units (5000 by default)
and keeps each member in the list of its cell (`Dictionary<long, Cell>`, a cell is created the first time a player stands
in it and kept). A player sees the members of its own cell and of the eight around it. So it sees everything within one
cell size, and nothing farther than twice the diagonal of a cell (about 14 100 units with the default), and the relation is
symmetric. The X and Y of the place count; the height does not. A member knows its cell and its slot in the list of the
cell, so leaving a cell is a swap with the last member and no search; walking the nine cells is a struct enumerator.

**Size of a cell, and why 5000.** Nothing is known about how far the client draws other players: `docs/NOTES.md` has no
view distance, only that the reference server keeps "the surroundings by a grid" (`XDistrict::SendObjectInfo`). 5000 is a
guess, to be checked on a real client. What it rests on: one unit is about a centimetre (Rocco Town starts at
16401, 9960, 127, and the exit of `M04_RSQUARE` lands near X 15 862), so a cell is 50 m; a hero runs 560 units a second
(`Normal_Run_Speed` of `tb_Character_Info`), so it crosses a cell in about 9 s; the sight of most monsters in
`tb_Monster` is 1000. A player therefore sees at least 50 m around it, which should cover what is on the screen in a town,
and the cells are large enough that a running player crosses an edge every few seconds, not every step. If a real client
shows players popping in or out at a visible distance, raise the radius; if a crowded town is still too costly, lower it.

**What is sent.**

| Event | To the players around | To the player |
|---|---|---|
| Joining a channel (entering the district, changing channel) | `WorldInInfoPc` and `PersonUpdateOriginStatList` of the player, in its nine cells | nothing (it asks with `World:ObjectReq`) |
| Leaving (closing, changing channel) | `WorldOutInfoPc` of the player | nothing |
| Crossing the edge of a cell | the cells that come into sight get `WorldInInfoPc` and `PersonUpdateOriginStatList`; those that go out of sight get `WorldOutInfoPc` | the same about each player who comes into sight, and one `WorldOutInfoPc` with everyone who goes out of sight (at most 255 per message) |
| `World:ObjectReq` | | `WorldOtherPersonList` with the players in sight only |

The answer to `World:ObjectReq` no longer grows with the channel, which also closes `REFACTORING.md` 2.9.3 (the list of a
channel of several hundred did not fit one frame).

**What goes to the players in sight** (`BroadcastNearby`, `BroadcastNearbyToOthers`): every movement message (moves,
stops, jumps, battle stance, motions, loop motions, the end of a knock-back), `PersonChangeMotionBroadcast`, the gestures,
the change of a title shown to others (`PersonProfileService`) and the league alert (`LeagueNotifier`). A player who does
not see the actor knows nothing of it, so it has no use for them; when it comes into sight, `WorldInInfoPc` carries the
current state. **The chat stays channel-wide** (`ChatService.SendNearby` calls `Broadcast`), as the plan says;
`docs/notes-social.md` notes that the reference server sends the normal chat to the area around the speaker, which would
be one call to `BroadcastNearby` once it is wanted.

**Encoding once, without a box.** `Frame.Encode<T>(in T) where T : struct, IResponseHotspotMessage, IWireWritable` writes a
message with a span format straight from its struct; the movement handlers pass their responses as generic structs
(`MovementServiceBase.Broadcast<T>(Entity, in T)`), so the boxing per move that phase 2 left (`A-send-path.md`, "Handlers that
box their response") is gone. A broadcast to the neighbours encodes one frame and queues it to each of them.

What allocates: joining (the snapshot of the player for `WorldInInfoPc`), crossing an edge (the snapshots of the players who
come into sight, the arrays of `WorldOutInfoPc`) and `World:ObjectReq` (the list). A move within a cell, the common case,
allocates nothing.

## Step 2. Coalescing of moves

The world loop has a natural batch boundary: the moment it has handled everything that is queued and is about to wait.
`WorldLoop.AtEndOfPass(IEndOfPass)` runs work then, after the callback that emptied the queue, or after 5 ms of a queue that
never runs dry, so a loop under load still sends. No timer and no `SimulationClock` are involved.

A move (05/02) does not go out when it is handled: `ServiceChannel.Move` keeps it in the member (one struct field, no
allocation) and registers the channel for the end of the pass once. A later move of the same player in the pass replaces it.
At the end of the pass the channel sends each waiting move once, encoded once, to the cells around the mover as they are
then. So an observer gets at most one position update of an actor per pass, with the last place. Any other message of the
player for the players around it (a stop, a jump, a gesture) sends the waiting move first, so the order the client sees is
the order the player acted in; leaving the channel drops it. The validation and the cell change happen when each move is
handled, so `WorldInInfoPc` and `WorldOutInfoPc` still come at the crossing.

The read loop of a connection hands over the next packet only after the handler of the previous one has finished, so the
moves of one player pile up only when the loop is behind: when it keeps up, a pass is one packet and nothing waits;
when it falls behind, what is behind is folded. This is the behaviour the plan asks for, at no cost while there is no load.
Off the loop (tests, tools) a move goes at once.

## Step 3. Validation of movement

`MovementPace` decides whether a player could have covered the ground a move claims.

- **Speed.** The highest speed allowed is the run speed of the hero, times the move speed of its abilities, times
  `World:SpeedTolerance` (1.5). The run speed is the larger of `Normal_Run_Speed` and `Battle_Run_Speed` over the rows of the
  hero in `tb_Character_Info` (the columns after the costumes). In the client tables of this machine the rows `1000·hero` have
  560 and 560, the rows `1000·hero + 100` have 400 and 480 to 520; the dash (`Battle_Dash_Speed`) is 800 for every hero, which the
  tolerance covers (560 × 1.5 = 840). The only modifier that exists today is `AbilityComponent.Speed.Move`, the move speed the
  client is told in its character info, a percentage that both servers set to 100; speed buffs, titles with speed effects
  (`docs/notes-social.md`: parsed, not applied) and transports are not there yet.
- **Budget of running.** A player saves up running time as time passes, at most `World:MoveBurst` (5 s), and a move spends
  the distance it covers on the ground divided by the speed allowed. A move that would spend more than is saved is refused.
  Saving up is what lets the moves through that arrive late or bunched together (a stall of the connection), and a client
  that reports its place rarely; a plain "distance / time between packets" check refuses the second of two packets that
  arrive together. The price: a player that stood still may jump by up to the burst of running at once, 5 × 840 = 4200 units
  by default. A teleport across a map is refused. The first move after entering starts with a full burst.
- **Refusal.** The place stays as it was, nobody else hears of the move, and the player is sent `WorldWarpRes` (04/08) with
  the place the server keeps. That is the message the maze already sends to put a player somewhere within the map, and the
  client takes it there; the move broadcast of the player itself would not do, because the client skips its own 05/02
  (`docs/NOTES.md`, "Movement and channels"). A refusal is logged at Debug.
- **Error budget.** The first refusal in a second counts against the error budget of the session
  (`HandlerExecutor.Charge`, reason `violation` in `handler.errors`, the same log lines and closing as a packet that fails):
  the moves the client sent before it saw the correction are refused too, and they are one fault. Repeated teleports close
  the connection once the budget is used up (`Hotspot:ErrorBudget`, 10 in a minute).
- **Where.** Every request of `MovementServiceBase` that carries a place goes through it, in the district and in the maze.
- **Not checked.** The height (falling is fast and legal), walls and the floor: they wait for the navigation data, as the
  plan says. A place set by a skill (`SkillPassiveEnd` in the district) is taken as sent and becomes the reference for the
  next move.

## Step 4. Rate limits

A token bucket per group of opcodes and per session, in `HandlerExecutor` next to the error budget: before a packet is
parsed, `PacketRateLimiter` takes a token from the bucket of its group in the `PacketRateComponent` of the session (added on the
first packet, four buckets of a float and a timestamp). A bucket fills at `PerSecond` up to `Burst`; a packet that finds it
empty is dropped and counted in `handler.refused` with the group as tag (`movement`, `chat`, `items`, `other`). It is no
error of the session and closes nothing: a client that sends too fast loses packets, not its connection. The dashboard shows
the refused packets on the panel of the handler errors.

| Group | Opcodes | `PerSecond` | `Burst` |
|---|---|---:|---:|
| `Movement` | 05 | 30 | 60 |
| `Chat` | 07 (the chat keeps its own throttle of lines, 5 in 3 s) | 5 | 10 |
| `Items` | 08 items, 09 shop, 0A trade, 13 upgrade, 14 drop, 18 item setup | 20 | 40 |
| `Other` | everything else | 60 | 200 |

The defaults are guesses with room: the load test bot sends 5 moves a second and one ping behind each; a real client has not
been measured. The burst of `Other` is large because a client sends a flurry of requests when it enters a server
(character info, quests, missions, options). The settings are `Hotspot:RateLimits:<Group>:PerSecond` and `:Burst`.
`DispatchBenchmark` sets a bucket that never runs dry, so it measures the way to the handler.

## Step 5. Channel counters by events

`ChannelLifecycleService` read and wrote every channel every second (a query and an update each). Now a join or a leave
notifies `ServiceChannels.Changes`, a bounded channel of one element that drops what comes while a notice is pending (the
loop never waits and allocates nothing). The service waits for a notice, then waits a second on its `TimeProvider` for the
joins and leaves that follow, then writes each channel whose count differs from what it wrote last: one `InsertAsync` of the
whole record with its lifetime per channel, built from the identifier the channel chose when the server started
(`ServiceChannel.Record`, which the bootstrap uses too), with no lookup. A failed write is tried again after the next wait. A
count in Redis is therefore at most about a second old, as before.

**The lifetime.** Phase 4, merged while this branch was under way, gave the records a lifetime of 30 s, which the old loop
renewed every second, and made it write again a record that had expired. That is kept, less often: the wait for a change
ends after `ChannelLifecycleService.Renewal` (10 s) at the latest, and then, or at the first write after that, every record is
written again whether its count changed or not. A record expired while Redis was away comes back within 10 s; a record of a
server that crashed goes within 30 s. The drain still takes the records back.

## Step 6. `ServerChannels.MoveAsync`

Removed, with `Switch` returning what `ServiceChannels.TrySwitch` says. The stub did nothing and answered `Ok`, and
`SwitchAsync` sent every result that was not `Ok` there, so a change to a full channel, to the same one or to one that does not
exist was reported as done. Moving between processes is not needed: the list of channels the client is offered
(`SendListAsync`) holds the channels of this server only, so a change never names a channel of another process, and an
identifier this server does not run is `NotFound`. Implementing it would take a reservation for the channel and a district
that honours the channel of the reservation when the player enters, which it does not (`PersonService.JoinChannel` takes the
first channel that is not full, whatever `DistrictReserveCache.Channel` says).

## Benchmarks

`NearbyMoveBenchmark` (BenchmarkDotNet, `MemoryDiagnoser`): a channel of `Players` players, 16 of whom (the mover among
them) stand in the cell of the mover and the others in cells three and more away. `MoveToThoseInSight` is one move request
through the movement handler of the district: the check of the speed, the cell unchanged, one frame encoded and queued to the
16 sessions in sight, whose writers send it to `Stream.Null`. Off the loop a move goes at once, so it is the cost of a pass
that holds one move. Run: `dotnet run -c Release --project OpenWorker.Benchmarks -- --filter '*NearbyMove*'`.

A benchmark of the old way, the same move queued to every member of the channel, was tried beside it and dropped: queued to
64 or 500 writers per operation, as fast as BenchmarkDotNet calls it, it outruns the writers, which fill their 4096 frames
and close their sessions (30 of 64 and 499 of 500 closed in a short run), so the time it reports is the time of sending to
closed sessions. That is the phase 1 result in small. `BroadcastBenchmark` (50 recipients) stands for it below: the cost of a
broadcast is about 0.4 µs per recipient, so the move of a channel of 500 cost some 200 µs of queueing before this phase.

Machine: AMD Ryzen 9 5950X, CachyOS, .NET SDK 10.0.403, runtime 10.0.14, default job, workstation GC. Two other agents built
and tested on the same machine during the runs (load average 7 to 21 of 32 threads), so the numbers carry some noise; the
broadcast to 50 took 23.6 µs here against 21.2 µs on the idle machine of phase 2.

| Benchmark | | Mean | Allocated |
|---|---|---:|---:|
| `NearbyMoveBenchmark.MoveToThoseInSight` | 64 players, 16 in sight | 8.94 µs (median 9.14) | 0 B |
| | 500 players, 16 in sight | 8.90 µs (median 8.91) | 1 B (rounding of the diagnoser) |
| `BroadcastBenchmark.EncodeAndQueueToFifty` (phase 2, same run) | 50 recipients | 23.58 µs | 0 B |
| `DispatchBenchmark.DispatchAMove` | without the rate limiter | 42.82 ns | 0 B |
| | with it, as a server runs | 73.63 ns | 0 B |

The rate limiter costs about 31 ns a packet, nearly all of it the read of the clock (`TimeProvider.GetTimestamp`) that a
token bucket needs; a first version that looked the component up twice and went through `TimeSpan` cost 42 ns.

The move to the players in sight costs the same with 64 and with 500 players in the channel: it follows the 16 who see the
mover, not the channel, and allocates nothing.

## Tests

Six new test methods, all behavioural:

- `DistrictMovementTest.AMoveIsHeardOnlyByThePlayersWhoSeeTheMover`: a player in the next cell hears a move, one three cells
  away does not, and the list for `World:ObjectReq` holds the one in sight.
- `DistrictMovementTest.CrossingTheEdgeOfACellTellsWhoComesIntoSightAndWhoGoesOut`: crossing into the next cell tells the
  player ahead and the mover about each other and the player behind nothing; crossing the next edge tells the player behind
  and the mover that the other is gone, and the player behind does not hear the move.
- `DistrictMovementTest.TheMovesOfAPlayerInOnePassOfTheLoopGoOutOnceWithTheLastPlace`: three moves queued behind a held loop
  reach the observer and the mover once, with the last place; the next pass sends the next move.
- `DistrictMovementTest.ATeleportIsRefusedThePlayerIsWarpedBackAndRepeatedOnesCloseTheSession`: a run is accepted; a move across
  the map is refused, the place stays, the player gets `WorldWarpRes` with its last place, nobody hears of it, the fault counts
  once in its second, and three faults in three seconds use up a budget of three.
- `HandlerExecutorTest.APacketOverTheRateOfItsGroupIsDroppedAndCountedWithoutAnError`: a burst of three passes, the two after
  it are dropped and counted under `other` with no error of the session, and a second later two more pass.
- `ChannelCounterTest.TheCountIsWrittenOnceTheJoinsAndLeavesOfASecondAreInAndTheLifetimeIsRenewedLessOften` (needs `OPENWORKER_TEST_REDIS`): two
  joins and a leave are written as 1 once the second has passed on the clock of the service, nothing is written before it, a
  value written behind the back of the service stays through five seconds of its clock and more than a second of real time,
  the renewal then writes the record again with a lifetime, and the next join is written; the drain takes the record back.

## What to check on a real client

What a real client shows cannot be checked here. By hand, with two or three clients in one channel of Rocco Town:

1. **The edge of a cell.** Walk one character away from another, standing one, until it is two cells away (more than 5000
   units along X or Y, up to 14 100 on the diagonal): the other character should vanish on `WorldOutInfoPc`, without an error
   and without a ghost left standing; walking back should bring it in again on `WorldInInfoPc`, in the right place, with its
   costume, title and league. Check what the client does when a character comes into sight while it is moving (does it snap,
   does it play the run).
2. **The distance.** Whether 5000 units is beyond what the client draws: a character should not pop in or out where the camera
   can see it. Adjust `World:VisibilityRadius` from what is seen.
3. **Crowds.** Many characters around an NPC: everyone in the cells around is shown; check the frame rate of the client and
   that the list of `World:ObjectReq` after loading is right.
4. **Changing channel.** After a change, the characters of the old channel vanish and those around in the new one appear.
   The server still sends no `ChannelChangeResponse` (it never did); check whether the client waits for one.
5. **The correction.** With a speed hack or by editing a packet, move a character far at once: the client should be put back
   by `WorldWarpRes` (04/08), which has only been seen in a maze so far; check that a district takes it, and that a normal run,
   a dash, a jump, a knock-back, a skill that moves the character and a long run in a straight line are never put back.
   If they are, raise `World:SpeedTolerance` or `World:MoveBurst`, and look for a speed modifier the server does not know.
6. **The rate limits.** Normal play, entering the district and the maze, a long chat, a shop with many purchases: nothing should
   be dropped (`handler.refused` stays at zero). If something is, raise the limit of its group.

## Left

- **The load test.** The criterion "at a fixed density the cost of a move per bot does not grow with the bots of the channel"
  needs `eng/loadtest.sh`, which the coordinator runs after the merge. Two things to know before reading it: the bots stand
  within ±10 units of the entry point (`Bot.cs`), so all bots of a channel share one cell and the run measures the same work as
  before inside a channel of 65; to show the criterion the bots must spread over an area larger than a cell (an option of the bot,
  not made here). And under load the moves of a bot are folded, so a bot hears fewer of the moves of the others and fewer echoes
  of its own (the folded ones are not matched, `move.echo` keeps the last); the moves delivered per second drop by design.
- **The view distance of the client** is a guess (see step 1) until a real client is checked.
- **NPCs** still all go in the answer to `World:ObjectReq`; they are few per district and do not move.
- **Speed modifiers** other than the ability speed (buffs, titles, transports) are not applied; the transport (05/16) is not
  implemented. The height, walls and floor wait for the navigation data. A place set by a skill is not checked.
- **The chat** stays channel-wide; the reference server sends the normal chat to the area around.
- **The district ignores the channel of its reservation** on entry, and the client gets no answer to a change of channel; both
  were so before.
- **Crossings allocate** the snapshots of the players who come into sight; a crowded crossing could build them once per pass.
