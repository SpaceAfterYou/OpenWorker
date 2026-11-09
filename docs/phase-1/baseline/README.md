# Baseline of phase 1: 100 to 1000 bots on one machine

The first runs of `OpenWorker.LoadTest` against the real servers. Each folder (`100-bots`, `250-bots`, `500-bots`,
`1000-bots`) holds the `report.md` and `report.json` of one run; the table below is taken from them. What each figure
means is in `docs/phase-1/D-loadtest.md` ("What is reported, and what each latency means").

## Conditions

- Machine: AMD Ryzen 9 5950X (16 cores, 32 threads), 62 GB RAM, CachyOS, kernel 7.3.3, Docker 29.9.3. The bots and the
  servers run on the same machine; the bots reach the servers through the ports Docker publishes on 127.0.0.1.
- Servers: the Release build of the solution at commit `3eea4e3` (the merge of workstream D; the server code is that of
  `388cc13`, the bot commits that followed change no server), each one run from its `bin/Release/net10.0` in the image
  `mcr.microsoft.com/dotnet/aspnet:10.0` (.NET 10.0.14) with plain `docker run` on one user-defined network, not with
  compose (`docker compose` is not installed on this machine). The recipe is that of the local stand script
  (`docs/tools/stand_up.sh`, not in the repository) with the settings of `eng/loadtest/compose.loadtest.yaml`:
  `Logging__LogLevel__Default=Warning`, `Hotspot__PacketDumpPath=` (no packet dumps), `Game__Version=1.4.2.3`, and
  `DOTNET_CONTENTROOT=/srv` so that each server finds its `appsettings.json` in the mounted build while its working
  directory, `/app`, holds the client data.
- One auth server, one gate (`Gate__Id=1`), one district (Rocco Town, location 10003, channels 1 to 20), one maze
  server, and the relay once at the start with a placeholder Discord token (it migrated the database and made the Redis
  indexes, and stayed up). Redis is `redis/redis-stack-server`, Postgres is `postgres:alpine`, both in containers on the
  same machine. Metrics go to Prometheus v3.15.1 over OTLP every 10 s.
- Client data: the tables, maps, maze scripts, action files and navigation meshes of client 1.4.2.3.
- Accounts `lt00000` to `lt00999` and gate 1 made by `OpenWorker.LoadTest seed --count 1000`. A run of N bots uses the
  first N accounts; an account without a character creates one at the start of the run (so the 250, 500 and 1000 runs
  created 150, 250 and 500 characters while they ramped up).
- Bot: `OpenWorker.LoadTest` at commit `b7cbf00`, Release, run on the host with .NET 10.0.14:
  `run --bots N --ramp-up 50 --duration 60s --warmup 10s --move-rate 5 --channel-fill 65 --docker-stats
  --docker-names ow-lt-auth,ow-lt-gate,ow-lt-district,ow-lt-maze --prometheus http://127.0.0.1:9090`, the other options
  at their defaults: a tenth of the bots go to maze 21311 and use a skill a second, a chat line per bot per 20 s, a ping
  behind every move. `--channel-fill 65` is not shown in the scenario table of the reports (the bot of that commit did
  not list it); see "Channels" below for why it was given.
- Dates: 2026-11-08, the four runs between 18:53 and 19:00 UTC, one after another, with the sessions of every server
  back at zero (`sessions_active` in Prometheus) before each.

## Results

Latencies in ms, from the bot (`move.echo`, `move.delivery`, `ping.after-move`, `chat.delivery`, `maze.move.echo`: see
D-loadtest.md). CPU is in percent of one core, from `docker stats`, averaged over the whole run (ramp-up and warm-up
included). CPU per bot is the district's average CPU % × 10 / bots, i.e. ms of CPU per second per bot. Loop callback,
loop queue and simulation step are from Prometheus over the length of the run, per server.

