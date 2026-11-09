# OpenWorker load test

Started 2026-11-08 19:01:01Z, ran 72.1 s, measured 60.0 s.

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
| auth.login | 100 | 0 | 38.91 | 41.98 | 46.08 | 48.48 |
| auth.gate-list | 100 | 0 | 1.82 | 2.18 | 3.84 | 7.80 |
| gate.enter | 100 | 0 | 2.75 | 3.58 | 4.91 | 4.91 |
| gate.character | 100 | 0 | 1.28 | 1.60 | 8.82 | 8.82 |
| district.enter | 100 | 0 | 4.99 | 5.76 | 11.78 | 11.78 |
| maze.enter | 10 | 0 | 3.46 | 9.28 | 9.28 | 9.28 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 26999 | 0.31 | 0.32 | 0.47 | 0.59 | 1.79 |
| move.delivery | 2402921 | 0.31 | 0.32 | 0.47 | 0.59 | 2.02 |
| ping.after-move | 26999 | 0.36 | 0.35 | 0.50 | 0.64 | 2.02 |
| chat.echo | 270 | 0.32 | 0.33 | 0.51 | 0.70 | 1.04 |
| chat.delivery | 24030 | 0.32 | 0.32 | 0.51 | 0.75 | 1.06 |
| maze.move.echo | 3000 | 0.15 | 0.14 | 0.27 | 0.32 | 0.71 |
| maze.skill.response | 600 | 0.16 | 0.14 | 0.27 | 0.33 | 0.77 |
| bot.send-lag | 30000 | 0.01 | 0.00 | 0.04 | 0.29 | 1.14 |

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
| moves sent | 35515 (30000 while measured, 500 per second) |
| pings, chat lines, skills sent | 32194, 323, 665 (skills answered: 665) |
| moves delivered to other bots | 40049 per second |
| bots that hear a move, on average | 81 (the size of the channel the bots were in, the mover included) |
| frames received | 2988610 |
| broadcasts not matched to a move | 39 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 1.13 cores on average, 1.16 at the peak, of 32 |
| memory | 94 MB at most |
| garbage collections | 308 / 1 / 0 (gen 0 / 1 / 2), 178 ms paused |
| thread pool | 22 threads at most, 0 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 15 | 17.2 | 225.4 | 156 | 159 |
| ow-lt-district | docker stats | 15 | 100.7 | 124.6 | 364 | 366 |
| ow-lt-gate | docker stats | 15 | 2.5 | 32.5 | 202 | 204 |
| ow-lt-maze | docker stats | 15 | 3.8 | 5.4 | 258 | 261 |

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
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.062 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.054 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.064 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.055 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.235 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.462 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.232 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.247 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.048 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.073 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.045 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.045 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 14.928 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 11.086 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 923.57 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 81.273 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.156 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.25 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.136 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.75 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 1.689 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.053 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 49.25 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 4.918 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.227 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.099 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 13.452 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 1.686 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.042 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.019 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 2.7 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 1.929 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 908.999 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 57.229 |
