# Phase 6, part A: the layers

Steps 1, 2, 6 and 7 of phase 6 of `REFACTORING.md` ("Структура"): `OpenWorker.Hotspot` split into the protocol, the runtime
and Redis; the store of a person declared in `OpenWorker.Domain` and implemented in `OpenWorker.Persistence`, so that the game
logic no longer references EF Core; a test that holds every project to its layer; the decision about the ECS written down.
Branch `refactor/phase-6-layers`, 2026-11-09. Steps 3 (`OpenWorker.Server.Shared`), 4 (`HotspotMessageAttribute` and
`MessageOpcode`) and 5 (the names of the columns of the tables) are left to the second part.

Nothing changes in behaviour: files moved with `git mv`, namespaces stayed, and the full test suite (970 tests, 89 skipped for
want of client data, the 3 new ones being the dependency test) passes as before.

## The graph before

`OpenWorker.` left out; projects of tests and benchmarks left out.

| Project | References | Packages of note |
|---|---|---|
| Wire | | |
| Domain | Wire | Arch, Microsoft.EntityFrameworkCore.Abstractions, Redis.OM (unused) |
| Extensions | Domain | |
| Res | Extensions | |
| Batch | Domain, Res | |
| Persistence | Domain | EF Core, Npgsql |
| **Hotspot** | Batch, Domain, Extensions, Persistence, Wire, Wire.SourceGenerators | Microsoft.Extensions.Hosting |
| Gameplay | Domain, Hotspot, Persistence, Res | Arch |
| Lua | Batch, Gameplay, Hotspot, Navigation | LuaCSharp |
| AuthServer.Gameplay | Gameplay, Hotspot | |
| DistrictServer.Gameplay | Gameplay, Hotspot | |
| GateServer.Gameplay | DependencyInjection, Gameplay, Hotspot, Persistence | |
| Channel | Domain, Gameplay, Hotspot, Persistence, Res | |
| Commands | Channel, Gameplay, Lua | |
| DependencyInjection | Channel, Domain, Extensions, Hotspot, Persistence | OpenTelemetry |
| AuthServer | AuthServer.Gameplay, DependencyInjection, Hotspot | |
| GateServer | Channel, Gameplay, GateServer.Gameplay, Hotspot | |
| DistrictServer | Batch, Channel, Commands, DependencyInjection, DistrictServer.Gameplay, Extensions, Gameplay, Hotspot | |
| MazeServer | Channel, Commands, DependencyInjection, Extensions, Gameplay, Hotspot, Lua | |
| RelayServer | DependencyInjection, Domain | |
| UpdateContent | Batch, Domain, Hotspot, Persistence, Res | |
| LoadTest | Hotspot, Persistence | |

Every project reached EF Core through `Hotspot` → `Persistence` and Redis through `Domain`; the protocol, the loop and the
Redis records were one assembly.

## The graph after

| Layer | Project | References | Packages of note |
|---|---|---|---|
| Foundation | Wire, Wire.SourceGenerators, Commands.SourceGenerators, Navigation, Havok | | |
| Domain | Domain | Wire | Arch |
| Shared data | Extensions | Domain | |
| | Res | Extensions | |
| | Batch | Domain, Res | |
| | SourceGenerator.ResourceStructures | Extensions | |
| Protocol | **Protocol** | Domain, Extensions, Wire, Wire.SourceGenerators | Microsoft.Extensions.Logging.Abstractions |
| Runtime | **Runtime** | Domain, Extensions, Protocol | Arch, Microsoft.Extensions.Hosting |
| Infrastructure | **Infrastructure.Redis** | Extensions, Runtime | Redis.OM |
| | Persistence | Domain, Runtime | EF Core, Npgsql |
| Game logic | Gameplay | Batch, Domain, Res, Runtime | Arch |
| | Lua | Batch, Gameplay, Navigation, Runtime | LuaCSharp |
| | AuthServer.Gameplay | Gameplay, Infrastructure.Redis, Persistence, Runtime | |
| | DistrictServer.Gameplay | Gameplay, Runtime | |
| Channel | Channel | Domain, Gameplay, Infrastructure.Redis, Persistence, Res, Runtime | |
| | GateServer.Gameplay | Channel, Gameplay, Infrastructure.Redis, Persistence, Runtime | |
| | Commands | Channel, Commands.SourceGenerators, Gameplay, Lua | |
| Hosting | DependencyInjection | Channel, Domain, Extensions, Infrastructure.Redis, Persistence, Runtime | OpenTelemetry |
| Servers | AuthServer | AuthServer.Gameplay, DependencyInjection, Runtime | |
| | GateServer | Channel, DependencyInjection, Gameplay, GateServer.Gameplay, Runtime | |
| | DistrictServer | Batch, Channel, Commands, DependencyInjection, DistrictServer.Gameplay, Extensions, Gameplay, Runtime | |
| | MazeServer | Channel, Commands, DependencyInjection, Extensions, Gameplay, Lua, Runtime | |
| | RelayServer | DependencyInjection, Domain | |
| | UpdateContent | Batch, Domain, Persistence, Protocol, Res | |
| | LoadTest | Persistence, Protocol | |
| Tests | AuthServer.Test, Hotspot.Test, LoadTest.Test, Benchmarks | | |

