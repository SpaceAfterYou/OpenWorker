# Phase 1, workstream D: the load test bot

Step 5 of phase 1 of `REFACTORING.md`: `OpenWorker.LoadTest`, a console application that runs N bots in one process
against the servers of `compose.yaml`, and `eng/loadtest.sh`, which does it with one command. Usage is in
`OpenWorker.LoadTest/README.md`; this file says what was done, how it is checked, what each figure means, and what could
not be checked.

## Done

- `OpenWorker.LoadTest` (solution folder "Load test"): the bot, the report, the samplers, the `seed` command.
- `OpenWorker.LoadTest.Test`: the tests (below).
- `eng/loadtest.sh` and `eng/loadtest/compose.loadtest.yaml`: the one command.
- `eng/check-forbidden.sh`: one file of the tool, `OpenWorker.LoadTest/Program.cs`, may use `Console` (see "Output").
- No server code was changed. No setting of a server was added. `compose.yaml` and the props files are untouched.

### Scenario of a bot

1. Auth: connect, log in with a username and a password, ask for the list of gates, ask to enter the first gate that is
   online (or `--gate`), and follow the redirect.
2. Gate: enter with the session, ask for the characters, create one when there is none (the looks of a real client's
   creation), select it, and follow the redirect to the district.
3. District: enter with the reservation the gate wrote, ask who is around, optionally change the channel (`--channel`,
   or `--channel-fill n`: bot k asks for channel 1 + k / n) or travel to another district (`--district`). Then move
   along a circle at `--move-rate`, say a line at `--chat-rate`, and, behind every `--probe-every`-th move, send a ping.
4. A share of the bots (`--maze-share`, spread evenly over the bot numbers) asks for the maze `--maze-location` after
   `--maze-delay` (plus up to as much again), follows the redirect, enters, tells the server it has loaded the maze, and
   then moves and uses a skill at `--skill-rate`. A refusal leaves the bot in the district.

The servers answer `127.0.0.1` for the next hop by default; `--redirect-host` makes the bots connect to another host
instead (a bot in a container, or on another machine).

One process drives the bots with no thread per bot: the script of a bot is one async task that sleeps until its next
action; each connection has one read loop that reads into one pooled buffer, decrypts in place and hands the frame to the
bot; requests are built into pooled buffers with no stream and no writer object; the hot path (the broadcast of a move,
the answer to a ping, a chat line) reads fixed offsets and allocates nothing. A move and its ping go out in one write.

## How a bot logs in

`Auth:AllowNextHumanNetworkLogin` signs in without checking the ticket, but as the first account of the database
(`FirstOrDefaultAsync` with no filter), whatever the request says: every bot after the first would meet
`InGameAlready`. It cannot carry a load test, and it is a setting that must stay off in production, so the bots do not use it.

The bots use the other login the auth server has, `LoginAuthRequest` (username, password, MAC address), which checks
the password hash. An account is created only by a Discord command of the relay, so a run needs accounts in the database
first. That is the `seed` command: it writes the rows straight into Postgres (`AccountPersistent` through
`PersistenceContext`, the same password hashing as the Discord command, one shared salt and hash, existing accounts
left alone). Names are `<prefix><5 digits>` (`lt00000`...); the column holds 12 characters. It also makes the gate row
the gate server registers as (`--gate`, default 1, the `Gate:Id` of `compose.yaml`) when the database has none of that
number: gates come from a Discord command as well, the auth server lists only the gates of the database, and without the
row every bot stopped at the gate list with `no-gate` (found in the first run against real servers). This changes no
server, adds no setting that is off by default, and weakens nothing in production: only a person with the database can
add accounts. `eng/loadtest.sh` runs it before the bots. The character is created by the bot with the protocol, as a player does.

There is **no dev-only switch** on any server.

## Messages

