# Phase 2: the load test after the send and receive branches

One run of `eng/loadtest.sh` at 1000 bots on `dev` at `5f00ee3` (the send-path merge on top of the receive-path
merge), 2026-11-08 21:56 UTC, with the same command and the same stack as the GC runs of phase 1
(`docs/phase-1/baseline/README.md`, section "GC modes at 1000 bots"): Server GC with DATAS, compose, Release,
`--ramp-up 50 --duration 60s --warmup 10s --move-rate 5`, the servers' own channel placement. The report is in
`loadtest-1000-bots/`. One run, so the spread is the one seen between the two Server GC runs of phase 1.

| | phase 1, Server GC, run 1 | phase 1, Server GC, run 2 | after phase 2 A + B |
|---|---:|---:|---:|
| `move.echo` p50 / p99 (ms) | 0.47 / 2.75 | 0.45 / 2.37 | 0.43 / 2.18 |
| `ping.after-move` p99 (ms) | 3.20 | 2.62 | 2.30 |
| `maze.move.echo` p99 (ms) | 2.05 | 1.89 | 1.79 |
| district CPU avg / max (% of a core) | 454 / 489 | 434 / 489 | 407 / 445 |
| district CPU per bot (ms/s) | 4.5 | 4.3 | 4.1 |
| district memory max, docker stats (MB) | 450 | 493 | 301 |
| maze CPU avg (%), memory max (MB) | n/a | n/a | 32.2, 264 |
| world loop callback p99 district / maze (ms) | 0.32 / 2.05 | 0.26 / 2.07 | 0.19 / 2.05 |
| world loop queue max, district | 2 | 5 | 1 |
| simulation step p99, maze (ms) | 2.49 | 3.16 | 2.49 |
| bots that hear a move | about 54 | about 54 | 56.7 |
| errors | 0 | 0 | 0 |

What changed between the two codes: the send path encodes a broadcast once into a pooled frame instead of once
per recipient, the handler and the readers of the executor are reused, the exchange of the bytes is vectorised,
the read loop rents a buffer per packet through a pipe instead of holding 64 KB per connection, and every length
read from the network is checked (`A-send-path.md`, `B-receive-path.md`).

What the run says: the district does the same work with about 7% less CPU and 35 to 40% less memory, its
world-loop callbacks are shorter (p99 0.19 ms against 0.26 and 0.32), and the round trip of a move is a little
faster (p99 2.18 ms against 2.37 and 2.75). The gains are smaller than the benchmarks of `A-send-path.md`
suggest (a broadcast to 50 at half the time and no allocation), because the bot shares the machine and the
loopback, and because a large part of the time per recipient is spent waking each connection's writer, which
phase 2 has not touched. Entry into the world is as slow as in phase 1 (`district.enter` p99 672 ms with 500
characters created during the ramp-up); it is outside the measured minute and outside phase 2.

The remaining criteria of phase 2 (the `ref struct` reader and writer, the generator) are the next branch; the
next run should follow it.
