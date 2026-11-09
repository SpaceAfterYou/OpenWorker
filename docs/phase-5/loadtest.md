# Phases 3, 4 and 5: the load test after their merges

One run of `eng/loadtest.sh` at 1000 bots with a fifth of them in mazes (200 mazes, twice the 100 of the phase 1
baseline) on `dev` at `df13f6d`, the merge of phase 3 on top of phases 4 and 5, 2026-11-09 00:14 UTC. The stack
and the command are those of the earlier runs (`docs/phase-1/baseline/README.md`, `docs/phase-2/loadtest.md`):
Server GC with DATAS, compose, Release, `--ramp-up 50 --duration 60s --warmup 10s --move-rate 5`, plus
`--maze-share 0.2`. The report is in `loadtest-1000-bots-200-mazes/`. One run; the spread between runs of the
same code was 0.4 ms of p99 in phase 1.

| | phase 1, Server GC (2 runs) | after phase 2 | after phases 3, 4, 5 |
|---|---:|---:|---:|
| bots in / mazes | 1000 / 100 | 1000 / 100 | 1000 / 200 |
| `move.echo` p50 / p99 (ms) | 0.46 / 2.37 and 2.75 | 0.43 / 2.18 | 0.38 / 1.79 |
| `ping.after-move` p99 (ms) | 2.62 and 3.20 | 2.30 | 1.70 |
| `maze.move.echo` p99 (ms) | 1.89 and 2.05 | 1.79 | 3.07 (twice the mazes) |
| district CPU avg / max (% of a core) | 434 and 454 / 489 | 407 / 445 | 368 / 411 |
| district CPU per bot (ms/s) | 4.3 and 4.5 | 4.1 | 3.7 |
| district memory max, docker stats (MB) | 450 and 493 | 301 | 306 |
| maze CPU avg (%), memory max (MB) | n/a | 32, 264 | 52, 284 |
| simulation step p99, maze (ms) | 2.49 and 3.16 | 2.49 | 4.98 |
| world loop callback p99 district / maze (ms) | 0.26–0.32 / 2.05–2.07 | 0.19 / 2.05 | 0.22 / 3.09 |
| world loop queue max, district / maze | 2–5 / 0 | 1 / 0 | 0 / 0 |
| GC pause of the district over the run (ms) | 958 and 1461 | n/a | 46 |
| Postgres calls, all servers, over the run | 22 565 (90 s, phase 1 baseline, workstation GC) | n/a | 18 012 (91 s) |
| Postgres calls per bot per minute | 15.0 | n/a | 11.9 |
| Redis calls, all servers, over the run | 33 214 | n/a | 20 701 |
| Redis calls per bot per minute | 22 | n/a | 13.6 (district 10 418 against 18 486) |
| bots that hear a move | about 54 | 56.7 | 49.5 |
| refused packets, speed violations, dropped frames | n/a | n/a | none recorded |
| errors | 0 | 0 | 0 |

What the run says, phase by phase:

- **Phase 5.** 200 mazes with one bot each, every bot using a skill a second, keep the maze server at a simulation
  step p99 of 5.0 ms and a world-loop callback p99 of 3.1 ms, far under the 50 ms budget the criterion names at
  twice the baseline; the maze process costs 52% of a core and 284 MB, about 0.2 MB per maze more than at 100.
- **Phase 4.** The write-behind and the Redis changes cut the database calls by a fifth and the Redis calls by a
  third per bot per minute while the run also created 500 characters and 200 maze entries; the district's Redis
  calls almost halved. No "too many clients" at the end of the run.
- **Phase 3.** The bots of a channel all stand within a few units of the entry point, so they share one cell and a
  move still reaches the whole channel (49.5 listeners); the fixed-density criterion needs a bot option that
  spreads the bots over more than one cell. The speed check and the rate limits refused nothing: the bots walk
  a circle at 5 moves a second within the allowed speed. The move round trip is the fastest so far, 1.79 ms p99.
- **Across the phases.** The GC pause of the district fell from about a second per run to 46 ms, the effect of the
  pooled frames and the allocation-free dispatch of phase 2 under Server GC.

Not shown by this run: visibility at the cell boundaries (needs a real client), the behaviour of the speed check
on real movement (dash, jump, knock-back), a `kill -9` with bots to confirm what the write-behind loses, and
the spread between repeated runs.