| bots | got in | move.echo p50 / p99 | move.delivery p99 | ping.after-move p99 | chat.delivery p99 | maze.move.echo p99 | district CPU avg / max % | district memory max (MB) | maze CPU avg % | district CPU per bot (ms/s) | bot CPU avg / peak (cores) | worldloop callback p99 district / maze (ms) | worldloop queue max district / maze | simulation step p99, maze (ms) | unmatched | errors |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 100 | 0.23 / 0.48 | 0.48 | 0.49 | 0.42 | 0.28 | 74.5 / 119 | 259 | 4.1 | 7.4 | 0.88 / 0.97 | 0.10 / 0.25 | 0 / 0 | 0.32 | 29 | 0 |
| 250 | 250 | 0.26 / 0.75 | 0.75 | 0.85 | 0.73 | 0.41 | 174.5 / 194 | 270 | 8.4 | 7.0 | 2.02 / 2.11 | 0.19 / 0.49 | 0 / 0 | 0.92 | 42 | 0 |
| 500 | 500 | 0.29 / 1.22 | 1.22 | 1.31 | 1.18 | 0.66 | 295.1 / 321 | 300 | 14.9 | 5.9 | 3.44 / 3.68 | 0.17 / 0.92 | 0 / 0 | 1.75 | 41 | 0 |
| 1000 | 1000 | 0.43 / 3.52 | 3.52 | 4.22 | 3.65 | 1.82 | 421.4 / 466 | 355 | 25.6 | 4.2 | 5.16 / 5.79 | 0.21 / 2.02 | 2 / 0 | 3.42 | 47 | 0 |

Every bot of every run got through login, gate list, gate entry, character, district entry and (the tenth chosen for
it) maze entry; none was dropped, no error was counted, every skill was answered. "Unmatched" is the count of move
broadcasts the bot could not tie to a move, out of 1.3 to 15.3 million deliveries.

More from the same reports:

| bots | moves delivered per second | bots that hear a move (channel size) | maze.skill.response p99 (ms) | handler p99 district / maze (ms) | gate.character p99, step (ms) | district.enter p99, step (ms) | bot GC: gen 0 / 1 / 2, pause total | bot.send-lag p99 (ms) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 21 762 | 44.4 | 0.23 | 0.10 / 0.10 | 9.2 | 13.3 | 314 / 1 / 0, 193 ms | 0.37 |
| 250 | 62 510 | 50.9 | 0.46 | 0.13 / 0.10 | 9.0 | 12.0 | 822 / 2 / 1, 744 ms | 0.58 |
| 500 | 125 830 | 51.2 | 0.74 | 0.10 / 0.10 | 9.7 | 14.3 | 1754 / 411 / 4, 1724 ms | 0.75 |
| 1000 | 254 910 | 51.9 | 1.92 | 0.16 / 0.10 | 188.4 | 43.0 | 3319 / 790 / 6, 5089 ms | 1.38 |

## Channels

A district is meant to put a player into the first channel that is not full (a channel is Full above 64, i.e. at 65
players). Against the real district every bot landed in channel 1 instead: `ChannelUtils.GetWorkload(IComparable count)`
is called with `Members.Count`, a boxed `int`, and matches it with relational patterns against the `short` constants of
`ChannelModuleDefinitions.Workload`; a relational pattern on a boxed value first tests its type, a boxed `int` is not a
`short`, so every channel is `Low` and `JoinChannel` never skips one. The server was not changed. The baseline runs give
`--channel-fill 65`, which makes bot n ask for channel 1 + n / 65 after entering, so the channels hold what the district
means them to hold: 65 bots of the numbering each, of which about 58 stay in the district (a tenth go to the maze), and
a remainder channel. That is why a move is heard by 44 to 52 bots on average, not 65.

Without it, all the district bots of a run share one channel and every move goes to all of them, so the work of the
district grows with the square of the bots. The same command without `--channel-fill`, run right after the baseline
(reports in `one-channel/`):

| bots | bots that hear a move | move.echo p50 / p99 | ping.after-move p99 | district CPU avg / max % | district CPU per bot (ms/s) | worldloop queue max (district) | worldloop callback p99, district (ms) | moves delivered per second | bot CPU avg (cores) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 81.0 | 0.32 / 0.59 | 0.64 | 100.7 / 125 | 10.1 | 0 | 0.23 | 40 049 | 1.13 |
| 250 | 202.5 | 0.61 / 1.85 | 2.17 | 347.1 / 402 | 13.9 | 1 | 0.49 | 251 998 | 3.02 |
| 500 | 363.1 | 4718.59 / 7733.25 | 7864.32 | 834.6 / 941 | 16.7 | 473 | 0.98 | 905 739 | 3.68 |

