# OpenWorker.LoadTest

Bots for the servers of OpenWorker. A run starts N bots in one process; each signs in, gets a character, enters a
district, walks in a circle and talks, and a share of them goes to a maze and uses a skill. The report gives the
latency of movement, the CPU and memory of the servers and of the bot itself, and, when Prometheus is there, the lag of
the world loop. It is the load scenario of phase 1 of `REFACTORING.md`.

## One command

```bash
SOULWORKER_DATAS=/mnt/games/soulworker/datas eng/loadtest.sh --bots 500 --duration 2m
```

The script (`eng/loadtest.sh`) checks `SOULWORKER_DATAS` (the `datas` folder of an installed client; the district, gate
and maze servers cannot start without it), starts `compose.yaml` with `eng/loadtest/compose.loadtest.yaml` laid over it,
waits until the health check of every server passes, creates the accounts of the bots, runs them, and writes
`TestResults/loadtest/<timestamp>/report.md` and `report.json`. Everything after the script name is passed to the bot.
`LOADTEST_DOWN=1` stops the containers at the end; the other variables are listed at the top of the script.

The override makes the servers fit to be measured (Release builds, Warning logs, no packet dumps, twenty channels per
district), starts the other servers only once the relay is healthy (it migrates the database and makes the Redis
indexes), and is used by the script only. `compose.yaml` is not changed.

## By hand

```bash
dotnet run -c Release --project OpenWorker.LoadTest -- seed --count 1000        # accounts lt00000 .. lt00999
dotnet run -c Release --project OpenWorker.LoadTest -- run --bots 1000 --ramp-up 50 --docker-stats
```

`run --help` and `seed --help` list the options. The ones that matter most:

| option | meaning | default |
|---|---|---|
| `--bots` | number of bots | 100 |
| `--ramp-up` | bots started per second | 20 |
| `--duration`, `--warmup` | length of the measured time, and the time after the last start that is not measured | 60s, 5s |
| `--move-rate` | moves per second of every bot | 5 |
| `--chat-rate` | lines per second of every bot (the server allows 5 per 3 s) | 0.05 |
| `--maze-share`, `--maze-location`, `--maze-delay` | share of the bots that fight, the maze, how long they play in the district first | 0.1, 21311, 3s |
| `--skill-rate`, `--skill` | skills per second in a maze, and the skill (0: the first the character has learned) | 1, 0 |
| `--probe-every` | send a ping behind every n-th move (0: none) | 1 |
| `--auth-host`, `--auth-port` | the auth server | 127.0.0.1:10000 |
| `--redirect-host` | follow every redirect to this host (the servers answer `127.0.0.1`) | the answer of the server |
| `--channel`, `--district` | ask for a channel after entering; travel to another district | the server's choice |
| `--channel-fill` | bot n asks for channel 1 + n / this: the channels fill in order, so many bots each (65 is a full channel) | off |
| `--account-prefix`, `--account-offset`, `--password` | the accounts | `lt`, 0, `loadtest` |
| `--output` | directory of `report.md` and `report.json` | `TestResults/loadtest/<time>` |
| `--docker-stats`, `--docker-names`, `--pids name=pid` | sample CPU and memory of the servers | off |
| `--prometheus <url>`, `--prom-metric`, `--prom-query` | ask Prometheus for the loop and handler metrics | off |

Ctrl+C stops the run and still writes the report of what was collected; a second Ctrl+C ends the process.

## How the bots sign in

A server creates an account only on a request in Discord, and the `Auth:AllowNextHumanNetworkLogin` login signs
everybody in as the first account of the database, so it can carry one bot. The bots use the normal login with a
username and a password, the way a launcher does. `seed` writes the accounts straight into Postgres (the same rows as the
Discord command, one shared password hash, existing accounts untouched) so the servers need no change and no setting that
is off in production. A gate, too, is made only by a Discord command, and the auth server lists only the gates of the
database, so `seed` also makes gate 1 (`--gate`, the `Gate:Id` of the gate server) when the database has no gate of that
number. The character is made by the bot through the protocol when the account has none.

## Reading the report

See `docs/phase-1/D-loadtest.md` for the exact definition of each latency and for what the figures can and cannot tell.
The report starts with the warnings that matter: bots that did not get in, bots the server dropped, and a bot that was
itself close to its limit (its CPU, or moves sent late), which means the latency includes the lag of the bot.

## Tests

`OpenWorker.LoadTest.Test` checks every request against the request types of the server and the packets a real client
sent, every response against the response types of the server, the statistics and the report, and runs whole bots against
stand-in servers built from the same types (`FakeWorld`).
