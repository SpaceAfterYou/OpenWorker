# OpenWorker load test

Started 2026-11-08 18:56:56Z, ran 80.1 s, measured 60.0 s.

Bot machine: CachyOS, .NET 10.0.14, 32 cores.

## Scenario

| setting | value |
|---|---|
| bots | 500 |
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
| asked for | 500 |
| started | 500 |
| reached a district | 500 |
| in the world at the end | 0 (peak 500) |
| failed before they got in | 0 |
| disconnected after they got in | 0 |
| chosen to fight in a maze | 50 (in a maze at the end: 0) |

## Steps into the world

Time is what the step took for the bots that got through it, in ms.

| step | passed | failed | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| auth.login | 500 | 0 | 43.01 | 46.08 | 48.13 | 54.99 |
| auth.gate-list | 500 | 0 | 2.18 | 2.88 | 3.71 | 12.99 |
| gate.enter | 500 | 0 | 2.94 | 3.65 | 4.86 | 6.84 |
| gate.character | 500 | 0 | 5.38 | 8.19 | 9.73 | 19.69 |
| district.enter | 500 | 0 | 7.42 | 10.75 | 14.34 | 17.32 |
| maze.enter | 50 | 0 | 5.38 | 8.45 | 14.05 | 14.05 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 135000 | 0.32 | 0.29 | 0.62 | 1.22 | 6.71 |
| move.delivery | 7549797 | 0.33 | 0.29 | 0.62 | 1.22 | 6.75 |
| ping.after-move | 135000 | 0.37 | 0.32 | 0.67 | 1.31 | 6.71 |
| chat.echo | 1350 | 0.33 | 0.29 | 0.66 | 1.18 | 3.50 |
| chat.delivery | 75498 | 0.34 | 0.29 | 0.66 | 1.18 | 3.65 |
| maze.move.echo | 15000 | 0.18 | 0.14 | 0.34 | 0.66 | 3.80 |
| maze.skill.response | 3000 | 0.18 | 0.14 | 0.34 | 0.74 | 1.90 |
| bot.send-lag | 150000 | 0.03 | 0.00 | 0.12 | 0.75 | 6.60 |

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
| moves sent | 187569 (150000 while measured, 2500 per second) |
| pings, chat lines, skills sent | 169969, 1699, 3521 (skills answered: 3521) |
| moves delivered to other bots | 125829.9 per second |
| bots that hear a move, on average | 51.2 (the size of the channel the bots were in, the mover included) |
| frames received | 10180794 |
| broadcasts not matched to a move | 41 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 3.44 cores on average, 3.68 at the peak, of 32 |
| memory | 129 MB at most |
| garbage collections | 1754 / 411 / 4 (gen 0 / 1 / 2), 1724 ms paused |
| thread pool | 27 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 16 | 29.6 | 236.7 | 151 | 153 |
| ow-lt-district | docker stats | 16 | 295.1 | 320.7 | 296 | 300 |
| ow-lt-gate | docker stats | 16 | 5.8 | 53.9 | 194 | 196 |
| ow-lt-maze | docker stats | 16 | 14.9 | 18.4 | 240 | 246 |

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
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.077 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.054 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.075 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.293 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.486 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.169 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.923 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.073 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.099 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.035 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.056 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 50.386 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 77.429 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 4580.514 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 318.881 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.736 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 1.75 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.693 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.77 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.008 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 49.25 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.625 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.103 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.099 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 15.014 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 2.741 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.036 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.018 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 9.471 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 11.6 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 4519.371 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 288.71 |