At 500 bots in one channel (450 in the district) the district falls behind: its world loop had up to 473 callbacks
waiting, a move took 4.7 s to come back at the median, the entry into the district took up to 2.3 s, and the bots heard
fewer moves than were sent because broadcasts were still queued when the run ended. The bot was not the limit (3.7
cores, send lag p99 3.5 ms). The same run also hit a second fault of the district: 199 times, `World:ObjectReq` failed
with `NotSupportedException: Memory stream is not expandable` in `WorldOtherPersonListResponse.Write`, called from
`ServerSessionComponent.TrySend`, whose buffer holds one frame (`MessageHeader.MaxContentLength`): the list of the other
players of a channel of several hundred does not fit one frame. A channel capped at 65 does not reach it, and no
baseline run logged it. The sessions of every server went back to zero after each of these runs as well.

## What the numbers can and cannot tell

They tell what one district and one maze server cost and how fast they answer on this machine, for bots that do what the
scenario says: walking at 5 moves a second in channels of about 58, chatting rarely, and a tenth alone in a maze, using
a skill a second. Up to 1000 bots nothing was close to a limit of the servers: the world loop of the district never had
more than 2 callbacks waiting, its callbacks and handlers stayed at a p99 of about 0.2 ms, and its CPU grew less than
linearly (7.4 ms of CPU per second per bot at 100, 4.2 at 1000, partly because the averages include the quiet ramp-up
and a fixed cost is spread over more bots). The p99 of a move's round trip rose from 0.5 ms to 3.5 ms while the handlers
stayed flat, so the rise is in what is around the handler: fan-out into 52 send queues per move, the writes to the
sockets, the loopback, and the reading of 255 000 broadcasts a second by the bot.

They cannot tell how the servers behave on their own hardware. The bot shares the cores: at 1000 bots it used 5.2 of the
32 threads on average, the servers about 5 more, and its garbage collector ran 3319 gen-0 collections that paused it
for 5.1 s in all over 90 s, so part of every p99 is the bot's own pauses (its send lag p99 went from 0.4 to 1.4 ms).
The network is the loopback, with no latency, loss or bandwidth limit; Redis and Postgres sit on the same machine. The
per-channel size of 65 is enforced from the bot side, not by the district (see "Channels"); a district that fills its
channels itself would show the same work only if it fills them the same way. Every move goes to the whole channel
(there is no area of interest), so the cost of a move is set by the size of a channel, and a channel of 65 is the worst
case the district allows. Each maze holds one bot that stands near its start, uses the first skill it learned and meets
no player; the maze's cost of monsters and scripts is for 10 to 100 mazes of map 21311 with nobody fighting the
monsters. The figures of the servers' histograms are coarse: their first bucket ends at 0.1 ms and the next at 0.25 ms,
so a p99 under 0.25 ms only says that most callbacks are that short. Each size was run once; the spread between runs is
not known. The login (about 40 ms per account, the hashing of the password) and the character creation of 500 new
accounts at 50 a second (p99 188 ms at 1000) happen in the ramp-up and are not part of the measured time.

## Run through eng/loadtest.sh

The baseline above was taken with `docker run`. Once `docker compose` (v5.7.0) was installed, the one command of step 5
was run end to end on the same machine, on 2026-11-08 at 19:48 to 19:55 UTC, with no `--channel-fill`, so the district
placed the players itself, with the channel fix of `89ea83b` (`ChannelUtils.GetWorkload`). Report in
`compose-500-bots/`.

```
SOULWORKER_DATAS=$HOME/.cache/openworker/datas eng/loadtest.sh --bots 500 --ramp-up 50 --duration 60s --warmup 10s \
  --move-rate 5 --prometheus http://127.0.0.1:9090
```

What differs from the baseline: the servers are the images of `compose.yaml` built by the script with
`eng/loadtest/compose.loadtest.yaml` (Release, Warning logs, no packet dumps, twenty channels per district) at commit
`ee880d4` (the server code is that of `89ea83b`); the whole of `compose.yaml` runs, i.e. four districts (the bots use Rocco
Town), RabbitMQ, pgAdmin, Prometheus and Grafana as well; the database is fresh (the Postgres container has no volume),
so all 500 accounts created their character during the ramp-up; and `docker stats` covers every `OpenWorker-*` container.
The script built the bot and the images (3 min 15 s), waited 44 s for every server to turn healthy, seeded the accounts
(1 s), waited 15 s, and ran the bots (80 s and the report): 5 min 45 s in all.

