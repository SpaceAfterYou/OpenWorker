# OpenWorker load test

Started 2026-11-08 19:04:02Z, ran 82.4 s, measured 60.0 s.

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
| auth.login | 500 | 0 | 49.15 | 69.63 | 77.82 | 88.54 |
| auth.gate-list | 500 | 0 | 2.82 | 11.01 | 16.38 | 20.02 |
| gate.enter | 500 | 0 | 3.78 | 10.75 | 13.82 | 21.11 |
| gate.character | 500 | 0 | 1.92 | 10.75 | 14.85 | 18.11 |
| district.enter | 500 | 0 | 9.73 | 1966.08 | 2281.13 | 2281.13 |
| maze.enter | 50 | 0 | 499.71 | 1343.49 | 1512.79 | 1512.79 |

## Latency in the measured time (ms)

| what | samples | mean | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|---:|
| move.echo | 121038 | 4579.61 | 4718.59 | 7471.10 | 7733.25 | 7911.56 |
| move.delivery | 54344908 | 4579.60 | 4718.59 | 7471.10 | 7733.25 | 7914.92 |
| ping.after-move | 121040 | 4679.03 | 4718.59 | 7602.18 | 7864.32 | 8005.54 |
| chat.echo | 1203 | 4626.99 | 4587.52 | 7602.18 | 7864.32 | 7935.59 |
| chat.delivery | 540147 | 4626.99 | 4587.52 | 7602.18 | 7864.32 | 7936.94 |
| maze.move.echo | 15002 | 2.88 | 2.43 | 7.17 | 10.50 | 16.63 |
| maze.skill.response | 3000 | 2.82 | 2.30 | 6.91 | 10.50 | 16.78 |
| bot.send-lag | 150001 | 0.42 | 0.00 | 1.98 | 3.52 | 11.99 |

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
| moves sent | 192603 (150001 while measured, 2500 per second) |
| pings, chat lines, skills sent | 174627, 1745, 3596 (skills answered: 3596) |
| moves delivered to other bots | 905738.6 per second |
| bots that hear a move, on average | 363.1 (the size of the channel the bots were in, the mover included) |
| frames received | 70415569 |
| broadcasts not matched to a move | 41 |

## Errors

None.

## The bot itself

| | |
|---|---|
| cpu | 3.68 cores on average, 4.12 at the peak, of 32 |
| memory | 185 MB at most |
| garbage collections | 927 / 4 / 2 (gen 0 / 1 / 2), 1915 ms paused |
| thread pool | 35 threads at most, 7 items waiting at most |

## Servers

CPU is in percent of one core.

| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |
|---|---|---:|---:|---:|---:|---:|
| ow-lt-auth | docker stats | 17 | 33.0 | 291.2 | 159 | 162 |
| ow-lt-district | docker stats | 17 | 834.6 | 941.4 | 377 | 386 |
| ow-lt-gate | docker stats | 17 | 4.6 | 39.8 | 205 | 208 |
| ow-lt-maze | docker stats | 17 | 11.0 | 13.5 | 268 | 274 |

## Loop and handlers (Prometheus)

From http://127.0.0.1:9090, over the length of the run, in the unit of each metric.

| query | server | value |
|---|---|---:|
| worldloop_queue_length (max) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (max) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (max) | OpenWorker.DistrictServer | 473 |
| worldloop_queue_length (max) | OpenWorker.MazeServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.AuthServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.GateServer | 0 |
| worldloop_queue_length (mean) | OpenWorker.DistrictServer | 400.889 |
| worldloop_queue_length (mean) | OpenWorker.MazeServer | 0 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.055 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.AuthServer | 0.091 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.GateServer | 0.077 |
| worldloop_callback_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.109 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.GateServer | 0.813 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.982 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.993 |
| worldloop_callback_duration_seconds (p99, ms) | OpenWorker.AuthServer | 0.926 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.AuthServer | 0.107 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.GateServer | 0.11 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.238 |
| worldloop_callback_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.078 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.AuthServer | 10.686 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.GateServer | 111.199 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.DistrictServer | 3980.425 |
| worldloop_callback_duration_seconds (per second) | OpenWorker.MazeServer | 294.83 |
| simulation_step_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.743 |
| simulation_step_duration_seconds (p99, ms) | OpenWorker.MazeServer | 3.981 |
| simulation_step_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.848 |
| simulation_step_duration_seconds (per second) | OpenWorker.MazeServer | 20 |
| handler_duration_seconds (p50, ms) | OpenWorker.AuthServer | 4.095 |
| handler_duration_seconds (p50, ms) | OpenWorker.GateServer | 3.038 |
| handler_duration_seconds (p50, ms) | OpenWorker.DistrictServer | 0.125 |
| handler_duration_seconds (p50, ms) | OpenWorker.MazeServer | 0.05 |
| handler_duration_seconds (p99, ms) | OpenWorker.AuthServer | 98.479 |
| handler_duration_seconds (p99, ms) | OpenWorker.GateServer | 20 |
| handler_duration_seconds (p99, ms) | OpenWorker.DistrictServer | 0.986 |
| handler_duration_seconds (p99, ms) | OpenWorker.MazeServer | 0.1 |
| handler_duration_seconds (mean, ms) | OpenWorker.AuthServer | 22.862 |
| handler_duration_seconds (mean, ms) | OpenWorker.GateServer | 2.809 |
| handler_duration_seconds (mean, ms) | OpenWorker.DistrictServer | 0.579 |
| handler_duration_seconds (mean, ms) | OpenWorker.MazeServer | 0.045 |
| handler_duration_seconds (per second) | OpenWorker.AuthServer | 2.043 |
| handler_duration_seconds (per second) | OpenWorker.GateServer | 18.75 |
| handler_duration_seconds (per second) | OpenWorker.DistrictServer | 3869.3 |
| handler_duration_seconds (per second) | OpenWorker.MazeServer | 264.193 |
