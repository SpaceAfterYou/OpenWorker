# OpenWorker load test

Started 2026-11-08 19:02:30Z, ran 75.1 s, measured 60.0 s.

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
| auth.login | 250 | 0 | 40.96 | 48.13 | 51.20 | 53.00 |
| auth.gate-list | 250 | 0 | 2.11 | 3.14 | 3.84 | 8.70 |
| gate.enter | 250 | 0 | 3.01 | 4.35 | 5.12 | 9.53 |
| gate.character | 250 | 0 | 1.47 | 2.56 | 3.84 | 8.76 |
| district.enter | 250 | 0 | 6.40 | 11.01 | 14.59 | 18.75 |
| maze.enter | 25 | 0 | 5.63 | 7.68 | 8.16 | 8.16 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 67500 | 0.65 | 0.61 | 1.28 | 1.86 | 4.23 |
| move.delivery | 15120000 | 0.66 | 0.61 | 1.28 | 1.82 | 4.35 |
| ping.after-move | 67500 | 0.76 | 0.67 | 1.44 | 2.18 | 4.23 |
| chat.echo | 675 | 0.65 | 0.62 | 1.18 | 1.54 | 2.78 |
| chat.delivery | 151200 | 0.66 | 0.62 | 1.22 | 1.57 | 2.91 |
| maze.move.echo | 7500 | 0.25 | 0.16 | 0.72 | 1.09 | 2.06 |
| maze.skill.response | 1500 | 0.29 | 0.19 | 0.75 | 1.18 | 2.34 |
| bot.send-lag | 75000 | 0.03 | 0.00 | 0.16 | 0.66 | 4.54 |

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
| moves sent | 90657 (75000 while measured, 1250 per second) |
| pings, chat lines, skills sent | 82178, 821, 1696 (skills answered: 1696) |
| moves delivered to other bots | 251998.4 per second |
| bots that hear a move, on average | 202.5 (the size of the channel the bots were in, the mover included) |
| frames received | 18740821 |
| broadcasts not matched to a move | 41 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 3.02 cores on average, 3.16 at the peak, of 32 |
| memory | 109 MB at most |
| garbage collections | 818 / 3 / 2 (gen 0 / 1 / 2), 742 ms paused |
| thread pool | 32 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 15 | 16.8 | 219.9 | 157 | 160 |
| ow-lt-district | docker stats | 15 | 347.1 | 402.3 | 368 | 369 |
| ow-lt-gate | docker stats | 15 | 2.6 | 36.1 | 203 | 209 |
| ow-lt-maze | docker stats | 15 | 8.1 | 9.2 | 261 | 263 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 1 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0.25 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.069 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.073 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.101 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.058 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.248 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.492 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.488 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.526 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.064 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.094 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.121 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.06 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 40.314 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 39.785 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 2299.861 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 168.257 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.375 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.963 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.38 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.771 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 2.902 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.101 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 49.247 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 4.967 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.487 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 14.622 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 2.008 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.124 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.023 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 7.529 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 6.743 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 2264.747 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 141.643 |