The first runs of the script failed on faults of the script and the override, fixed in `ee880d4` before this run: the
wait for the TCP ports passed at once (Docker publishes a port before the server in it listens), on a fresh database the
maze server died with `relation "ServerContents" does not exist` (it reads the table while it builds its services, before
any server has migrated), the gate could not write its record before the relay had made the Redis indexes, and the first
43 bots found no gate because the auth server reads the gates of the database once a second and the seed ran right
before the bots. The script now waits for the health checks, the other servers start when the relay is healthy, and the
seed runs before the 15 s wait.

| bots | got in | move.echo p50 / p99 | move.delivery p99 | ping.after-move p99 | chat.delivery p99 | maze.move.echo p99 | district CPU avg / max % | district memory max (MB) | maze CPU avg % | district CPU per bot (ms/s) | bot CPU avg / peak (cores) | worldloop callback p99 district / maze (ms) | worldloop queue max district / maze | simulation step p99, maze (ms) | unmatched | errors |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 500 | 500 | 0.31 / 1.70 | 1.70 | 1.89 | 2.11 | 0.86 | 310.7 / 482 | 235 | 21.8 | 6.2 | 3.38 / 3.67 | 0.42 / 0.98 | 0 / 0 | 2.39 | 81 | 0 |

| bots | moves delivered per second | bots that hear a move (channel size) | maze.skill.response p99 (ms) | handler p99 district / maze (ms) | gate.character p99, step (ms) | district.enter p99, step (ms) | bot GC: gen 0 / 1 / 2, pause total | bot.send-lag p99 (ms) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 500 | 133 731 | 54.4 | 0.99 | 0.36 / 0.10 | 516.1 | 770.1 | 1709 / 300 / 3, 2003 ms | 0.83 |

Every bot got in, the 50 chosen for the maze entered it, every skill was answered, no bot was dropped and no error was
counted.

Channels: the `OnlineCount` of the channel records of Rocco Town in Redis, read every 5 s during the run, settled at
65, 64, 64, 61, 61, 60, 59 and 16 players in channels 1 to 8 (450, the 500 bots less the 50 in the maze), and stayed so
to the end. The district filled channel 1 to 65, moved on to the next, and gave the seats freed by bots that left for the
maze to the bots that entered later. So the fix works on the real district. A move in the district is heard by 60.4 bots
on average (the sum of the squares of these sizes over their sum); the report's 54.4 is lower because it divides by every
move sent, the 15 000 moves of the maze bots included, which are heard by their sender alone.

Compared with the 500-bot row of the baseline (`docker run`, `--channel-fill 65`): every bot got in with no error in both,
and the work is about the same, a little more because the channels are fuller (54.4 bots hear a move against 51.2,
133 731 deliveries a second against 125 830). The district used 310.7% of a core on average against 295.1 (6.2 ms of CPU
per second per bot against 5.9), and its world loop never had a callback waiting. The p99 is higher: 1.70 ms against 1.22
for `move.echo`, 1.89 against 1.31 for `ping.after-move`, 0.42 against 0.17 ms for the district's callbacks. The district
handlers stay at 0.36 ms p99, and the bot did not lag (send lag p99 0.83 ms), so the rise is most likely the rest of the
stack, which shares the machine here and did not in the baseline: three more districts, RabbitMQ (44.8% of a core on
average, 368% at the peak) and pgAdmin. Character creation and district entry take longer at the p99 (516 against 9.7
ms, 770 against 14.3 ms) because all 500 accounts were new and created their character in the ramp-up, against 250 in
the baseline. Each was run once, so the spread is not known.

When the run ended and the 450 district bots left at once, the district logged `53300: sorry, too many clients already`
from Postgres: 3 `Session close handler failed`, 29 `Could not tell about a person leaving`, 32 failed connections, all
at 19:54:42. Postgres runs with its default `max_connections` of 100, and the pools of the servers held 96 connections
when idle afterwards (gate 34, maze 33, Rocco Town 19, auth 5, and one each for the other servers). The sessions of every
server still went back to zero. The measured part of the run is not affected; the state of the characters whose close
handler failed may not have been saved. The server was not changed.

