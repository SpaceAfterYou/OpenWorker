# OpenWorker load test

Started 2026-11-08 21:56:31Z, ran 90.1 s, measured 60.0 s.

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
| auth.login | 1000 | 0 | 46.08 | 54.27 | 60.42 | 151.16 |
| auth.gate-list | 1000 | 0 | 2.50 | 4.35 | 10.75 | 59.86 |
| gate.enter | 1000 | 0 | 3.07 | 14.34 | 229.38 | 317.10 |
| gate.character | 1000 | 0 | 8.70 | 112.64 | 491.52 | 513.20 |
| district.enter | 1000 | 0 | 9.73 | 557.06 | 671.74 | 672.84 |
| maze.enter | 100 | 0 | 5.89 | 15.10 | 200.70 | 461.78 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270001 | 0.55 | 0.43 | 1.44 | 2.18 | 10.88 |
| move.delivery | 16727455 | 0.56 | 0.43 | 1.44 | 2.18 | 10.94 |
| ping.after-move | 270001 | 0.62 | 0.49 | 1.57 | 2.30 | 10.88 |
| chat.echo | 2700 | 0.55 | 0.44 | 1.44 | 2.05 | 4.06 |
| chat.delivery | 167274 | 0.56 | 0.45 | 1.44 | 2.11 | 4.12 |
| maze.move.echo | 30001 | 0.31 | 0.20 | 0.90 | 1.79 | 4.16 |
| maze.skill.response | 6000 | 0.33 | 0.20 | 1.01 | 1.92 | 4.13 |
| bot.send-lag | 300001 | 0.09 | 0.00 | 0.64 | 1.38 | 10.89 |

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
| moves sent | 399782 (300001 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362110, 3617, 7535 (skills answered: 7535) |
| moves delivered to other bots | 278789.4 per second |
| bots that hear a move, on average | 56.7 (the size of the channel the bots were in, the mover included) |
| frames received | 23649742 |
| broadcasts not matched to a move | 42 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.22 cores on average, 5.91 at the peak, of 32 |
| memory | 166 MB at most |
| garbage collections | 3278 / 731 / 5 (gen 0 / 1 / 2), 5061 ms paused |
| thread pool | 47 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 59.8 | 275.6 | 157 | 163 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 1.2 | 1.4 | 182 | 188 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 1.4 | 2.2 | 205 | 212 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 407.0 | 444.7 | 288 | 301 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 1.3 | 1.7 | 202 | 207 |
| OpenWorker-GateServer | docker stats | 18 | 22.3 | 162.6 | 264 | 273 |
| OpenWorker-Grafana | docker stats | 18 | 1.4 | 2.7 | 141 | 144 |
| OpenWorker-MazeServer | docker stats | 18 | 32.2 | 57.3 | 246 | 264 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 235 | 235 |
| OpenWorker-Postgres | docker stats | 18 | 15.4 | 100.8 | 299 | 302 |
| OpenWorker-Prometheus | docker stats | 18 | 1.0 | 4.3 | 70 | 73 |
| OpenWorker-RabbitMQ | docker stats | 18 | 28.2 | 372.1 | 124 | 185 |
| OpenWorker-Redis | docker stats | 18 | 3.4 | 10.7 | 141 | 142 |
| OpenWorker-RelayServer | docker stats | 18 | 3.8 | 9.8 | 144 | 150 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 1 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0.028 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.084 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.087 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.048 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.191 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.5 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.436 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.086 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.073 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.017 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.114 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 122.238 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 543.199 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 8289.604 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 196.535 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.677 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.491 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.337 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.486 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.85 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 95.022 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 15.541 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.099 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.777 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.023 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 4.121 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.021 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 23.025 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 498.742 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 29.65 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 8034.619 |
