# Phase 1, workstream B: health checks, graceful shutdown, structured logs

Scope: steps 2, 3 and 4 of phase 1 in `REFACTORING.md`. Steps 2 and 3 were done in this workstream; step 4 (structured
logs) was stopped before it started and was built afterwards, on the design written here (branch
`refactor/phase-1-logs`, see "Step 4" below).

## Done

### Step 2. Health checks

- `/healthz` (liveness) and `/readyz` (readiness) on `Health:Port` (default 8080 inside the container) in the auth, gate,
  district and maze servers and in the relay. Kestrel through `FrameworkReference Microsoft.AspNetCore.App` in
  `OpenWorker.DependencyInjection`, built on `Microsoft.Extensions.Diagnostics.HealthChecks` (part of the shared
  framework, no new package). One call in each `Program.cs`: `.UseHealthEndpoint()` right after `CreateDefaultBuilder`.
- Liveness: the world loop runs an empty callback within `Health:LoopTimeout` (5 s). Readiness: liveness, Redis
  `PING`, database `CanConnect` (each within `Health:CheckTimeout`, 3 s), the game listener is up, the server is not
  draining. The module that owns the dependency registers the check: `AddCache`, `AddPersistence`, `AddHotspot`.
- The checks run on the thread pool. Only the empty callback runs on the loop, and a stuck loop keeps one pending
  callback instead of collecting one per probe (`WorldLoopHealthCheck`).
- Settings follow the rules of `ARCHITECTURE.md`: `HealthOptions` with attributes and a partial `[OptionsValidator]`,
  registered in `OptionsServiceCollectionExtensions.AddHealthOptions`, defaults in the five `appsettings.json`, covered
  by `TestServerOptions`. `Health:Port` must differ from `Server:Port` (checked at start, the message names the keys).
- Docker: the five images moved from `dotnet/runtime` to `dotnet/aspnet`, with `curl` installed as root and
  `USER $APP_UID` restored. `compose.yaml` has a shared `x-openworker-server` block: `healthcheck` of `/readyz` with
  `curl` (`CMD-SHELL`, so `Health__Port` set in the environment of the container is honoured), `start_period: 60s`, and
  `stop_grace_period: 40s`. The health port is not published; `docker compose exec <service> curl -s
  http://127.0.0.1:8080/readyz` shows the body, which names the failing check.
- Why `curl` and not something else: the images have no HTTP client. A probe that starts the application a second time
  every few seconds costs far more than a few megabytes of `curl`; the `-alpine` images would change libc and
  globalization for the whole server. `docker build --check` could not reach `mcr.microsoft.com` from here (connection
  reset), so the Dockerfiles were checked by reading, and `docker compose config -q` passes.

### Step 3. Graceful shutdown

`ServerShutdownService` is an `IHostedLifecycleService`: the host calls its `StoppingAsync` before it stops any
service, so the order does not rest on the order of registration.

1. The server is draining (`ServerState`, set at `ApplicationStopping`, so SIGTERM turns readiness off at once) and
   `ServerService.StopAcceptingAsync` closes the listener.
