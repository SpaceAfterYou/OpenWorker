# Phase 1, workstream A: metrics and the dashboard

Scope: step 1 of phase 1 of `REFACTORING.md` (the metrics and their export), the criterion "a dashboard with these metrics on the
local compose", and the switch for the GC and JIT settings of step 7. How the code works and how to add an instrument is in the
"Metrics" section of `ARCHITECTURE.md`; this file records what was done and what is left.

## Done

- One `Meter` named `OpenWorker` (`OpenWorkerMeter.Name`), created through the `IMeterFactory` of the container, with a small class per
  area in `OpenWorker.Hotspot/Telemetry`: `WorldLoopMetrics`, `HandlerMetrics`, `SimulationMetrics`, `SessionMetrics`, `RedisMetrics`,
  `PostgresMetrics`. `AddOpenWorkerMetrics` registers them as singletons.
- Every instrument of the plan, recorded where the thing happens (table below). Nothing allocates per packet, and nothing reads the clock
  while no one listens (`Instrument.Enabled` is checked first).
- Why a connection closed: `SessionCloseReason` (`client`, `server`, `backpressure`, `timeout`, `malformed`, and `shutdown` since the
  merge with workstream B), set once per connection and counted by `SessionWriter` when its write loop ends, so each connection is
  counted exactly once under its first reason.
- Redis calls timed on the profiler of the one shared `IConnectionMultiplexer`; Postgres commands timed by an EF Core interceptor.
- Export over OTLP with the OpenTelemetry SDK (`AddTelemetry`, called by `AddHotspot`, and by the relay), configured by the `Telemetry`
  section, off when the endpoint is empty. The `System.Runtime` meter of .NET gives the GC and thread pool metrics.
- Prometheus 3.15.1 and Grafana 12.4.14 in `compose.yaml`, configured under `eng/observability`, with the data source and the dashboard
  `OpenWorker` provisioned.
- The GC and JIT settings of the servers written out in `Directory.Build.props` (the values chosen in step 7, below).
- Tests with `MetricCollector<T>` for every instrument, and for the wiring of the container.
- Step 7, the choice of the collector, on 2026-11-08 after the load bot: the servers run Server GC with DATAS
  (`ServerGarbageCollection=true` in `Directory.Build.props`; background collections, `GarbageCollectionAdaptationMode=1`
  and `TieredPGO` as before). Four runs of 1000 bots through `eng/loadtest.sh`, alternating workstation and Server GC
  (`eng/loadtest/compose.servergc.yaml`, picked with `LOADTEST_COMPOSE_EXTRA`). Rocco Town: `move.echo` p99 2.37 / 2.75 ms
  against 3.77 / 4.35 with workstation, GC pause 958 / 1461 ms in a 92 s run against 1894 / 1948 (1.0 to 1.6% of the
  run against 2.1%), gen-0 collections 713 / 785 against 484 but 1.3 to 1.6 ms each against 3.8, working set at most
  450 / 493 MB against 394 / 396, CPU unchanged. The maze: pause 34 / 40 ms against 118 / 129, simulation step p99
  2.49 / 3.16 ms against 3.25 / 4.62. Every server logs the collector it runs with when it starts
  (`RuntimeSettingsLogService`, registered by `AddTelemetry`), which is how each run was checked. Table, conditions and
  what the runs cannot tell: `docs/phase-1/baseline/README.md`, "GC modes at 1000 bots". TieredPGO was on in every run
  and was not compared.

Checked by hand against the real thing: the auth server ran against Postgres and Redis and pushed to a real Prometheus, a load of
connections, logins and broken packets was sent to it, and every query of the dashboard returned data in Prometheus (the simulation
panels from a small host that runs `SimulationClock`, since a maze server needs the client tables). Grafana loaded the provisioned
data source and dashboard.

## Not done, left open

- ~~`shutdown` as a close reason~~: added when this work was merged into `dev` after workstream B. The read loop gives `Shutdown` when the
  drain (or the stop of the host) cancels the reading, and names it with `ServerSessionComponent.SetCloseReason` before the close handlers
  queue the kick-out, so a drained session is counted once as `shutdown` even when its client does not take the last frames and is cut
  off (`TestGracefulShutdown`, `SessionMetricsTest`, `ServerServiceMetricsTest`).
- The `timeout` reason exists in the district only. The keep-alive service of the gate is not a keep-alive of sessions (it updates the
  record of the gate in Redis), and the maze has no keep-alive at all (`ARCHITECTURE`/`REFACTORING` open question of phase 0).
