# OpenWorker load test

Started 2026-11-08 20:54:22Z, ran 90.1 s, measured 60.0 s.

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
| auth.login | 1000 | 0 | 47.10 | 55.30 | 60.42 | 145.19 |
| auth.gate-list | 1000 | 0 | 2.56 | 4.86 | 13.31 | 56.23 |
| gate.enter | 1000 | 0 | 3.20 | 12.29 | 229.38 | 291.75 |
| gate.character | 1000 | 0 | 8.96 | 19.46 | 475.14 | 488.32 |
| district.enter | 1000 | 0 | 11.01 | 573.44 | 688.13 | 702.84 |
| maze.enter | 100 | 0 | 6.53 | 19.46 | 286.72 | 544.20 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270002 | 0.74 | 0.53 | 2.02 | 4.35 | 14.96 |
| move.delivery | 16729348 | 0.74 | 0.53 | 2.02 | 4.35 | 14.98 |
| ping.after-move | 270002 | 0.86 | 0.61 | 2.24 | 4.99 | 14.96 |
| chat.echo | 2700 | 0.74 | 0.53 | 2.02 | 4.03 | 14.44 |
| chat.delivery | 167292 | 0.75 | 0.54 | 2.02 | 4.35 | 14.75 |
| maze.move.echo | 30000 | 0.39 | 0.22 | 1.41 | 2.30 | 10.83 |
| maze.skill.response | 6000 | 0.38 | 0.23 | 1.22 | 2.11 | 14.19 |
| bot.send-lag | 300002 | 0.10 | 0.00 | 0.67 | 1.41 | 13.06 |

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
| moves sent | 399774 (300002 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362102, 3617, 7535 (skills answered: 7535) |
| moves delivered to other bots | 278819.8 per second |
| bots that hear a move, on average | 56.7 (the size of the channel the bots were in, the mover included) |
| frames received | 23652205 |
| broadcasts not matched to a move | 0 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 4.91 cores on average, 5.59 at the peak, of 32 |
| memory | 168 MB at most |
| garbage collections | 3192 / 700 / 4 (gen 0 / 1 / 2), 4984 ms paused |
| thread pool | 35 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 61.5 | 275.6 | 135 | 136 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 1.3 | 1.6 | 146 | 151 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 1.3 | 1.4 | 146 | 149 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 442.2 | 478.1 | 262 | 282 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 1.3 | 1.5 | 149 | 152 |
| OpenWorker-GateServer | docker stats | 18 | 22.4 | 165.9 | 181 | 186 |
| OpenWorker-Grafana | docker stats | 18 | 1.4 | 2.8 | 147 | 148 |
| OpenWorker-MazeServer | docker stats | 18 | 32.1 | 61.0 | 194 | 205 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 235 | 235 |
| OpenWorker-Postgres | docker stats | 18 | 14.8 | 82.5 | 248 | 251 |
| OpenWorker-Prometheus | docker stats | 18 | 0.6 | 2.7 | 63 | 69 |
| OpenWorker-RabbitMQ | docker stats | 18 | 26.0 | 374.8 | 130 | 215 |
| OpenWorker-Redis | docker stats | 18 | 3.1 | 7.5 | 140 | 149 |
| OpenWorker-RelayServer | docker stats | 18 | 3.7 | 7.1 | 130 | 134 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10003 | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10003 | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.052 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.088 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10021 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.087 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10041 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10031 | 0.058 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.086 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.291 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.5 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10021 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.474 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10041 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10031 | 2.389 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.08 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.042 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.12 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10021 | 0.239 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.092 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10041 | 0.22 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10031 | 0.206 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 542.938 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8286.038 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 201.635 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10021 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 119.138 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10041 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10031 | 0.462 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.7 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 4.621 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.474 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.58 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.896 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.23 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.843 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 95.439 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.024 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.046 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.313 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 17.031 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 498.617 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8031.641 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 30.4 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 22.463 |
| gc pause total (ms) | OpenWorker.RelayServer | 0 |
| gc pause total (ms) | OpenWorker.MazeServer | 110.851 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10003 | 2000.378 |
| gc pause total (ms) | OpenWorker.GateServer | 80.46 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10021 | 5.101 |
| gc pause total (ms) | OpenWorker.AuthServer | 42.252 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10041 | 3.671 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10031 | 3.351 |
| gc collections | gen2, OpenWorker.RelayServer | 0 |
| gc collections | gen1, OpenWorker.RelayServer | 0 |
| gc collections | gen0, OpenWorker.RelayServer | 0 |
| gc collections | gen2, OpenWorker.MazeServer | 1.163 |
| gc collections | gen1, OpenWorker.MazeServer | 4.65 |
| gc collections | gen0, OpenWorker.MazeServer | 23.25 |
| gc collections | gen2, OpenWorker.DistrictServer, 10003 | 1.163 |
| gc collections | gen1, OpenWorker.DistrictServer, 10003 | 20.925 |
| gc collections | gen0, OpenWorker.DistrictServer, 10003 | 493.612 |
| gc collections | gen2, OpenWorker.GateServer | 0 |
| gc collections | gen1, OpenWorker.GateServer | 3.487 |
| gc collections | gen0, OpenWorker.GateServer | 31.387 |
| gc collections | gen2, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10021 | 5.813 |
| gc collections | gen2, OpenWorker.AuthServer | 0 |
| gc collections | gen1, OpenWorker.AuthServer | 8.138 |
| gc collections | gen0, OpenWorker.AuthServer | 12.788 |
| gc collections | gen2, OpenWorker.DistrictServer, 10041 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10041 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10041 | 4.65 |
| gc collections | gen2, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10031 | 4.65 |
| gc allocated (MB) | OpenWorker.RelayServer | 0.555 |
| gc allocated (MB) | OpenWorker.MazeServer | 473.682 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10003 | 8673.429 |
| gc allocated (MB) | OpenWorker.GateServer | 595.024 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10021 | 83.193 |
| gc allocated (MB) | OpenWorker.AuthServer | 347.183 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10041 | 83.154 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10031 | 83.165 |
| gc committed max (MB) | OpenWorker.RelayServer | 50.106 |
| gc committed max (MB) | OpenWorker.MazeServer | 101.183 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10003 | 163.701 |
| gc committed max (MB) | OpenWorker.GateServer | 77.472 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10021 | 67.236 |
| gc committed max (MB) | OpenWorker.AuthServer | 42.918 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10041 | 67.236 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10031 | 67.236 |
| working set max (MB) | OpenWorker.RelayServer | 233.361 |
| working set max (MB) | OpenWorker.MazeServer | 300.532 |
| working set max (MB) | OpenWorker.DistrictServer, 10003 | 377.356 |
| working set max (MB) | OpenWorker.GateServer | 280.334 |
| working set max (MB) | OpenWorker.DistrictServer, 10021 | 246.788 |
| working set max (MB) | OpenWorker.AuthServer | 232.706 |
| working set max (MB) | OpenWorker.DistrictServer, 10041 | 246.723 |
| working set max (MB) | OpenWorker.DistrictServer, 10031 | 246.465 |
| cpu time (s) | OpenWorker.RelayServer | 0.558 |
| cpu time (s) | OpenWorker.MazeServer | 30.275 |
| cpu time (s) | OpenWorker.DistrictServer, 10003 | 404.966 |
| cpu time (s) | OpenWorker.GateServer | 9.743 |
| cpu time (s) | OpenWorker.DistrictServer, 10021 | 1.613 |
| cpu time (s) | OpenWorker.AuthServer | 39.587 |
| cpu time (s) | OpenWorker.DistrictServer, 10041 | 1.643 |
| cpu time (s) | OpenWorker.DistrictServer, 10031 | 1.64 |