## What moved where

The namespaces did not change. `OpenWorker.Hotspot.*` and `OpenWorker.GameServer.SoulWorker.*` are now spread over the new
assemblies; renaming them would have touched the 986 files of the protocol and their callers for nothing, and the keys of the Redis records are
named after the full names of their types (`typeof(SessionCache).FullName`), so renaming the cache types would have changed
the keys the servers share. A new project sets `RootNamespace` to `OpenWorker.Hotspot` for the files it gets later.

| From `OpenWorker.Hotspot` | To |
|---|---|
| `Modules/**`, `Messages/**`, `Extensions/**` (binary readers and writers), `Dtos/**`, `Enums/**`, `SoulWorker/**` (opcodes, `MessageOpcode`, `MessageHeader`, `MessageEncryption`, `PacketRedaction`), `Handler/Attributes/**` (`HotspotMessageAttribute`, its direction), `Frame`, `FrameEncoder`, `ServerContent` | `OpenWorker.Protocol` |
| `WorldLoop`, `SessionWriter`, `ServerSessionComponent`, `SessionCloseReason`, `ISessionCloseHandler`, `SessionLogScope`, `SimulationClock`, `ScheduledAction`, `StepList`, `IServerRegistration`, `RegistrationService`, `Handler/**` (table, builder, executor, error budget, rate limiter, `IHotspotHandler`), `Telemetry/` (`OpenWorkerMeter`, the metrics of the loop, handlers, simulation and sessions) | `OpenWorker.Runtime` (the folder, renamed) |
| `Cache/**` (Redis.OM records, `SessionLeaseManager`, `MazeDirectory`, `RedisScript`, `BootstrapService`, `ChannelUtils`, `AddRedisConnection`), `Telemetry/RedisMetrics`, `RedisMetricsService` | `OpenWorker.Infrastructure.Redis` |
| `Telemetry/PostgresMetrics`, `PostgresCommandInterceptor` | `OpenWorker.Persistence/Telemetry` |
| `BatchManager`, `BatchResult`, `BatchType`, `AddBatch` | `OpenWorker.Gameplay/Batch` |

Seams that had to be cut:

- **The encoder and the loop.** `FrameEncoder.Current` asked whether the synchronization context was the `WorldLoop`, and the
  encoder counted dropped frames on `SessionMetrics`. The protocol now has two internal interfaces, `IEncoderThread` (a context
  with one thread and one encoder) and `IFrameDrops` (the counter of `frames.dropped`); `WorldLoop` and `SessionMetrics`
  implement them, and `Protocol` lets `Runtime` see its internals.
- **The metrics.** `AddOpenWorkerMetrics` registers the metrics of the runtime; `AddRedisMetrics` (`Infrastructure.Redis`) and
  `AddPostgresMetrics` (`Persistence`) add theirs on the same meter, and `AddTelemetry` calls all three, so a server registers
  the same classes as before. `OpenWorkerMeter.Create` and `CreateDuration` became public for the two other assemblies.
- **The address of Redis.** `RedisOptions.ToConfigurationOptions` built a StackExchange.Redis configuration inside
  `OpenWorker.Extensions`, which compiled only because `Domain` carried Redis.OM. It is an extension method of
  `Infrastructure.Redis` now (same namespace, same call), and `Domain` no longer references Redis.OM.

## The store of a person (step 2)

`OpenWorker.Domain.Stores.IPersonStore` loads the items and the progress documents of a person, reserves item serials and writes
the `PersonChanges` of a flush; `PersonChanges`, `PersonStateChanges`, `PersonPlace` and `PersonDocument` moved from
`OpenWorker.Persistence` to that namespace. `PersonStore` of `Persistence` implements it with the statements of `ItemStore` and the
query `PersonStateStore` ran itself; `AddPersistence` registers it (`AddPersistenceStores`). `InventoryPersistence`,
`DatabaseItemSerials`, `PersonStateStore` and `PersistenceWriter` take the store instead of `IDbContextFactory<PersistenceContext>`,
and `Gameplay` dropped its reference to `Persistence`.

