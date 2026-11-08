# OpenWorker load test

Started 2026-11-08 18:53:21Z, ran 72.1 s, measured 60.0 s.

Bot machine: CachyOS, .NET 10.0.14, 32 cores.

## Scenario

| setting | value |
|---|---|
| bots | 100 |
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
| asked for | 100 |
| started | 100 |
| reached a district | 100 |
| in the world at the end | 0 (peak 100) |
| failed before they got in | 0 |
| disconnected after they got in | 0 |
| chosen to fight in a maze | 10 (in a maze at the end: 0) |

## Steps into the world

Time is what the step took for the bots that got through it, in ms.

| step | passed | failed | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| auth.login | 100 | 0 | 39.94 | 45.06 | 51.87 | 51.87 |
| auth.gate-list | 100 | 0 | 2.18 | 2.56 | 7.68 | 8.84 |
| gate.enter | 100 | 0 | 2.88 | 3.26 | 5.04 | 5.04 |
| gate.character | 100 | 0 | 1.44 | 1.92 | 9.20 | 9.20 |
| district.enter | 100 | 0 | 5.76 | 7.30 | 13.32 | 13.32 |
| maze.enter | 10 | 0 | 4.48 | 7.38 | 7.38 | 7.38 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 27002 | 0.24 | 0.23 | 0.38 | 0.48 | 2.43 |
| move.delivery | 1305716 | 0.25 | 0.24 | 0.38 | 0.48 | 2.67 |
| ping.after-move | 27002 | 0.28 | 0.27 | 0.40 | 0.50 | 2.67 |
| chat.echo | 270 | 0.23 | 0.22 | 0.35 | 0.42 | 0.51 |
| chat.delivery | 13056 | 0.24 | 0.24 | 0.36 | 0.42 | 0.70 |
| maze.move.echo | 3000 | 0.14 | 0.13 | 0.22 | 0.28 | 2.33 |
| maze.skill.response | 600 | 0.15 | 0.14 | 0.20 | 0.23 | 0.80 |
| bot.send-lag | 30002 | 0.01 | 0.00 | 0.04 | 0.37 | 1.51 |

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
| moves sent | 35514 (30002 while measured, 500 per second) |
| pings, chat lines, skills sent | 32193, 323, 665 (skills answered: 665) |
| moves delivered to other bots | 21761.6 per second |
| bots that hear a move, on average | 44.4 (the size of the channel the bots were in, the mover included) |
| frames received | 1672473 |
| broadcasts not matched to a move | 29 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 0.88 cores on average, 0.97 at the peak, of 32 |
| memory | 90 MB at most |
| garbage collections | 314 / 1 / 0 (gen 0 / 1 / 2), 193 ms paused |
| thread pool | 19 threads at most, 0 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 15 | 15.8 | 231.2 | 147 | 148 |
| ow-lt-district | docker stats | 15 | 74.5 | 118.7 | 255 | 259 |
| ow-lt-gate | docker stats | 15 | 2.7 | 36.8 | 192 | 194 |
| ow-lt-maze | docker stats | 15 | 4.1 | 6.4 | 226 | 229 |

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
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.065 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.067 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.05 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.067 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.244 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.471 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.1 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.248 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.059 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.082 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.029 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.051 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 16.915 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 13.014 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 916.571 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 81.327 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.17 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.319 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.154 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.75 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 2.593 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.099 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 49.601 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 4.952 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 13.833 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 1.787 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.027 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.022 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 3 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 2.229 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 910 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 57.228 |
