# OpenWorker load test

Started 2026-11-08 18:54:50Z, ran 75.1 s, measured 60.0 s.

Bot machine: CachyOS, .NET 10.0.14, 32 cores.

## Scenario

| setting | value |
|---|---|
| bots | 250 |
| ramp-up | 50 per second |
| duration | 1m |
| warmup | 10s |
| move-rate | 5 per second |
| chat-rate | 0.05 per second |
| maze-share | 0.1 |
| maze-location | 21311 |
| skill-rate | 1 per second |
| probe-every | 1 |
| auth | 127.0.0.1:10000 |
| accounts | lt00000 ... |

## Bots

| | |
|---|---|
| asked for | 250 |
| started | 250 |
| reached a district | 250 |
| in the world at the end | 0 (peak 250) |
| failed before they got in | 0 |
| disconnected after they got in | 0 |
| chosen to fight in a maze | 25 (in a maze at the end: 0) |

## Steps into the world

Time is what the step took for the bots that got through it, in ms.

| step | passed | failed | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| auth.login | 250 | 0 | 39.94 | 44.03 | 46.08 | 52.28 |
| auth.gate-list | 250 | 0 | 2.05 | 2.50 | 5.76 | 8.54 |
| gate.enter | 250 | 0 | 2.56 | 3.20 | 4.74 | 5.17 |
| gate.character | 250 | 0 | 5.50 | 7.04 | 8.96 | 10.85 |
| district.enter | 250 | 0 | 7.30 | 8.70 | 12.03 | 16.32 |
| maze.enter | 25 | 0 | 4.35 | 6.91 | 7.75 | 7.75 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 67500 | 0.28 | 0.26 | 0.48 | 0.75 | 3.19 |
| move.delivery | 3750600 | 0.28 | 0.26 | 0.48 | 0.75 | 3.30 |
| ping.after-move | 67500 | 0.32 | 0.30 | 0.53 | 0.85 | 3.31 |
| chat.echo | 675 | 0.27 | 0.26 | 0.46 | 0.86 | 1.78 |
| chat.delivery | 37506 | 0.28 | 0.26 | 0.46 | 0.74 | 1.81 |
| maze.move.echo | 7500 | 0.16 | 0.14 | 0.27 | 0.42 | 3.09 |
| maze.skill.response | 1500 | 0.16 | 0.14 | 0.27 | 0.46 | 1.45 |
| bot.send-lag | 74999 | 0.02 | 0.00 | 0.07 | 0.58 | 9.44 |

Percentiles are read from buckets that are at most 3% wide and are given as the top of the bucket, so a figure is never below the real one; the maximum is exact.

- `move.echo`: A move sent, to the broadcast of that move coming back to the same bot (a district).
- `move.delivery`: A move sent by one bot, to the broadcast of it reaching another bot of the same channel.
- `ping.after-move`: A ping sent in the same write behind a move, to its answer. The server handles the packets of a connection in order, so this bounds the time the server takes to handle the move.
- `chat.echo`: A line said, to the same bot hearing it.
- `chat.delivery`: A line said by one bot, to another bot hearing it.
- `maze.move.echo`: A move sent in a maze, to its broadcast, which in a maze reaches the bot alone.
- `maze.skill.response`: A skill used in a maze, to the answer of the server.
- `bot.send-lag`: How late the bot sent a move compared to when it was due. This is the lag of the bot, not of the server.

## Traffic

| | |
|---|---|
| moves sent | 90660 (74999 while measured, 1250 per second) |
| pings, chat lines, skills sent | 82181, 821, 1695 (skills answered: 1695) |
| moves delivered to other bots | 62510.1 per second |
| bots that hear a move, on average | 50.9 (the size of the channel the bots were in, the mover included) |
| frames received | 4887567 |
| broadcasts not matched to a move | 42 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 2.02 cores on average, 2.11 at the peak, of 32 |
| memory | 106 MB at most |
| garbage collections | 822 / 2 / 1 (gen 0 / 1 / 2), 744 ms paused |
| thread pool | 22 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 15 | 15.4 | 219.8 | 150 | 153 |
| ow-lt-district | docker stats | 15 | 174.5 | 193.8 | 268 | 270 |
| ow-lt-gate | docker stats | 15 | 2.5 | 33.8 | 193 | 194 |
| ow-lt-maze | docker stats | 15 | 8.4 | 9.9 | 235 | 238 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.069 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.066 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.058 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.245 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.463 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.186 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.491 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.062 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.082 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.035 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.058 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 41.986 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 61.271 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 2308.914 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 168.086 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.367 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.922 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.364 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.738 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 2.298 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 49.25 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 7.85 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.126 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 13.734 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 2.29 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.037 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.021 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 7.843 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 9.214 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 2264.714 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 141.357 |
