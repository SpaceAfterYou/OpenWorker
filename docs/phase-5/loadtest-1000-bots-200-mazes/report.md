# OpenWorker load test

Started 2026-11-09 00:14:41Z, ran 90.1 s, measured 60.0 s.

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
| maze-share | 0.2 |
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
| chosen to fight in a maze | 200 (in a maze at the end: 0) |

## Steps into the world

Time is what the step took for the bots that got through it, in ms.

| step | passed | failed | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| auth.login | 1000 | 0 | 46.08 | 53.25 | 73.73 | 177.97 |
| auth.gate-list | 1000 | 0 | 2.43 | 3.65 | 13.06 | 104.68 |
| gate.enter | 1000 | 0 | 3.01 | 13.06 | 217.09 | 265.71 |
| gate.character | 1000 | 0 | 7.94 | 22.53 | 442.37 | 445.45 |
| district.enter | 1000 | 0 | 8.19 | 450.56 | 557.06 | 567.33 |
| maze.enter | 200 | 0 | 7.55 | 17.92 | 335.87 | 563.56 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 239999 | 0.46 | 0.38 | 1.12 | 1.79 | 11.17 |
| move.delivery | 14612916 | 0.48 | 0.38 | 1.12 | 1.79 | 11.31 |
| ping.after-move | 239999 | 0.40 | 0.33 | 0.93 | 1.70 | 11.17 |
| chat.echo | 2400 | 0.43 | 0.35 | 0.99 | 1.76 | 3.22 |
| chat.delivery | 146130 | 0.43 | 0.35 | 0.99 | 1.76 | 5.28 |
| maze.move.echo | 60000 | 0.35 | 0.17 | 1.47 | 3.07 | 9.76 |
| maze.skill.response | 12000 | 0.35 | 0.17 | 1.50 | 2.82 | 4.62 |
| bot.send-lag | 299997 | 0.07 | 0.00 | 0.56 | 1.25 | 10.91 |

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
| moves sent | 399827 (299997 while measured, 5000 per second) |
| pings, chat lines, skills sent | 324442, 3239, 15078 (skills answered: 15078) |
| moves delivered to other bots | 243550.2 per second |
| bots that hear a move, on average | 49.5 (the size of the channel the bots were in, the mover included) |
| frames received | 21058674 |
| broadcasts not matched to a move | 74 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.16 cores on average, 5.80 at the peak, of 32 |
| memory | 167 MB at most |
| garbage collections | 3429 / 782 / 5 (gen 0 / 1 / 2), 4771 ms paused |
| thread pool | 32 threads at most, 1 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 61.6 | 307.9 | 157 | 162 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 0.4 | 3.4 | 186 | 190 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 0.2 | 1.4 | 192 | 194 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 367.9 | 411.3 | 285 | 306 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 0.1 | 0.3 | 178 | 180 |
| OpenWorker-GateServer | docker stats | 18 | 22.8 | 153.4 | 273 | 279 |
| OpenWorker-Grafana | docker stats | 18 | 1.2 | 2.6 | 149 | 150 |
| OpenWorker-MazeServer | docker stats | 18 | 51.5 | 102.5 | 266 | 284 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 241 | 241 |
| OpenWorker-Postgres | docker stats | 18 | 12.5 | 61.5 | 160 | 165 |
| OpenWorker-Prometheus | docker stats | 18 | 1.0 | 2.6 | 104 | 107 |
| OpenWorker-RabbitMQ | docker stats | 18 | 32.1 | 371.0 | 137 | 194 |
| OpenWorker-Redis | docker stats | 18 | 1.5 | 5.3 | 154 | 155 |
| OpenWorker-RelayServer | docker stats | 18 | 3.4 | 6.8 | 149 | 154 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.051 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.088 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.052 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.081 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.493 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 3.09 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.218 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.079 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.018 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.109 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.065 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 123.213 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 7445.103 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 178.527 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 1061.97 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 3.329 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 4.979 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 2.488 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.801 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.418 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.099 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.818 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.099 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 92.516 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 16.55 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.011 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.097 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.019 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 23.2 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 7202.636 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 29.188 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 998.844 |
