# OpenWorker load test

Started 2026-11-08 21:03:41Z, ran 90.1 s, measured 60.0 s.

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
| auth.login | 1000 | 0 | 47.10 | 54.27 | 58.37 | 160.50 |
| auth.gate-list | 1000 | 0 | 2.50 | 3.78 | 12.54 | 58.04 |
| gate.enter | 1000 | 0 | 3.14 | 12.03 | 221.18 | 296.33 |
| gate.character | 1000 | 0 | 8.70 | 17.92 | 483.33 | 504.40 |
| district.enter | 1000 | 0 | 10.24 | 573.44 | 688.13 | 694.83 |
| maze.enter | 100 | 0 | 5.76 | 16.38 | 237.57 | 504.20 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 270007 | 0.58 | 0.45 | 1.54 | 2.37 | 6.86 |
| move.delivery | 16727852 | 0.59 | 0.45 | 1.54 | 2.37 | 11.06 |
| ping.after-move | 270007 | 0.68 | 0.53 | 1.73 | 2.62 | 6.86 |
| chat.echo | 2700 | 0.60 | 0.46 | 1.50 | 2.56 | 5.79 |
| chat.delivery | 167274 | 0.60 | 0.46 | 1.47 | 2.56 | 5.80 |
| maze.move.echo | 30001 | 0.32 | 0.19 | 1.02 | 1.89 | 6.38 |
| maze.skill.response | 6000 | 0.35 | 0.21 | 1.22 | 1.95 | 6.36 |
| bot.send-lag | 300003 | 0.08 | 0.00 | 0.62 | 1.31 | 10.20 |

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
| moves sent | 399759 (300003 while measured, 5000 per second) |
| pings, chat lines, skills sent | 362085, 3616, 7535 (skills answered: 7535) |
| moves delivered to other bots | 278795.7 per second |
| bots that hear a move, on average | 56.7 (the size of the channel the bots were in, the mover included) |
| frames received | 23648772 |
| broadcasts not matched to a move | 91 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 5.25 cores on average, 5.89 at the peak, of 32 |
| memory | 167 MB at most |
| garbage collections | 3290 / 745 / 5 (gen 0 / 1 / 2), 4933 ms paused |
| thread pool | 44 threads at most, 7 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| OpenWorker-AuthServer | docker stats | 18 | 61.0 | 278.0 | 162 | 166 |
| OpenWorker-DistrictServer-CandusCity | docker stats | 18 | 1.3 | 1.4 | 194 | 203 |
| OpenWorker-DistrictServer-GraceCity | docker stats | 18 | 1.7 | 4.5 | 207 | 215 |
| OpenWorker-DistrictServer-RoccoTown | docker stats | 18 | 433.8 | 488.7 | 297 | 400 |
| OpenWorker-DistrictServer-RuinFortress | docker stats | 18 | 1.3 | 1.4 | 197 | 206 |
| OpenWorker-GateServer | docker stats | 18 | 22.3 | 163.6 | 270 | 277 |
| OpenWorker-Grafana | docker stats | 18 | 1.4 | 2.7 | 142 | 143 |
| OpenWorker-MazeServer | docker stats | 18 | 33.0 | 70.6 | 247 | 271 |
| OpenWorker-PgAdmin | docker stats | 18 | 0.0 | 0.0 | 235 | 235 |
| OpenWorker-Postgres | docker stats | 18 | 14.3 | 74.4 | 214 | 217 |
| OpenWorker-Prometheus | docker stats | 18 | 1.0 | 2.7 | 74 | 79 |
| OpenWorker-RabbitMQ | docker stats | 18 | 25.9 | 381.3 | 127 | 202 |
| OpenWorker-Redis | docker stats | 18 | 3.5 | 6.7 | 139 | 142 |
| OpenWorker-RelayServer | docker stats | 18 | 3.7 | 7.0 | 144 | 151 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10003 | 5 |
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10031 | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10003 | 0.889 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10041 | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer, 10021 | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.086 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10021 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.053 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10031 | 0.058 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.052 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.086 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10041 | 0.058 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10031 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.264 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.443 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10041 | 4.075 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.549 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10021 | 2.389 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 2.067 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.077 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10031 | 0.207 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.043 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.087 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10041 | 0.297 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.114 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10021 | 0.203 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 542.904 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10031 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8286.418 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 119.363 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10041 | 0.463 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 197.775 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer, 10021 | 0.462 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 1.697 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 3.158 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 1.422 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer, 10003 | 0.051 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 1.847 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.538 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer, 10003 | 0.228 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 96.07 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 9.818 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.024 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer, 10003 | 0.046 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 17.096 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 3.225 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 498.614 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer, 10003 | 8031.74 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 22.5 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 29.8 |
| gc pause total (ms) | OpenWorker.RelayServer | 0 |
| gc pause total (ms) | OpenWorker.MazeServer | 35.954 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10031 | 1.044 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10003 | 1007.723 |
| gc pause total (ms) | OpenWorker.AuthServer | 22.168 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10041 | 12.563 |
| gc pause total (ms) | OpenWorker.GateServer | 18.43 |
| gc pause total (ms) | OpenWorker.DistrictServer, 10021 | 9.486 |
| gc collections | gen2, OpenWorker.RelayServer | 0 |
| gc collections | gen1, OpenWorker.RelayServer | 0 |
| gc collections | gen0, OpenWorker.RelayServer | 0 |
| gc collections | gen2, OpenWorker.MazeServer | 1.163 |
| gc collections | gen1, OpenWorker.MazeServer | 2.325 |
| gc collections | gen0, OpenWorker.MazeServer | 5.813 |
| gc collections | gen2, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10031 | 0 |
| gc collections | gen0, OpenWorker.DistrictServer, 10031 | 1.163 |
| gc collections | gen2, OpenWorker.DistrictServer, 10003 | 4.65 |
| gc collections | gen1, OpenWorker.DistrictServer, 10003 | 4.65 |
| gc collections | gen0, OpenWorker.DistrictServer, 10003 | 726.192 |
| gc collections | gen2, OpenWorker.AuthServer | 0 |
| gc collections | gen1, OpenWorker.AuthServer | 0 |
| gc collections | gen0, OpenWorker.AuthServer | 9.3 |
| gc collections | gen2, OpenWorker.DistrictServer, 10041 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10041 | 1.163 |
| gc collections | gen0, OpenWorker.DistrictServer, 10041 | 1.163 |
| gc collections | gen2, OpenWorker.GateServer | 1.163 |
| gc collections | gen1, OpenWorker.GateServer | 0 |
| gc collections | gen0, OpenWorker.GateServer | 16.275 |
| gc collections | gen2, OpenWorker.DistrictServer, 10021 | 0 |
| gc collections | gen1, OpenWorker.DistrictServer, 10021 | 1.162 |
| gc collections | gen0, OpenWorker.DistrictServer, 10021 | 0 |
| gc allocated (MB) | OpenWorker.RelayServer | 0.519 |
| gc allocated (MB) | OpenWorker.MazeServer | 473.673 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10031 | 83.128 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10003 | 7157.749 |
| gc allocated (MB) | OpenWorker.AuthServer | 347.164 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10041 | 83.116 |
| gc allocated (MB) | OpenWorker.GateServer | 585.196 |
| gc allocated (MB) | OpenWorker.DistrictServer, 10021 | 83.126 |
| gc committed max (MB) | OpenWorker.RelayServer | 57.475 |
| gc committed max (MB) | OpenWorker.MazeServer | 160.952 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10031 | 123.347 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10003 | 279.724 |
| gc committed max (MB) | OpenWorker.AuthServer | 68.08 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10041 | 118.075 |
| gc committed max (MB) | OpenWorker.GateServer | 154.44 |
| gc committed max (MB) | OpenWorker.DistrictServer, 10021 | 114.618 |
| working set max (MB) | OpenWorker.RelayServer | 249.557 |
| working set max (MB) | OpenWorker.MazeServer | 368.542 |
| working set max (MB) | OpenWorker.DistrictServer, 10031 | 312.832 |
| working set max (MB) | OpenWorker.DistrictServer, 10003 | 493.318 |
| working set max (MB) | OpenWorker.AuthServer | 261.39 |
| working set max (MB) | OpenWorker.DistrictServer, 10041 | 302.486 |
| working set max (MB) | OpenWorker.GateServer | 374.133 |
| working set max (MB) | OpenWorker.DistrictServer, 10021 | 298.361 |
| cpu time (s) | OpenWorker.RelayServer | 0.559 |
| cpu time (s) | OpenWorker.MazeServer | 29.873 |
| cpu time (s) | OpenWorker.DistrictServer, 10031 | 1.595 |
| cpu time (s) | OpenWorker.DistrictServer, 10003 | 413.221 |
| cpu time (s) | OpenWorker.AuthServer | 39.847 |
| cpu time (s) | OpenWorker.DistrictServer, 10041 | 1.677 |
| cpu time (s) | OpenWorker.GateServer | 9.417 |
| cpu time (s) | OpenWorker.DistrictServer, 10021 | 1.672 |