## GC modes at 1000 bots

Step 7 of phase 1: which garbage collector the servers run with. Four runs of `eng/loadtest.sh` on 2026-11-08 between
20:53 and 21:06 UTC, alternating, workstation (the props as they were) and Server GC with DATAS
(`LOADTEST_COMPOSE_EXTRA=eng/loadtest/compose.servergc.yaml`, which sets `DOTNET_gcServer=1` and
`DOTNET_GCDynamicAdaptationMode=1` for every server). Reports in `gc-workstation-1/`, `gc-server-1/`,
`gc-workstation-2/`, `gc-server-2/`, in the order they ran.

```
SOULWORKER_DATAS=$HOME/.cache/openworker/datas LOADTEST_NO_BUILD=1 [LOADTEST_COMPOSE_EXTRA=eng/loadtest/compose.servergc.yaml] \
  eng/loadtest.sh --bots 1000 --ramp-up 50 --duration 60s --warmup 10s --move-rate 5 \
  --prometheus http://127.0.0.1:9090 --prom-group-by 'job, openworker_location' --prom-query 'gc pause total (ms)=...' ...
```

Conditions, where they differ from "Run through eng/loadtest.sh" above:

- Images built once from the tree of commit `0fb31ec` (the server code of `e87cbf3` and the log line below), Release; the whole of
  `compose.yaml` with the load-test override, no `--channel-fill` (the district fills its channels). The override gives
  Postgres `max_connections=200`; none of the four runs logged `too many clients`, and the district logged no error.
- Every run started from `docker compose down` (volumes kept): fresh Postgres, Redis and RabbitMQ, fresh servers, so all
  1000 accounts created their character in the ramp-up of every run. Before each run `sessions_active` was 0 for every
  server in Prometheus, and it was 0 again after each.
- No container has a CPU or memory limit, so every server sees the 32 threads of the machine.
- The mode was checked in the log of each run: every server logs one line at start (`RuntimeSettingsLogService`), and
  the override keeps that category at Information. Workstation runs: `GC: server False, concurrent True, DATAS False
  (adaptation mode 1), latency mode Interactive, heaps 1, processors 32.` Server GC runs: `GC: server True, concurrent
  True, DATAS True (adaptation mode 1), latency mode Interactive, heaps 32, processors 32.`, the same on the relay, auth,
  gate, Rocco Town and maze. `heaps` is the heap count the collector was configured with; under DATAS it is the most it
  may grow to, not the heaps in use (the runtime exports no metric of those).
- GC figures are the `System.Runtime` meter of each server in Prometheus (`dotnet_gc_pause_time_seconds_total`,
  `dotnet_gc_collections_total`, `dotnet_gc_heap_allocated_bytes_total`, `dotnet_process_cpu_time_seconds_total`,
  `dotnet_process_memory_working_set_bytes`), taken as the difference between the last export before the run started and
  the last one after it ended (exports every 10 s, so a window of 92 to 112 s that holds the ramp-up, the warm-up, the
  measured minute and the logout). The `--prom-query` rows of the reports use `increase()` over the run, which
  extrapolates to the edges of the window and gives about 5% more; the table uses the exact differences. The share of
  the run is the pause over the 92 s the bot ran.

