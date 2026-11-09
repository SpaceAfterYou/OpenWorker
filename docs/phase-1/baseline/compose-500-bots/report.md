# OpenWorker load test

Started 2026-11-08 19:53:21Z, ran 80.1 s, measured 60.0 s.

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
| auth.login | 500 | 0 | 46.08 | 52.22 | 88.06 | 163.51 |
| auth.gate-list | 500 | 0 | 2.62 | 3.97 | 72.75 | 72.75 |
| gate.enter | 500 | 0 | 3.33 | 225.28 | 360.45 | 360.59 |
| gate.character | 500 | 0 | 9.73 | 507.90 | 516.10 | 521.95 |
| district.enter | 500 | 0 | 14.34 | 753.66 | 770.05 | 776.84 |
| maze.enter | 50 | 0 | 6.53 | 163.84 | 586.91 | 586.91 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 135000 | 0.38 | 0.31 | 0.82 | 1.70 | 8.66 |
| move.delivery | 8023800 | 0.38 | 0.31 | 0.82 | 1.70 | 8.76 |
| ping.after-move | 135000 | 0.43 | 0.34 | 0.90 | 1.89 | 8.66 |
| chat.echo | 1350 | 0.40 | 0.34 | 0.83 | 2.11 | 4.14 |
| chat.delivery | 80238 | 0.41 | 0.34 | 0.88 | 2.11 | 4.19 |
| maze.move.echo | 14999 | 0.18 | 0.14 | 0.41 | 0.86 | 3.93 |
| maze.skill.response | 3000 | 0.19 | 0.15 | 0.46 | 0.99 | 5.00 |
| bot.send-lag | 150000 | 0.04 | 0.00 | 0.20 | 0.83 | 8.31 |

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
| moves sent | 187098 (150000 while measured, 2500 per second) |
| pings, chat lines, skills sent | 169548, 1694, 3510 (skills answered: 3510) |
| moves delivered to other bots | 133730.5 per second |
| bots that hear a move, on average | 54.4 (the size of the channel the bots were in, the mover included) |
| frames received | 10661001 |
| broadcasts not matched to a move | 81 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 3.38 cores on average, 3.67 at the peak, of 32 |
| memory | 127 MB at most |
| garbage collections | 1709 / 300 / 3 (gen 0 / 1 / 2), 2003 ms paused |
| thread pool | 33 threads at most, 0 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 16 | 35.6 | 290.1 | 134 | 135 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 16 | 1.3 | 1.5 | 146 | 149 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 16 | 1.2 | 1.6 | 146 | 149 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 16 | 310.7 | 482.0 | 224 | 235 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 16 | 1.2 | 1.6 | 146 | 149 |
| OpenWorker-GateServer | docker stats | 16 | 16.7 | 139.6 | 180 | 183 |
| OpenWorker-Grafana | docker stats | 16 | 1.3 | 2.5 | 146 | 147 |
| OpenWorker-MazeServer | docker stats | 16 | 21.8 | 80.1 | 185 | 193 |
| OpenWorker-PgAdmin | docker stats | 16 | 0.0 | 0.0 | 237 | 237 |
| OpenWorker-Postgres | docker stats | 16 | 9.7 | 80.2 | 194 | 196 |
| OpenWorker-Prometheus | docker stats | 16 | 0.8 | 2.5 | 58 | 60 |
| OpenWorker-RabbitMQ | docker stats | 16 | 43.5 | 365.4 | 126 | 132 |
| OpenWorker-Redis | docker stats | 16 | 24.8 | 53.7 | 144 | 152 |
| OpenWorker-RelayServer | docker stats | 16 | 3.4 | 6.7 | 130 | 132 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.086 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.089 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.055 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.053 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.401 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.494 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.976 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.42 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.086 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.117 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.071 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.039 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 27.2 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 33.814 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 298.367 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 4374.769 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.763 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.391 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.714 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.638 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.052 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.82 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.364 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 70.25 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.861 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.099 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.143 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.206 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.023 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.039 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 5.1 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 5.086 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 261.526 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 4223.924 |