- `postgres.call.duration` is tagged by the kind of statement, not by table or by feature. A table tag would be one more tag with about
  forty values; add it when a query is found slow and the kind does not tell which.
- `handler.duration` has one series per opcode that has a handler (about 200 per process, times 15 buckets); the 68 opcodes nobody
  handles share the tag `unhandled`. If Prometheus gets heavy, drop the buckets for the opcodes below a rate.
- The maze server was not run: it needs the tables, batch files and scripts of the client. Its instruments are the ones tested elsewhere.
- No alerts, no trace export (the plan asks for metrics only).
- Pre-existing, not caused by this work: `OpenWorker.AuthServer.Test` crashes the test process with an `AccessViolationException` inside
  Arch (`Chunk.GetArray`) when `OPENWORKER_TEST_REDIS` is set and `TestLoginService` runs after the other tests of the assembly. It runs
  alone, and it crashes the same way on the commit this work started from. See "Test results".

## Design decisions

- **`IMeterFactory`, not a static meter.** A test builds its own container and listens only to its own meters, so tests do not see
  each other even when they run in parallel. The price is that every measuring class is handed its metrics class.
- **The metrics class is the last, optional constructor parameter** (`WorldLoop`, `SimulationClock`, `HandlerExecutor`,
  `ServerSessionComponent`, `SessionWriter`). Existing tests, and the tests the other workstreams are writing, keep compiling; `null` is a
  part nobody measures. The risk is a container that forgets to pass it, which `TestMetricsWiring` covers.
- **Sessions are counted in `SessionWriter`.** Its write loop ends exactly once per connection and every way to end a connection goes
  through it, so "exactly once" holds by construction. The reason is a set-once field (`Interlocked.CompareExchange`) on the writer; the
  `ServerSessionComponent` is a struct whose copies share the writer, so a copy can name the reason. `Disconnect(reason = Server)` keeps
  every existing caller as it is.
  A write that times out counts as `backpressure` (the client does not read), an I/O error on a write as `client`.
- **Up-down counters for `sessions.active` and `writer.queue.length`.** A counter that is read by a gauge would need an atomic operation
  on every frame even when nobody listens; `UpDownCounter.Add` costs a check. `worldloop.queue.length` and `simulation.count` are gauges
  because they are read from a value that exists anyway.
- **Redis on the profiler of the connection.** There are about thirty `IRedisCollection<T>` consumers (Redis.OM) and four classes on
  `IConnectionMultiplexer`. A decorator would cover one of the two worlds; the profiler covers both with no change at a call site. A
  wrapper around the task would also have resumed the caller twice on the world loop (or needed `ConfigureAwait(false)`, which the
  architecture forbids there) and added the loop queue to the measured time. The cost: the cache and the community now share one
  connection (`AddRedisConnection`), and commands are turned into measurements once a second by `RedisMetricsService`. While nobody
  listens the profiler returns no session and collects nothing.
- **Postgres on an EF Core `DbCommandInterceptor`.** EF Core already measures each command, so the interceptor only passes `Duration`
  on. The built-in meter of Npgsql has no operation tag. The tag is the first word of the statement (`SELECT`, `INSERT`, `UPDATE`,
  `DELETE`, `OTHER`; `WITH` counts as `SELECT`). The duration is the time until the database starts to answer, as EF Core logs it.
- **Bucket edges in seconds** (`OpenWorkerMeter.CreateDuration`, `InstrumentAdvice`): the default edges of an exporter are made for
  milliseconds, and every duration here would fall into the first bucket. The edges include 50 ms (the step of the clock) and 100 ms.
- **The exporter is built by hand** (`PeriodicExportingMetricReader` over `OtlpMetricExporter`) in the deferred configuration of the
  meter provider, because `AddOtlpExporter` registers services and the container is already built when the options are read; this way
  the endpoint comes from the validated options and an empty one adds no reader, which leaves the instruments switched off
  (tested through `Instrument.Enabled`).
- **`service.name` is the name of the executable** (`OpenWorker.DistrictServer`, ...), not `AddHotspot`'s `EntityComponentService`,
  because the maze server registers itself as `District`. `service.instance.id` is `Instance:Identifier` (or a new Guid for the
  servers with none), and the location of a district and the gate are resource attributes promoted to labels by Prometheus.
- **The GC knobs apply to the projects named `*Server`** through a condition in `Directory.Build.props`, which cannot see `OutputType`
  yet; the test projects are executables too and are left alone.

## Metrics