| run | start (UTC) | GC | move.echo p50 / p99 | ping.after-move p99 | maze.move.echo p99 | district CPU avg / max % | district working set max (MB) | district memory max, docker (MB) | district GC pause (ms, % of 92 s) | district gen 0 / 1 / 2 | district allocated (MB) | district CPU time (s) | worldloop callback p99 district / maze (ms) | worldloop queue max district / maze | simulation step p99, maze (ms) | maze GC pause (ms) | maze gen 0 / 1 / 2 | maze working set max (MB) | errors |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| gc-workstation-1 | 20:54 | workstation | 0.53 / 4.35 | 4.99 | 2.30 | 442.2 / 478 | 396 | 282 | 1948 (2.11 %) | 484 / 27 / 3 | 8445 | 394 | 0.29 / 2.09 | 0 / 0 | 4.62 | 129 | 24 / 5 / 2 | 301 | 0 |
| gc-workstation-2 | 21:00 | workstation | 0.46 / 3.77 | 4.48 | 1.89 | 450.8 / 485 | 394 | 275 | 1894 (2.06 %) | 484 / 27 / 3 | 8440 | 403 | 0.30 / 2.06 | 2 / 0 | 3.25 | 118 | 24 / 5 / 2 | 300 | 0 |
| gc-server-1 | 20:57 | Server, DATAS | 0.47 / 2.75 | 3.20 | 2.05 | 453.8 / 489 | 450 | 377 | 1461 (1.59 %) | 785 / 138 / 4 | 6908 | 406 | 0.32 / 2.05 | 2 / 0 | 2.49 | 34 | 6 / 2 / 2 | 362 | 0 |
| gc-server-2 | 21:03 | Server, DATAS | 0.45 / 2.37 | 2.62 | 1.89 | 433.8 / 489 | 493 | 400 | 958 (1.04 %) | 713 / 6 / 6 | 7003 | 402 | 0.26 / 2.07 | 5 / 0 | 3.16 | 40 | 6 / 2 / 2 | 374 | 0 |

The district is Rocco Town (`openworker_location` 10003); the CPU % and the docker memory are from `docker stats`, the
working set from the runtime. In all four runs every bot got in, every skill was answered, no error was counted, and
the bot itself used 4.9 to 5.3 cores and paused 4.9 to 5.0 s in its own GC (the bot runs workstation GC in every run).

**Decision: Server GC with DATAS.** Across the two runs of each mode:

- `move.echo` p99 was 2.37 and 2.75 ms with Server GC against 3.77 and 4.35 ms with workstation. The two workstation runs
  differ by 0.58 ms, the two Server GC runs by 0.38 ms, the modes by 1.5 ms on average; the worse Server GC run is better
  than the better workstation run. `ping.after-move` p99 follows (2.62 / 3.20 against 4.48 / 4.99 ms). The p50 is the
  same (0.45 to 0.53 ms).
- The district paused for 958 and 1461 ms in a run with Server GC against 1894 and 1948 ms with workstation. It collected
  more often (713 and 785 gen-0 collections against 484) but each pause was shorter: 1.3 to 1.6 ms per collection on
  average against 3.8 ms. The two Server GC runs differ more from each other (503 ms; 138 gen-1 collections in one, 6 in
  the other) than the workstation runs (54 ms), but less than the modes differ (711 ms on average).
- The maze paused for 34 and 40 ms against 118 and 129, with 6 gen-0 collections against 24, and its simulation step p99
  was 2.49 and 3.16 ms against 3.25 and 4.62. Its world loop callbacks did not change (p99 about 2.1 ms).
- Memory: the district's working set peaked at 450 and 493 MB against 394 and 396 MB (1.2 times; `docker stats` 377 and
  400 against 275 and 282 MB, 1.4 times), the maze's at 362 and 374 against 300 MB. Well under twice the workstation
  figure: DATAS kept the heaps few. The CPU of the district is the same in both modes (394 to 406 s of CPU per run).
- The allocated bytes differ (about 7000 MB against 8440 MB for the same work, the same moves and broadcasts). The
  meter reads `GC.GetTotalAllocatedBytes()` without `precise`, which is not exact, and the difference was not looked
  into; the figure is not part of the decision.

So `Directory.Build.props` now sets `ServerGarbageCollection` to `true`; the other values (background collections,
`GarbageCollectionAdaptationMode` 1, `TieredPGO`) stay as they were, and TieredPGO was not compared (it was on in all
four runs). Images built from the props after the change, with no override and no `DOTNET_gc*` variable in
the containers, log `GC: server True, concurrent True, DATAS True` on all five servers (checked once with
`docker compose up --build`, no bots).

What these runs cannot tell: one size (1000 bots), a minute measured, so neither a long run nor the growth of memory
over hours is seen; the servers saw 32 threads and had no CPU or memory limit, so a container limited to a few cores, or
a machine with fewer of them, was not tried (DATAS sizes the heaps from the load and the cores; under a memory limit
Server GC takes a share of it); the bot and the rest of the stack share the machine and the bot's own GC pauses are
part of every p99 in both modes; two runs per mode give a spread, not a distribution.