Requests are encoded by the bot (`Protocol/Requests.cs`, built on `FrameWriter`); responses are decoded by the bot
(`Protocol/Responses.cs`, built on `PacketReader`, plus the server's own readers where it has them). The key table of the
encryption is the server's: `FrameCipher` asks `MessageEncryption` for the three key bytes of a version once and applies
them with a loop of its own, and a test compares it to `MessageEncryption.Exchange` for every version and many lengths.
Opcodes are built from the server's enumerations and compared in tests with the `Opcode` of the server's message types.

"Dump" means a packet saved by the servers in `OpenWorker.Hotspot.Test/Datas/1.4.2.3`, sent by a real client: the test
reads it with the server's request type, encodes the same values with the bot, and requires the same bytes on the wire
(header version 0, encrypted body).

| Message | Dir. | Verified by |
|---|---|---|
| Login `LoginAuthRequest` (02,01) | bot to server | server's reader, dump 0x02,0x01 |
| Gate list request (02,03), gate connect request (02,05) | to server | server's reader, dumps |
| Enter gate request (02,13) | to server | server's reader, dump |
| Person list (03,11), create (03,01), select (03,13) | to server | server's reader, dumps (create: the bot's template equals the captured request) |
| Person enter (03,21) | to server | server's reader, dump |
| World object (04,06), maze create (04,41), world enter (04,01) | to server | readers and dumps (world enter: reader only) |
| Move (05,01) | to server | server's reader, dump |
| Ping (01,06), keep-alive (01,05) | to server | server's reader, dumps |
| Chat normal (07,01), channel change (F1,02), maze complete (11,22) | to server | server's reader, dumps |
| Skill active (06,08) | to server | server's reader only: no dump exists |
| Login result (02,02), gate list (02,04), gate connect (02,11), enter gate (02,14) | to bot | written by the server's response types, decoded by the bot |
| Person list (03,12), select (03,14), world enter result (04,02) | to bot | same; person list and skills use the server's `PersonValue` and `SkillLoadInfoResponse` readers |
| Move broadcast (05,02), pong (01,06), keep-alive (01,05), chat (07,01), skill result (06,09), error form (opcode with 0x80), kick out (03,04) | to bot | same, including the fixed offsets of the hot path |

