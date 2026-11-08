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
