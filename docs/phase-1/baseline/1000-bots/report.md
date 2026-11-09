# OpenWorker load test

Started 2026-11-08 18:58:50Z, ran 90.1 s, measured 60.0 s.

Bot machine: CachyOS, .NET 10.0.14, 32 cores.

## Scenario

| setting | value |
|---|---|
| bots | 1000 |
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
| asked for | 1000 |
| started | 1000 |
| reached a district | 1000 |
| in the world at the end | 0 (peak 1000) |
| failed before they got in | 0 |
| disconnected after they got in | 0 |
| chosen to fight in a maze | 100 (in a maze at the end: 0) |

## Steps into the world

Time is what the step took for the bots that got through it, in ms.

| step | passed | failed | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| auth.login | 1000 | 0 | 46.08 | 54.27 | 56.32 | 65.77 |
| auth.gate-list | 1000 | 0 | 2.50 | 4.35 | 6.40 | 11.58 |
| gate.enter | 1000 | 0 | 3.39 | 5.25 | 10.75 | 13.09 |
| gate.character | 1000 | 0 | 6.27 | 13.82 | 188.42 | 285.22 |
| district.enter | 1000 | 0 | 8.45 | 19.97 | 43.01 | 277.35 |
| maze.enter | 100 | 0 | 5.63 | 11.01 | 15.10 | 17.48 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270012 | 0.60 | 0.43 | 1.63 | 3.52 | 12.32 |
| move.delivery | 15294655 | 0.60 | 0.43 | 1.63 | 3.52 | 12.49 |
| ping.after-move | 270011 | 0.69 | 0.49 | 1.76 | 4.22 | 12.50 |
| chat.echo | 2700 | 0.61 | 0.43 | 1.60 | 3.58 | 9.89 |
| chat.delivery | 152940 | 0.62 | 0.43 | 1.60 | 3.65 | 10.90 |
| maze.move.echo | 30000 | 0.29 | 0.18 | 0.96 | 1.82 | 5.29 |
| maze.skill.response | 6001 | 0.31 | 0.19 | 1.01 | 1.92 | 4.25 |
| bot.send-lag | 300001 | 0.08 | 0.00 | 0.62 | 1.38 | 11.74 |

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
| moves sent | 400092 (300001 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362387, 3621, 7542 (skills answered: 7542) |
| moves delivered to other bots | 254910.3 per second |
| bots that hear a move, on average | 51.9 (the size of the channel the bots were in, the mover included) |
| frames received | 21959059 |
| broadcasts not matched to a move | 47 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.16 cores on average, 5.79 at the peak, of 32 |
| memory | 169 MB at most |
| garbage collections | 3319 / 790 / 6 (gen 0 / 1 / 2), 5089 ms paused |
| thread pool | 33 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 18 | 58.2 | 279.0 | 153 | 158 |
| ow-lt-district | docker stats | 18 | 421.4 | 466.1 | 343 | 355 |
| ow-lt-gate | docker stats | 18 | 11.0 | 63.5 | 199 | 204 |
| ow-lt-maze | docker stats | 18 | 25.6 | 30.8 | 255 | 259 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 2 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0.333 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.078 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.082 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.411 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.5 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.211 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.023 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.08 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.112 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.038 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.064 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 169.678 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 229.856 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 8521.213 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 555.463 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.609 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 3.421 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.335 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.86 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.431 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 93.682 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 24.076 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.156 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.083 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 4.354 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.047 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.024 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 31.167 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 36.122 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 8291.275 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 513.85 |