No dump of a server response exists (the servers save only what they receive), so the responses are checked against the
server's writers, not against a real server's bytes. Tests of whole bots against `FakeWorld` (stand-in servers in the test
project that read requests and write answers with the server's own types) check that the pieces work together:
login, character creation, district entry, moves, chat, ping, channel change, travel, maze entry, skills, refusals,
dropped connections and a stop in the middle of a run.

## What is reported, and what each latency means

All times are taken with `Stopwatch` in the bot process, so the bots of one process can be compared to each other.
A latency is recorded only in the measured time (after the ramp-up has settled and the warm-up is over). Percentiles come
from log-linear histograms with 32 buckets per power of two: at most 3% wide, the top of the bucket is reported (never
under the true value), the maximum is exact.

- **`move.echo`**: from the moment before the bot writes a move to the socket, to the moment the same bot has read the
  broadcast of that move. The server sends a move to everybody in the channel, the sender included. It covers the bot's
  send, the network, the read loop of the server, the wait on the world loop, the handler, the write queue of the
  server, the network back and the bot's read loop. This is what a player feels as the lag of input.
- **`move.delivery`**: from the same write of a move by bot A, to the moment bot B of the same channel has read the
  broadcast of it. Bots live in one process, so the timestamp of the send is shared. The broadcast is matched to the move
  by its position alone: a bot walks along a circle of 1024 points around its place, the server echoes the position
  unchanged, and the receiver finds the sender by the actor in the broadcast and the point by the angle. No field of the
  message that the server might check later is used. Broadcasts that cannot be matched are counted (`Unmatched`).
- **`ping.after-move`** (handling): a ping goes out in the same write behind the move. The server handles the packets of a
  connection in order (ARCHITECTURE.md, "Read loop"), so the answer cannot come before the move was handled and its
  broadcast was queued; it also waits for one more pass of the world loop. It bounds the time the server takes to handle
  the move, and unlike `move.echo` it does not include the fan-out to the others' queues in the receiver's time. A maze does
  not answer pings, so there is none there.
- **`chat.echo`, `chat.delivery`**: the same two for a chat line (the text is a number that identifies the line).
- **`maze.move.echo`**: the echo of a move in a maze (the maze tells a move to its player alone).
- **`maze.skill.response`**: from sending a skill to the answer; answers come in the order of the requests.
- **`bot.send-lag`**: how late the bot wrote a move compared to when it was due. The lag of the bot, not of the server.
- **Steps into the world**: for each step (login, gate list, gate enter, character, district enter, travel, maze enter),
  how many bots passed and failed and how long the step took. The district entry ends when the ping sent behind the
  request is answered, i.e. when the server has finished the whole entry.
- **Average listeners**: (echoes + deliveries) / moves sent while measured: the number of bots that heard a move, the
  sender included, i.e. the size of the channel. A channel holds 65 players (Full above 64) and the district puts a player in
  the first channel that is not full, so a thousand bots need at least sixteen channels (the override sets twenty). The
  district of phase 1 does not do that (it puts everybody into channel 1, see below); `--channel-fill 65` does it from
  the bot side.
- **Errors**: counted by `step: kind` with one example, e.g. `auth.login: refused-InGameAlready`, `district.enter:
  closed-by-server` (the server turned the connection away), `maze.enter: refused-50003`, `session: disconnected`.
- **The bot itself**: CPU in cores (average and peak), memory, collections and pause time of the GC, thread pool size
  and queue. The report warns when the bot used 80% of the cores of the machine or sent moves late, because the latency
  then includes the lag of the bot. Running the bot and the servers on one machine shares the cores between them.
- **Servers**: with `--docker-stats`, one `docker stats --no-stream` per interval for the running containers named
  `OpenWorker-*` (or `--docker-names`); with `--pids name=pid`, `/proc/<pid>/stat` and `status`. CPU is in percent of one
  core, as in `docker stats`. Both are optional and stop with a note if `docker` or `/proc` is not there.
- **Loop lag (Prometheus)**: with `--prometheus <url>`, instant queries over the length of the run for the metrics
  workstream A adds: `worldloop.queue.length` (max, mean), and p50, p99, mean and rate of `worldloop.callback.duration`,
  `simulation.step.duration` and `handler.duration`, per `job`. The names are the ones Prometheus has after the OTLP
  translation (`worldloop_queue_length`, `worldloop_callback_duration_seconds`, ...; the servers record durations in
  seconds, and a name that ends in `_seconds` is reported in milliseconds) and are settings:
  `--prom-metric worldloop-queue=<name>` (keys `worldloop-queue`, `worldloop-callback`, `simulation-step`, `handler`),
  `--prom-query label=promql` for any other query, `--prom-group-by`. A Prometheus that cannot be reached skips the step
  with a note; a metric with no data is noted in its row.

## Output

The report goes to the standard output as Markdown, to `report.md`, and to `report.json` (same figures; latencies in ms;
the timeline of the run is in it). Progress lines go to the standard error. `eng/check-forbidden.sh` forbids `Console`
in product projects. The report writer and the commands take a `TextWriter`, so tests read what they write; the console
is touched in one file, `Program.cs`, which has a path-scoped exemption in the script with the reason next to it. That is
cleaner than an exemption for the whole project and keeps the rule for the rest of it.

## Command line and script

`OpenWorker.LoadTest run|seed [options]`, `--help` on each; the options are in the README. Defaults: 100 bots, 20 started
per second, 5 s warm-up, 60 s measured, 5 moves per second, a chat line per 20 s, a tenth of the bots in a maze.

`eng/loadtest.sh [bot options]`: fails with a clear message if `SOULWORKER_DATAS` is not set or lacks `bin/Table`,
`World/Table`, `World/Navmesh`, `Character/ActionData`, `Scripts/Server`; builds the bot; starts compose with the
override; waits until the health check (`/readyz`) of the relay, auth, gate, the four districts and the maze passes, and
stops at once, naming the server, when one exits (an open port proves nothing: Docker publishes it before the server
listens); seeds the accounts (`--bots` of them) and the gate row; waits `LOADTEST_SETTLE` seconds for the channels to
register and the auth server to list the gate; runs the bots with `--docker-stats` and `--output
TestResults/loadtest/<timestamp>`. The override (`eng/loadtest/compose.loadtest.yaml`) builds Release, logs at Warning,
switches the packet dumps off, sets twenty channels per district, runs the relay once (it drops the Redis cache on
every start) with a placeholder Discord token, because the relay creates the Redis indexes and will not start without a
token, and starts the other servers only when the relay is healthy (on a fresh database the maze server dies if it
starts before the schema exists, and the gate cannot write its record before the indexes exist).

The nightly run in CI is not added (GitHub runners have no client data). The normal CI builds the new projects with the
rest of the solution and runs their tests.

## Tests

`OpenWorker.LoadTest.Test` (a separate project because the existing test projects are about the types of the server, and
this one needs the bot, the dump folder as content, and sockets). 94 tests, none skipped: codecs of requests (against
the server's readers and the dumps) and responses (against the server's writers), the cipher, the framing (a stream cut at
many sizes, 60 KB frames, broken headers), the histogram, the move circle (all 1024 points at several coordinates),
durations and options (invariant culture under `de-DE`), the samplers' parsers (`docker stats`, `/proc`), the Prometheus
reader and queries, the report (invariant culture, warnings), and the scenarios against `FakeWorld`.
Results are in the final report of the workstream.

## Not done

- An end-to-end test against a real in-process auth server with Redis: not written. The stand-in servers cover the flow
  with the server's types, and the codecs are checked against the server's types and dumps. The real handlers of the
  auth server (database, Redis index of sessions) are not exercised by any test. They were exercised by hand in the
  baseline runs (below).
- Server-side hops back from a maze to a district (the bots stay in the maze until the end).
- Characters are always created with the template of one hero; the hero cannot be chosen.

## Verified against the real servers (the baseline)

`docs/phase-1/baseline/` holds runs of 100, 250, 500 and 1000 bots against the real auth, gate, district and maze
servers with the data of client 1.4.2.3 (Release builds, `docker run`, one machine). They verified:

- The whole scenario: login with a seeded account, gate list, gate entry, character creation and selection, district
  entry, channel change, moves, pings, chat, maze request, maze entry and skills. In all four runs every bot got in and
  stayed, every skill was answered, and no error was counted.
- The protocol of the bot against the real servers: every request was accepted, every response the bot reads was read,
  and the move broadcasts were matched to their moves (at most 47 unmatched out of 15.3 million deliveries).
- `--maze-location 21311` has a maze row and a batch file, and a bot that stands near the start and uses the first skill
  it learned gets an answer to every skill.
- The relay with a placeholder Discord token stays up, migrates the database and makes the Redis indexes.
- The names Prometheus gives the metrics of workstream A: `worldloop_queue_length`, and
  `worldloop_callback_duration_seconds`, `simulation_step_duration_seconds`, `handler_duration_seconds` (with `_bucket`,
  `_sum`, `_count`): the unit is seconds, not milliseconds as assumed, which the bot now follows.
- 1000 connections from one address: the hops (auth, gate, district, and maze for a tenth) of 1000 bots started at 50
  a second did not run out of ports.
- The one command: `eng/loadtest.sh` ran end to end over compose with the override on 500 bots (`baseline/README.md`,
  "Run through eng/loadtest.sh", report in `baseline/compose-500-bots/`): it built the bot and every image, waited for the
  health checks, seeded a fresh database, and ran the bots, with every bot in and no error; every server was healthy,
  the compose Prometheus had the series of all five servers and Grafana answered. The maze service found its navigation
  meshes. Without `--channel-fill`, the district itself put the players into channels of 65 (65, 64, 64, 61, 61, 60, 59
  and 16 at the end, after the maze bots left), so the channel fix works on the real district. Its first runs found three
  faults of the script and the override, fixed in `ee880d4`: the wait for the ports passed at once, the other servers
  started before the relay had migrated a fresh database and made the Redis indexes (the maze server died), and the first
  second of bots found no gate because the seed ran right before them. The maze fault was the server's: it read
  `ServerContents` while its container was built, before its own migration. Fixed afterwards: the table is read by a
  hosted service that starts after the migration (`ServerContentLoadService`), and a maze started alone on an empty
  database migrates it and turns ready.

The first runs found five faults of the bot, fixed with tests: no gate in a fresh database (`seed` now makes it), the
names of the duration metrics, durations in seconds that the report rounded to zero, and the moves of the monsters of a
maze counted as unmatched broadcasts (twice: monsters without a bot's number, then monsters that carry the number of a
district bot). They also found three faults of the servers; the costume and the channel faults were fixed afterwards,
the third is open:

- The gate refuses the outfit a real client sends for Stella by default (511) with `CreateFailed`:
  `PersonCreationCatalog` reads the offered costumes from `Default_Cloth_ID_01..04` of `tb_Character_Info`, which hold
  521, 531, 541 and 50011; the first costume, 511 (also in `tb_Create_Cloth`), is in the column before them (`Field20`),
  so the names of the generated columns are one place off. The bot sent 521 until it was fixed. Fixed afterwards in
  `OpenWorker.Res/Rows/CharacterInfoRow.cs`: `Default_Cloth_ID_01..04` now name the four columns that start one place
  earlier (111 to 141 for Haru up to 611 to 641 for Iris, the costumes of `tb_Create_Cloth`), the column after them
  (10011 for Haru, 50011 for Stella) is `Field24`, and the bot sends the client's own 511 again.
- The district puts every player into channel 1: `ChannelUtils.GetWorkload(IComparable)` matches the boxed `int` of
  `Members.Count` against `short` constants, which never matches, so no channel is ever Full. The baseline uses
  `--channel-fill 65` to fill the channels from the bot side. Fixed afterwards in `OpenWorker.Hotspot/Cache/ChannelUtils.cs`:
  `GetWorkload` takes a `double` and compares it with the constants as a number, so a channel is Full above 64 players
  and the next one is used; `--channel-fill` stays as a way to control the placement from the bot side.
- With several hundred players in one channel, `World:ObjectReq` fails with `Memory stream is not expandable` in
  `WorldOtherPersonListResponse.Write`: the list of the others does not fit one frame (`MessageHeader.MaxContentLength`).
  Only seen because of the previous fault.

The run over compose found one more, open: when 450 bots left the district at once, Postgres refused connections
(`53300: sorry, too many clients already`) and 3 session close handlers failed. Postgres runs with its default
`max_connections` of 100, and the pools of the servers of `compose.yaml` hold about 96 connections when idle.

## Still unverified

- `eng/loadtest.sh` with `shellcheck` (not installed here; the shell syntax is checked with `bash -n`), with
  `LOADTEST_DOWN=1`, and from a second machine (`--redirect-host`).
- Other heroes, other mazes, a maze with more than one bot, a maze fought through to its end, and a hop back from a maze.
- That the hot path of the bot allocates nothing: at 1000 bots its garbage collector ran 3319 gen-0 collections in 90 s
  and paused it for 5.1 s in all, so something allocates. Where was not profiled.
- Runs on separate machines (bots and servers), with a real network between them, and runs repeated to know their
  spread.
- More than 1000 bots, more than one district, and the sizes at which the servers stop keeping up when the channels are
  filled as the district means to fill them.
