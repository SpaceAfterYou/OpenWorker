# OpenWorker load test

Started 2026-11-08 21:00:50Z, ran 90.1 s, measured 60.0 s.

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
| auth.login | 1000 | 0 | 47.10 | 55.30 | 60.42 | 151.07 |
| auth.gate-list | 1000 | 0 | 2.50 | 4.61 | 12.54 | 61.95 |
| gate.enter | 1000 | 0 | 3.14 | 12.29 | 221.18 | 301.90 |
| gate.character | 1000 | 0 | 8.70 | 20.48 | 466.94 | 487.52 |
| district.enter | 1000 | 0 | 11.01 | 589.82 | 688.13 | 692.82 |
| maze.enter | 100 | 0 | 6.53 | 19.46 | 270.34 | 533.83 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270004 | 0.65 | 0.46 | 1.76 | 3.78 | 12.06 |
| move.delivery | 16729479 | 0.65 | 0.46 | 1.76 | 3.78 | 12.18 |
| ping.after-move | 270004 | 0.75 | 0.53 | 1.92 | 4.48 | 12.06 |
| chat.echo | 2700 | 0.65 | 0.46 | 1.76 | 3.84 | 5.93 |
| chat.delivery | 167292 | 0.66 | 0.46 | 1.79 | 3.90 | 6.70 |
| maze.move.echo | 30000 | 0.33 | 0.20 | 1.09 | 1.89 | 7.45 |
| maze.skill.response | 6000 | 0.34 | 0.20 | 1.22 | 1.92 | 3.69 |
| bot.send-lag | 300005 | 0.08 | 0.00 | 0.62 | 1.31 | 11.71 |

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
| moves sent | 399758 (300005 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362088, 3617, 7535 (skills answered: 7535) |
| moves delivered to other bots | 278821.1 per second |
| bots that hear a move, on average | 56.7 (the size of the channel the bots were in, the mover included) |
| frames received | 23651681 |
| broadcasts not matched to a move | 40 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.14 cores on average, 5.78 at the peak, of 32 |
| memory | 167 MB at most |
| garbage collections | 3276 / 725 / 4 (gen 0 / 1 / 2), 4905 ms paused |
| thread pool | 33 threads at most, 6 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 60.7 | 277.6 | 134 | 138 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 1.3 | 1.5 | 146 | 149 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 1.3 | 1.4 | 146 | 149 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 450.8 | 484.7 | 258 | 275 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 1.2 | 1.5 | 147 | 151 |
| OpenWorker-GateServer | docker stats | 18 | 22.4 | 161.5 | 182 | 187 |
| OpenWorker-Grafana | docker stats | 18 | 1.2 | 2.7 | 143 | 144 |
| OpenWorker-MazeServer | docker stats | 18 | 32.5 | 66.3 | 194 | 205 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 235 | 235 |
| OpenWorker-Postgres | docker stats | 18 | 13.9 | 74.3 | 220 | 224 |
| OpenWorker-Prometheus | docker stats | 18 | 1.1 | 4.1 | 64 | 75 |
| OpenWorker-RabbitMQ | docker stats | 18 | 27.0 | 374.1 | 129 | 188 |
| OpenWorker-Redis | docker stats | 18 | 3.8 | 9.2 | 142 | 143 |
| OpenWorker-RelayServer | docker stats | 18 | 3.8 | 7.6 | 131 | 138 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10003 | 2 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10003 | 0.222 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.085 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10041 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.052 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10031 | 0.06 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10021 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.086 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.5 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.057 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.457 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10041 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.303 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10031 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10021 | 2.389 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.117 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.073 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.088 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10041 | 0.211 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.042 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10031 | 0.229 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10021 | 0.215 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 204.485 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 543.2 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 119.913 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10041 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8287.843 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10031 | 0.462 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10021 | 0.463 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.678 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 3.25 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.334 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.877 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.554 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.884 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 95.197 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.236 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.242 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.023 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.938 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.046 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 30.75 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 498.735 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 22.575 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8033.573 |
| gc pause total (ms) | OpenWorker.RelayServer | 0 |
| gc pause total (ms) | OpenWorker.GateServer | 89.001 |
| gc pause total (ms) | OpenWorker.MazeServer | 100.091 |
| gc pause total (ms) | OpenWorker.AuthServer | 43.2 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10041 | 3.484 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10003 | 1942.764 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10031 | 3.282 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10021 | 4.111 |
| gc collections | gen2, OpenWorker.RelayServer | 0 |
| gc collections | gen1, OpenWorker.RelayServer | 0 |
| gc collections | gen0, OpenWorker.RelayServer | 0 |
| gc collections | gen2, OpenWorker.GateServer | 1.162 |
| gc collections | gen1, OpenWorker.GateServer | 2.325 |
| gc collections | gen0, OpenWorker.GateServer | 33.712 |
| gc collections | gen2, OpenWorker.MazeServer | 1.163 |
| gc collections | gen1, OpenWorker.MazeServer | 3.488 |
| gc collections | gen0, OpenWorker.MazeServer | 24.413 |
| gc collections | gen2, OpenWorker.AuthServer | 0 |
| gc collections | gen1, OpenWorker.AuthServer | 8.138 |
| gc collections | gen0, OpenWorker.AuthServer | 12.788 |
| gc collections | gen2, OpenWorker.DistrictServer, 10041 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10041 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10041 | 4.65 |
| gc collections | gen2, OpenWorker.DistrictServer, 10003 | 1.163 |
| gc collections | gen1, OpenWorker.DistrictServer, 10003 | 19.763 |
| gc collections | gen0, OpenWorker.DistrictServer, 10003 | 492.814 |
| gc collections | gen2, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10031 | 4.65 |
| gc collections | gen2, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10021 | 5.813 |
| gc allocated (MB) | OpenWorker.RelayServer | 0.627 |
| gc allocated (MB) | OpenWorker.GateServer | 601.991 |
| gc allocated (MB) | OpenWorker.MazeServer | 474.771 |
| gc allocated (MB) | OpenWorker.AuthServer | 347.74 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10041 | 83.175 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10003 | 8668.606 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10031 | 83.132 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10021 | 83.167 |
| gc committed max (MB) | OpenWorker.RelayServer | 50.172 |
| gc committed max (MB) | OpenWorker.GateServer | 77.668 |
| gc committed max (MB) | OpenWorker.MazeServer | 101.04 |
| gc committed max (MB) | OpenWorker.AuthServer | 42.856 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10041 | 67.236 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10003 | 159.773 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10031 | 67.236 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10021 | 67.334 |
| working set max (MB) | OpenWorker.RelayServer | 236.753 |
| working set max (MB) | OpenWorker.GateServer | 281.358 |
| working set max (MB) | OpenWorker.MazeServer | 299.93 |
| working set max (MB) | OpenWorker.AuthServer | 230.285 |
| working set max (MB) | OpenWorker.DistrictServer, 10041 | 246.227 |
| working set max (MB) | OpenWorker.DistrictServer, 10003 | 370.254 |
| working set max (MB) | OpenWorker.DistrictServer, 10031 | 246.01 |
| working set max (MB) | OpenWorker.DistrictServer, 10021 | 246.276 |
| cpu time (s) | OpenWorker.RelayServer | 0.555 |
| cpu time (s) | OpenWorker.GateServer | 9.327 |
| cpu time (s) | OpenWorker.MazeServer | 29.675 |
| cpu time (s) | OpenWorker.AuthServer | 39.636 |
| cpu time (s) | OpenWorker.DistrictServer, 10041 | 1.614 |
| cpu time (s) | OpenWorker.DistrictServer, 10003 | 414.261 |
| cpu time (s) | OpenWorker.DistrictServer, 10031 | 1.652 |
| cpu time (s) | OpenWorker.DistrictServer, 10021 | 1.637 |