| Name | Kind | Unit | Tags |
|---|---|---|---|
| `worldloop.queue.length` | observable gauge | `{callback}` | |
| `worldloop.callback.duration` | histogram | `s` | |
| `handler.duration` | histogram | `s` | `opcode` (`Group:Command` by name, `unhandled`) |
| `handler.errors` | counter | `{error}` | `reason`: `malformed`, `handler_fault` |
| `simulation.step.duration` | histogram | `s` | |
| `simulation.count` | observable gauge | `{simulation}` | |
| `simulation.overbudget` | counter | `{step}` | |
| `sessions.active` | up-down counter | `{session}` | |
| `sessions.closed` | counter | `{session}` | `reason`: `client`, `server`, `backpressure`, `timeout`, `malformed`, `shutdown` |
| `writer.queue.length` | up-down counter | `{frame}` | |
| `frames.sent` | counter | `{frame}` | |
| `bytes.sent`, `bytes.received` | counters | `By` | |
| `redis.call.duration` | histogram | `s` | `operation`: the Redis command (`GET`, `JSON.SET`, `FT.SEARCH`, ...) |
| `postgres.call.duration` | histogram | `s` | `operation`: `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `OTHER` |
| `dotnet.gc.*`, `dotnet.thread_pool.*`, `dotnet.process.*`, `dotnet.jit.*` | the `System.Runtime` meter | | |

In Prometheus the names change: `.` becomes `_`, a unit becomes a suffix, a counter ends in `_total`
(`handler_duration_seconds_bucket`, `sessions_closed_total`, `bytes_sent_total`, `dotnet_gc_pause_time_seconds_total`).
Resource labels: `job` (the executable), `instance` (the identifier), `openworker_location`, `openworker_gate`.

## Settings

| Key | Meaning |
|---|---|
| `Telemetry:Endpoint` | OTLP endpoint of the collector; empty turns the export off. Default `http://prometheus:9090/api/v1/otlp/v1/metrics` |
| `Telemetry:Protocol` | `HttpProtobuf` (Prometheus) or `Grpc` |
| `Telemetry:ExportInterval` | time span, at least one second; default `00:00:10` |

They are in the `appsettings.json` of the auth, gate, district, maze and relay servers, validated at start (`TelemetryOptions`), and an
environment variable named like the key (`Telemetry__Endpoint=`) replaces one. To run a server outside compose against the compose
Prometheus: `Telemetry__Endpoint=http://localhost:9090/api/v1/otlp/v1/metrics`.

GC and JIT, in `Directory.Build.props` (all at today's values): `ServerGarbageCollection=false`, `ConcurrentGarbageCollection=true`,
`GarbageCollectionAdaptationMode=1` (DATAS, read only by Server GC), `TieredPGO=true`. To try Server GC with DATAS:
`dotnet publish -p:ServerGarbageCollection=true`, or change that one line.

## The dashboard

`docker compose up prometheus grafana` (the servers find `prometheus:9090` by themselves), then open <http://localhost:3000>: no login
in this setup, the home page is the dashboard `OpenWorker` (folder `OpenWorker`). Prometheus itself is <http://localhost:9090>.
Files: `eng/observability/prometheus/prometheus.yml`, `eng/observability/grafana/provisioning/{datasources,dashboards}`,
`eng/observability/grafana/dashboards/openworker.json`. The panels: loop queue and callback p50/p99; handler p99 and rate by opcode
(top 10); errors by reason; simulation step p50/p99 against the 50 ms line, simulations, steps over budget; sessions, closes by reason,
writer queue, frames and bytes per second; Redis and Postgres p99 and rate by operation; GC pause, collections, heap, allocation rate,
working set, CPU, thread pool. The variables `Server` (job) and `Instance` filter all of them. The dashboard file is provisioned
read-only: change it in the file.

## Test results

67 tests were added (the solution had 580).

- `OPENWORKER_TEST_REDIS=redis://localhost:6380 dotnet test`: `OpenWorker.Hotspot.Test` 542 tests, 529 passed, 13 skipped (tables of the
  client), 0 failed. `OpenWorker.AuthServer.Test` crashes the process in `TestLoginService` (pre-existing, see above, reproduced on
  `cd7f8bb`); without that one test it is 132 tests, 123 passed, 9 skipped, 0 failed, and the Redis test of this work
  (`TestRedisMetrics`) passes.
- Without Redis, whole solution (`dotnet test --solution OpenWorker.slnx -c Release`): 647 tests, 604 passed, 43 skipped (42 as before, plus
  `TestRedisMetrics`, which needs Redis), 0 failed.
- Build: `dotnet build OpenWorker.slnx -c Release --no-incremental`: 0 errors, 198 warnings, the same as before this work.
- `bash eng/check-forbidden.sh`: no forbidden patterns. `docker compose config -q`: valid.