2. Every `IServerRegistration` withdraws, next to step 3 (awaited after it). The registration services derive from the new
   `RegistrationService`: the loop ends first, then the record is removed, once, whether the drain or the host's stop asks
   first. Done for the maze (`MazeRegistrationService`, it already removed its record at stop), the district
   (`ServerBootstrapService`, `DistrictCache`), the channels (`ChannelLifecycleService`, every `ChannelCache` of the
   instance), the gate (`KeepAliveService`, `GateCache`) and the lease renewal (`SessionLeaseRenewalService`, which only
   stops, so a late renewal cannot write a lease as connected after its session's close wrote it as disconnected).
3. `ServerService.CloseSessionsAsync` cancels the read loops (the packet being handled is finished) and each session
   closes through the normal path: kick-out message, close handlers on the loop, `Disconnect`, then the session waits for
   the writer to send what is queued (`ServerSessionComponent.Closed`, bounded by the drain, `Abort()` after that).
4. The host stops the other services (the health endpoint among them) and the container disposes the world loop last.

Timeouts: `Shutdown:DrainTimeout` (15 s) bounds the drain as a whole and each writer; `Shutdown:Timeout` (30 s) becomes
`HostOptions.ShutdownTimeout` and must be longer; compose waits 40 s.

**Deviation from the brief.** The records in Redis are withdrawn at the start of the drain, not after the sessions are
closed: while the sessions close (seconds under load) other servers would keep sending players to a server that no
longer accepts them. The world loop still stops last.

**The shutdown message.** The protocol has two messages that could carry it: `PersonKickOutResponse`
(`Character/KickOut`) with the reason `GmToolServerShutdown` (0xB) from the client's own list, and `ChatNoticeResponse`
(a banner line, used by the `notice` command). The server sends the kick-out, with the account of the session when it is
logged in. **It has not been tried against a real client**: what the client shows for 0xB on a district or a maze
server is unknown. The place is `ServerService.NotifyShuttingDown`; swapping the message is a one-line change.

The close caused by the shutdown is the `shuttingDown` flag in `ServerService.SessionLoopAsync` / `CloseSessionAsync`;
since the merge of workstream A, `sessions.closed` counts such a session under the reason `shutdown`.

### Step 4. Structured logs

- **The scope.** `SessionLogScope` (`OpenWorker.Hotspot`) is one object per session, made with its
  `ServerSessionComponent` (`LogScope`), and implements `IReadOnlyList<KeyValuePair<string, object?>>` with four fixed
  keys: `Session` (the entity, set by the read loop when it creates it), `Remote`, `Account` and `Person` (null until
  known). The values are boxed once when they are set, and `ToString` (what the simple console prints after `=>`)
  is built once per change, under a version, so a line written off the loop while a value changes cannot keep a stale
  text. A packet builds no state object of its own.
- **Where it begins.** Inside the callback of the world loop: `HandlerExecutor.ExecuteAsync` around the parse and the
  handler (a new overload takes the scope; the read loop passes the one it holds, the old overload looks it up in the
  component) and `ServerService.CloseSessionAsync` around the shutdown notice and the close handlers. The read loop
  also begins it for its own lines (`Connection closed`, a failed loop, a flush that timed out); the queue of
  `WorldLoop` does not carry the execution context of whoever posts, so that one never reaches the loop. Both
  methods are `async`: the builder restores the execution context of the caller when the method returns, so a callback
  that awaits leaves nothing behind for the next callback of the loop, while its own continuations keep the scope.
- **Account and person.** The account is set where the claims are set: `SetLogAccount` in the login and the return of a
  session to the auth server, the entry to the gate, the district and the maze (five places; there is no shared place
  that has the world, `SessionLeaseManager.AttachAsync` has the account but no world). The person is set by
  `SetLogPerson` in the two `PersonRegistry.PullPerson` of the district and the maze, which duplicate each other. The
  gate keeps its persons on entities of their own, so a gate session carries the account only, as an auth session does.
- **Settings.** `Logging:Console:FormatterName: simple` and `Logging:Console:FormatterOptions:IncludeScopes: true` in the
  `appsettings.json` of the auth, gate, district, maze and relay servers; `TestServerOptions.TheConsolePrintsTheScopes`
  reads each file and checks what the formatter gets.
- **`PrintSanity` is gone** (the per-packet Debug dump of `HandlerExecutor`). `DumpPacket` of `ServerService` stays, governed
  by `Hotspot:PacketDumpPath`.
- **Cost per packet, to measure in the phase 2 benchmark.** `BeginScope` allocates on every call; measured with
  `GC.GetAllocatedBytesForCurrentThread` over 100,000 calls of `BeginScope` and `Dispose` (also inside an `async
  ValueTask` that completes at once, as `ExecuteAsync` does), Microsoft.Extensions.Logging 10.0.14: **200 B** with the
  providers of `Host.CreateDefaultBuilder` on Linux (Console, Debug, EventSource: the logger wraps the scopes of the
  providers without an external scope provider in a `Scope` with an array) and **120 B** with the console alone (the
  `Scope` node of `LoggerExternalScopeProvider` and two changes of its `AsyncLocal`, each a new execution context). The
  same method without the scope allocates nothing. A formatter that enumerates the scope (the JSON one) also allocates
  its enumerator per line; the simple formatter calls `ToString`, which is cached.
- **Smoke test.** The auth server built in Release, on the host, with Debug logs, a TCP client that sent three frames of
  garbage after a valid header:

  ```
  warn: OpenWorker.Hotspot.Handler.HandlerExecutor[1]
        => Session=0 Remote=127.0.0.1:46922
        Session 0 (actor 0, 127.0.0.1:46922) sent a packet 73:0xFA that failed, reason malformed: error 1 of 10 within 00:01:00.
  dbug: OpenWorker.DependencyInjection.ServerService[1672666189]
        => Session=0 Remote=127.0.0.1:46922
        Connection closed: Unable to read beyond the end of the stream.
  ```

  The lines of the background services in the same run (the gate list query of EF Core) carry no scope.
- **Tests.** `SessionLogScopeTest` (Hotspot.Test), on `FakeLoggerProvider` behind one `LoggerFactory`, so the scope
  begun by the executor reaches the logger of the handler: the lines of a handler before and after an await carry the
  one scope object of the session; a callback that runs while the handler waits and one that runs after it carry none;
  the account and the person appear once set (and not before: the text of the scope is taken when the line is
  written); a warning of the error budget carries it; the close handler and the read loop of a real `ServerService`
  carry the same object as the handler of that session. `ProbeHandler` got a `Logs` mode for them. Full run with
  Redis: 1012 tests, 935 passed, 77 skipped (the tables of the client), 0 failed. Warnings of a clean Release build:
  196 before and after.

## Not done / open

- A close with unread data from the client can end in a reset instead of a FIN, which may throw away the last frames
  the client has not read yet. The notice goes first in the close, so the client has normally read it. A half-close
  (`Shutdown(Send)`, then read until the client hangs up, with a short limit) would remove the doubt.
- The district and channel records have no lifetime in Redis and are not renewed: after a crash (`kill -9`) they stay
  until the relay clears Redis. Withdrawal fixes the orderly stop only. A lifetime and a renewal are phase 3/4 work.
- The ASP.NET Core runtime and targeting pack are now a prerequisite of the build and the tests (see below).
- Not validated: a build of the images and a run in compose (`mcr.microsoft.com` unreachable from here), the
  `Overriding address(es)` warning of Kestrel when the base image's `ASPNETCORE_HTTP_PORTS` is set (not expected, since
  the port is set in code, but unchecked).

