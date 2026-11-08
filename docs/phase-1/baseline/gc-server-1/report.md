# OpenWorker load test

Started 2026-11-08 20:57:40Z, ran 90.1 s, measured 60.0 s.

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
| auth.login | 1000 | 0 | 47.10 | 55.30 | 59.39 | 139.47 |
| auth.gate-list | 1000 | 0 | 2.56 | 4.48 | 9.98 | 58.79 |
| gate.enter | 1000 | 0 | 3.20 | 7.42 | 225.28 | 298.14 |
| gate.character | 1000 | 0 | 8.70 | 19.97 | 466.94 | 483.51 |
| district.enter | 1000 | 0 | 10.75 | 540.67 | 655.36 | 663.15 |
| maze.enter | 100 | 0 | 5.76 | 18.94 | 204.80 | 512.39 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270000 | 0.63 | 0.47 | 1.73 | 2.75 | 12.50 |
| move.delivery | 16726800 | 0.64 | 0.47 | 1.73 | 2.75 | 12.55 |
| ping.after-move | 270000 | 0.74 | 0.54 | 1.92 | 3.20 | 12.54 |
| chat.echo | 2700 | 0.65 | 0.50 | 1.73 | 2.62 | 5.09 |
| chat.delivery | 167268 | 0.65 | 0.50 | 1.73 | 2.69 | 5.58 |
| maze.move.echo | 30000 | 0.35 | 0.21 | 1.22 | 2.05 | 4.44 |
| maze.skill.response | 6000 | 0.33 | 0.21 | 1.02 | 1.89 | 3.34 |
| bot.send-lag | 299999 | 0.09 | 0.00 | 0.64 | 1.38 | 11.80 |

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
| moves sent | 399798 (299999 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362121, 3617, 7535 (skills answered: 7535) |
| moves delivered to other bots | 278781.2 per second |
| bots that hear a move, on average | 56.7 (the size of the channel the bots were in, the mover included) |
| frames received | 23650453 |
| broadcasts not matched to a move | 50 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.15 cores on average, 5.75 at the peak, of 32 |
| memory | 167 MB at most |
| garbage collections | 3271 / 721 / 4 (gen 0 / 1 / 2), 5028 ms paused |
| thread pool | 34 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 60.5 | 280.5 | 159 | 165 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 1.3 | 1.5 | 195 | 199 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 1.3 | 1.5 | 181 | 184 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 453.8 | 489.2 | 288 | 377 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 2.4 | 7.0 | 181 | 188 |
| OpenWorker-GateServer | docker stats | 18 | 22.3 | 160.9 | 236 | 246 |
| OpenWorker-Grafana | docker stats | 18 | 1.4 | 2.7 | 143 | 144 |
| OpenWorker-MazeServer | docker stats | 18 | 32.6 | 60.4 | 244 | 266 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 235 | 235 |
| OpenWorker-Postgres | docker stats | 18 | 14.3 | 70.3 | 208 | 211 |
| OpenWorker-Prometheus | docker stats | 18 | 0.9 | 2.7 | 62 | 63 |
| OpenWorker-RabbitMQ | docker stats | 18 | 26.7 | 368.1 | 125 | 208 |
| OpenWorker-Redis | docker stats | 18 | 3.0 | 7.3 | 142 | 143 |
| OpenWorker-RelayServer | docker stats | 18 | 3.7 | 7.4 | 151 | 158 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10003 | 2 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10003 | 0.333 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10041 | 0.06 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.088 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.052 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10031 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10021 | 0.062 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.086 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10041 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.499 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.315 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10031 | 4.075 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10021 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.453 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.054 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.089 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.076 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10041 | 0.206 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.116 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.042 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10031 | 0.257 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10021 | 0.223 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 125.638 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 543.024 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10041 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 204.497 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8287.923 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10031 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10021 | 0.463 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.674 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.494 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.376 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.878 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.611 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.051 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.835 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.243 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 95.698 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.945 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.025 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.276 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.046 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 23.663 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 498.588 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 30.762 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8033.324 |
| gc pause total (ms) | OpenWorker.RelayServer | 0 |
| gc pause total (ms) | OpenWorker.AuthServer | 24.503 |
| gc pause total (ms) | OpenWorker.MazeServer | 23.877 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10041 | 5.766 |
| gc pause total (ms) | OpenWorker.GateServer | 27.416 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10003 | 1548.356 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10031 | 5.055 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10021 | 1.468 |
| gc collections | gen2, OpenWorker.RelayServer | 0 |
| gc collections | gen1, OpenWorker.RelayServer | 0 |
| gc collections | gen0, OpenWorker.RelayServer | 0 |
| gc collections | gen2, OpenWorker.AuthServer | 0 |
| gc collections | gen1, OpenWorker.AuthServer | 0 |
| gc collections | gen0, OpenWorker.AuthServer | 9.3 |
| gc collections | gen2, OpenWorker.MazeServer | 1.163 |
| gc collections | gen1, OpenWorker.MazeServer | 1.163 |
| gc collections | gen0, OpenWorker.MazeServer | 5.813 |
| gc collections | gen2, OpenWorker.DistrictServer, 10041 | 1.163 |
| gc collections | gen1, OpenWorker.DistrictServer, 10041 | 1.163 |
| gc collections | gen0, OpenWorker.DistrictServer, 10041 | 1.163 |
| gc collections | gen2, OpenWorker.GateServer | 1.162 |
| gc collections | gen1, OpenWorker.GateServer | 1.162 |
| gc collections | gen0, OpenWorker.GateServer | 18.6 |
| gc collections | gen2, OpenWorker.DistrictServer, 10003 | 3.488 |
| gc collections | gen1, OpenWorker.DistrictServer, 10003 | 146.831 |
| gc collections | gen0, OpenWorker.DistrictServer, 10003 | 803.596 |
| gc collections | gen2, OpenWorker.DistrictServer, 10031 | 1.163 |
| gc collections | gen1, OpenWorker.DistrictServer, 10031 | 1.163 |
| gc collections | gen0, OpenWorker.DistrictServer, 10031 | 1.163 |
| gc collections | gen2, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10021 | 1.163 |
| gc allocated (MB) | OpenWorker.RelayServer | 0.512 |
| gc allocated (MB) | OpenWorker.AuthServer | 350.885 |
| gc allocated (MB) | OpenWorker.MazeServer | 473.712 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10041 | 83.373 |
| gc allocated (MB) | OpenWorker.GateServer | 421.628 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10003 | 7061.446 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10031 | 83.221 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10021 | 83.128 |
| gc committed max (MB) | OpenWorker.RelayServer | 62.96 |
| gc committed max (MB) | OpenWorker.AuthServer | 70.046 |
| gc committed max (MB) | OpenWorker.MazeServer | 151.024 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10041 | 98.607 |
| gc committed max (MB) | OpenWorker.GateServer | 122.38 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10003 | 224.244 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10031 | 95.15 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10021 | 110.936 |
| working set max (MB) | OpenWorker.RelayServer | 257.782 |
| working set max (MB) | OpenWorker.AuthServer | 261.788 |
| working set max (MB) | OpenWorker.MazeServer | 359.723 |
| working set max (MB) | OpenWorker.DistrictServer, 10041 | 280.74 |
| working set max (MB) | OpenWorker.GateServer | 339.173 |
| working set max (MB) | OpenWorker.DistrictServer, 10003 | 440.328 |
| working set max (MB) | OpenWorker.DistrictServer, 10031 | 279.114 |
| working set max (MB) | OpenWorker.DistrictServer, 10021 | 297.103 |
| cpu time (s) | OpenWorker.RelayServer | 0.567 |
| cpu time (s) | OpenWorker.AuthServer | 41.449 |
| cpu time (s) | OpenWorker.MazeServer | 30.175 |
| cpu time (s) | OpenWorker.DistrictServer, 10041 | 1.691 |
| cpu time (s) | OpenWorker.GateServer | 9.36 |
| cpu time (s) | OpenWorker.DistrictServer, 10003 | 416.905 |
| cpu time (s) | OpenWorker.DistrictServer, 10031 | 1.733 |
| cpu time (s) | OpenWorker.DistrictServer, 10021 | 1.651 |