`Domain` dropped `Microsoft.EntityFrameworkCore.Abstractions`: its only use was `[PrimaryKey]` on `ServerContentPersistent`, which
is `ServerContentConfiguration` in `Persistence` now. `has-pending-model-changes` reports no change.

The `*.Gameplay` projects of the servers were checked as well:

| Project | EF Core in its code | State |
|---|---|---|
| `AuthServer.Gameplay` | `LoginGameplay` (accounts), `GateGameplay` (gates) query the context | references `Persistence` directly now (it had it through `Gameplay`) |
| `GateServer.Gameplay` | `LoginGameplay`, `PersonGameplay` (with Npgsql for the unique violation), `OptionGameplay`, `SecondPasswordGameplay` | references `Persistence` as before |
| `DistrictServer.Gameplay` | none | held to no EF Core by the test |

Moving the queries of the gate and the login behind stores is a change of their code, not a move, and is left for later.

## The dependency test (step 6)

`ProjectLayersTest` in `OpenWorker.Hotspot.Test` reads `OpenWorker.slnx` and the project files with `System.Xml.Linq`, no package:

1. every project of the solution has a layer in the table of the test, and the table names no project that is gone;
2. no `ProjectReference` goes to a higher layer (a reference within a layer is allowed);
3. through every project it references, `Protocol` reaches no package or project named `Microsoft.EntityFrameworkCore*`, `Npgsql*`,
   `OpenWorker.Persistence`, `Redis.OM`, `StackExchange.Redis`, `OpenWorker.Infrastructure.Redis` or `OpenWorker.Batch`; `Runtime`
   none of the EF Core and Redis ones; `Gameplay`, `Lua` and `DistrictServer.Gameplay` none of the EF Core ones.

Each rule was checked to fail: a reference from `Runtime` to `Channel`, Redis.OM back on `Domain`, and `Persistence` back on
`Gameplay` each turn the test red with the path that reaches the forbidden dependency.

The exceptions it allows, and why:

| Exception | Why |
|---|---|
| `Persistence` → `Runtime` | Allowed by the order (Infrastructure is above Runtime), but worth knowing: the only use is `OpenWorkerMeter`, the meter the Postgres metrics are recorded on. |
| `Protocol` → `Extensions` | `Extensions` sits below the protocol: the messages use its binary readers and writers. It carries no Redis package any more. |
| References within a layer (`Res` → `Extensions`, `Batch` → `Res`, `Lua` → `Gameplay`, `Commands` → `Channel`, `GateServer.Gameplay` → `Channel`) | A layer is a set of peers; MSBuild already forbids cycles. |
| `GateServer.Gameplay` in the Channel layer | It sends a chosen character on to the world through `WorldManager` and `LeaveManager` of `Channel`. It referenced `DependencyInjection` for that; it references `Channel` now, and `GateServer` references `DependencyInjection` itself. |
| `AuthServer.Gameplay` and `GateServer.Gameplay` reach EF Core | Their handlers query the database themselves (table above). |
| The test only reads project references and package names | A package that pulls EF Core or Redis in by its own dependencies would not be seen; none of the packages of those projects does today (their `project.assets.json` lists neither EF Core, Npgsql nor Redis). |

## `ServerService`

It stays in `OpenWorker.DependencyInjection`. It accepts the connections and runs their read loops, but it also builds the entity
of a connection through `ArchComponentProvider` (which picks the components of the server type), reads `ClaimsComponent` of
`Gameplay` and sends `PersonKickOutResponse` to every player when the server stops, and it works with `ServerState` and
`SessionTracker`, which the health checks and the shutdown share. In `Runtime` it would need `Gameplay` under it. The runtime
gives it the parts: `WorldLoop`, `ServerSessionComponent`, `SessionWriter`, `HandlerExecutor`, the session metrics.

## The ECS (step 7)

Written into `ARCHITECTURE.md`, "Inside one process", "ECS", as the code does it today: for a session and its person Arch is a
typed set of components per entity, read and changed by the handlers of its connection, with no system over the sessions; what
must visit every player at once is an Arch query over its component (the day change of attendance, boosters and daily missions,
the snapshots of the flush, the player count of a maze server); monsters are entities of one archetype stepped by the simulation
clock through the `StepList` of their maze; visibility is the grid of each `ServiceChannel`, outside the ECS.

## Not done here

- Presence (`RedisPresenceStore`) and pub/sub (`RedisCommunityTransport`) of the community stay in `OpenWorker.Channel`: they
  implement `IPresenceStore` and `ICommunityTransport` of the channel and use `PresenceRecord`; moving them to
  `Infrastructure.Redis` means moving those contracts down first.
- The login and character handlers of the auth and gate servers still use EF Core directly.
- The test project keeps its name, `OpenWorker.Hotspot.Test`: compose mounts its `Datas` folder by that path.