## Environment finding (important for the merge)

The .NET install on this machine (distribution packages) has no ASP.NET Core runtime or targeting pack. With the
framework reference:

- `dotnet build` fails with `NETSDK1226: Prune Package data not found ... Microsoft.AspNetCore.App`. Install
  `aspnet-targeting-pack` (and `aspnet-runtime` to run), or pass `-p:AllowMissingPrunePackageData=true`; the pack itself
  comes from NuGet at restore.
- The test executables fail with `You must install or update .NET ... Microsoft.AspNetCore.App`.

This work was built and tested with `-p:AllowMissingPrunePackageData=true` and with a user-local copy of the runtime
(`DOTNET_ROOT_X64`). CI is not affected: the SDK that `actions/setup-dotnet` installs carries the ASP.NET Core
runtime and pack. The alternative that avoids all of this is `System.Net.HttpListener` over the same checks (no framework
reference, images stay on `dotnet/runtime`); it was not chosen because Kestrel is what the brief recommended and what a
later metrics endpoint is likely to need, and the checks do not depend on the transport.

## Tests

- New: `TestHealthChecks` (each check, timeouts, registration once, tags), `TestHealthEndpoint` (HTTP of a real host,
  stuck loop gives 503 and recovers), `TestGracefulShutdown` (real TCP clients: listener refuses, readiness 503 while
  liveness 200, the loop answers during the close handlers, close handlers run on the loop for every session, 3000 queued
  frames per client arrive before the connection closes followed by the kick-out, withdrawal before the first close and a
  failing withdrawal does not stop the drain, a client that does not read is cut off at the drain time, host timeout from
  the settings), `TestSessionTracker`, `TestRegistrationRecords` (real Redis), `RegistrationServiceTest`, two tests in
  `SessionSendTest`, and the settings tests in `TestServerOptions`.
- Two mutations were tried and caught: removing the wait for the writer fails the flush test; not stopping the
  accept loop fails the refusal test.
- Also fixed: `TestLoginService` called `entity.Set(world, component)`, which binds to the two-component overload and wrote
  past the chunk; the test process crashed with an access violation in about three runs of four.
- Result of the final run (`dotnet test --solution`, Redis on 6381): 635 tests, 613 passed, 22 skipped (the tables of
  the client), 0 failed. At the start of the work AuthServer.Test crashed in most runs because of the login test above.
- Warnings on a clean Release build: 198 before, 190 after (the log calls of `ServerService`,
  `SessionLeaseRenewalService` and the touched services moved to `[LoggerMessage]`). No new warning in a touched file.

## Files other workstreams are likely to touch

`ServerService.cs` (accept loop, session loop, close), `SessionWriter.cs` (`Completed`, `Abort`),
`ServerSessionComponent.cs` (`Closed`, `Abort`), `HotspotHostingExtensions.cs` (`AddHostedInternalServices`, `AddHotspot`),
`Cache.cs`, `Persistence.cs`, the five `Program.cs`, the five `Dockerfile`s, `compose.yaml` (the `x-openworker-server`
block and `<<: *openworker-server` in eight services), the five `appsettings.json` (`Health`, `Shutdown`),
`OpenWorker.DependencyInjection.csproj` (framework reference, `InternalsVisibleTo`), `OptionsServiceCollectionExtensions.cs`.
Kestrel already listens on `Health:Port`; a metrics endpoint can share it. Step 4 touched `HandlerExecutor.cs`,
`ServerService.cs` (session loop, `CloseSessionAsync`), `ServerSessionComponent.cs`, the five places that set
`ClaimsComponent`, the two `PersonRegistry.PullPerson` and the five `appsettings.json` (`Logging`).
