# AGENTS.md

Guide for coding agents working on OpenWorker, a SoulWorker server emulator in C# / .NET 10.
The game client (version 1.4.2.3) is the reference for every wire format. The emulator answers what that
client reads.

## Read first

1. **`AGENTS.local.md`** sits next to this file. It holds everything specific to this machine: paths to
   the game client, the data the test stand mounts, local tools, and the limits on the reference materials.
   It is not in git. If it is missing, create it by following [`INSTALL.md`](INSTALL.md) before doing
   anything that needs those paths.
2. [`ARCHITECTURE.md`](ARCHITECTURE.md) explains processes, the world loop, the database and settings, plus
   the rules for code. Those rules are binding.
3. [`REFACTORING.md`](REFACTORING.md) is the refactoring plan, in phases.
4. `docs/` is local and git-ignored. It holds working notes, research notes and the test tooling
   (`docs/NOTES.md`, `docs/PLAN.md`, `docs/notes-*.md`, `docs/tools/`). A fresh clone or a git worktree does
   not have it: read it from the main checkout (path in `AGENTS.local.md`).

## Layout

| Project | What it is |
|---|---|
| `OpenWorker.AuthServer`, `GateServer`, `DistrictServer`, `MazeServer`, `RelayServer` | The server processes. Each hop of the client is its own process. |
| `*.Gameplay` | Game logic per server. `OpenWorker.Gameplay` is shared: combat, AI, items, quests, rewards. |
| `OpenWorker.Protocol` | The protocol: messages (`Modules/<Group>/Requests`, `Responses`, `Types`), opcodes, header, encryption, frames. No database, Redis or batch files. |
| `OpenWorker.Runtime` | World loop, simulation clock, sessions and their writers, handler table and executor, error budget, rate limits, metrics. No EF Core, no Redis. |
| `OpenWorker.Channel` | Services shared by district and maze: chat, community, profiles. |
| `OpenWorker.Server.Common` | What district and maze both run: `PersonRegistry` and the handlers on the bases of `Channel`. A server gives its `IPlayerAudience`. |
| `OpenWorker.Persistence` | EF Core (PostgreSQL) model, stores (behind the interfaces of `OpenWorker.Domain.Stores`), migrations. |
| `OpenWorker.Infrastructure.Redis` | Redis: records of the servers and reservations, session leases, maze directory, index bootstrap. |
| `OpenWorker.Res` | Readers for the client tables (`*.res`). `Rows/` holds client tables, generated from `OpenWorker.SourceGenerator.ResourceStructures/Structures.json` (name a column there and run the generator); `ServerRows/` holds server-only tables, written by hand. Look a row up through a `RowIndex`, never a `First` over the table. |
| `OpenWorker.Batch` | Readers for map batch files and action files. |
| `OpenWorker.Lua` | Maze scripts and the creature manager. |
| `OpenWorker.Navigation`, `OpenWorker.Havok` | Navigation meshes: walls, floor, paths. |
| `OpenWorker.Commands` | Chat commands for testing (`/item`, `/title`, ...). |
| `OpenWorker.Domain`, `Extensions`, `DependencyInjection` | Shared types and store interfaces, options, the host of a server. |
| `OpenWorker.Hotspot.Test`, `OpenWorker.AuthServer.Test` | Tests (MSTest on Microsoft.Testing.Platform). |
| `Research.*`, `TestLang`, `OpenWorker.GameServer` | Experiments. They are not part of `OpenWorker.slnx`. Do not fix them unless asked. |

## Build and test

```bash
dotnet build OpenWorker.slnx
dotnet test --solution OpenWorker.slnx
```

- **Optional test data.** Tests that read client or server data skip themselves unless their variable
  is set:

  | Variable | What it points to |
  |---|---|
  | `OPENWORKER_TEST_RES` | Client tables |
  | `OPENWORKER_TEST_AI` | Monster scripts |
  | `OPENWORKER_TEST_ACTION` | Action files |
  | `OPENWORKER_TEST_NAVMESH` | Navigation meshes |
  | `OPENWORKER_TEST_SERVER_RES` | Server-only tables |
  | `OPENWORKER_TEST_REDIS` | A Redis URL |
  | `OPENWORKER_TEST_POSTGRES` | A Postgres connection string without a database (the test makes and drops its own) |

  `AGENTS.local.md` has a ready `export` block. Run with all of them set before you say the tests pass.
- **CI checks** (`.github/workflows/ci.yml`). Run the ones your change touches:
  - `bash eng/check-forbidden.sh`;
  - `python3 eng/check-duplicates.py` (no file repeated across projects);
  - a Release build;
  - `dotnet tool restore && dotnet ef migrations has-pending-model-changes --project OpenWorker.Persistence`;
  - the tests.
- **Warnings are errors** in every project of `OpenWorker.slnx` except the test projects `OpenWorker.Hotspot.Test`
  and `OpenWorker.AuthServer.Test`; a new project sets `TreatWarningsAsErrors` too. Do not suppress an analyzer without
  a reason in `.editorconfig`.
- **Layers.** A project references its own layer and the ones below it (`ARCHITECTURE.md`, "Projects and their layers");
  `ProjectLayersTest` fails otherwise. A new project gets its layer in that test. The code that came out of
  `OpenWorker.Hotspot` kept its namespaces (`OpenWorker.Hotspot.*`).

### Database migrations

After changing the EF model, add a migration before starting any server: the relay migrates on start (the other
servers start after it and refuse an older schema, or migrate themselves when started with `Persistence__Migrate=true`), and EF Core refuses to
migrate while the model has unmigrated changes.

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project OpenWorker.Persistence --startup-project OpenWorker.Persistence
```

Review the generated file: it must contain only your change. When several agents change the model in
parallel, only one of them (or the coordinator) generates migrations, after the branches are merged.

## Running the servers

The local stand is a set of Docker containers built from the Debug output. The scripts are in
`docs/tools/`:
- `stand_prepare.py` extracts the client data;
- `stand_up.sh` starts the stand;
- `e2e_*.py` are protocol-level end-to-end checks with a scripted client.

Paths and the exact commands are in `AGENTS.local.md`.

- Accounts are `tester`, then `tester2` through `tester12`. The password is `password`.
- `OW_STAND=<n>` starts an independent stand:
  - containers are named `ow<n>-*`;
  - every port is shifted by `n * 1000`;
  - the e2e scripts read the same variable.

  Use your own stand number. Never stop or restart stands that are not yours.
- After a rebuild, restart the stand: the containers mount `bin/Debug`.
- A session lease holds an account for about a minute after it disconnects. Scripts that log in with the
  same account wait 65 s between runs.

## Reading the client and the original server with IDA Pro

The client decides every wire format, and IDA Pro is available on the development machine. Use it whenever a
layout, a field or a rule is unclear, instead of guessing:

- **Headless.** `idat` runs a script over a database and exits. The scripts are in `docs/tools/`:
  - `client_eval.py` decompiles the functions you name;
  - `client_export.py` exports the senders and receivers of every message;
  - `client_dispatch.py` walks the receive switch.
- **MCP.** When the session has the IDA MCP server (tools named `ida`), open a database through it and
  decompile, list cross-references, strings and types directly.
- **The original server.** A build of the original game server is on the development machine as reference
  material, with IDA databases of its processes. Open them to compare a handler of ours with the original:
  - the checks and their order;
  - the error codes;
  - the messages sent and their order;
  - the tables and columns read.

  Compare whenever you implement or review game logic, and record every difference in `docs/`: fixed, or kept and
  why. The client still decides every wire format: where the two disagree on a layout, client 1.4.2.3 wins.
- **Paths and limits.** `AGENTS.local.md` names `idat`, the databases of the client and of the original server, and
  what must not be opened. Work on a fresh copy of a database for every headless run, and never open one database
  from two processes.
- **What you learn is research data.** It goes to the git-ignored `docs/` only (see "Research data stays out of
  git").

## Code rules

The full list is in `ARCHITECTURE.md`. The ones that are broken most often:

- **World loop.**
  - Every server runs its game logic on one thread.
  - Never use `ConfigureAwait(false)`, `.ContinueWith(`, `.Result`, `.Wait()` or `Task.Factory.StartNew`.
  - After every `await`, check that the entity is alive and read its components again.
- **ECS (Arch).** Change a component with `world.Set`. Never keep a reference to a component across an
  `await`.
- **Sending.** `SessionWriter.Send` never throws and never waits.
- **Client data is hostile.** Check lengths, indices, enum values and NaN before using them. A failed parse
  counts against the session error budget.
- **Uniqueness and limits** belong to database constraints. Do not rely on checks in memory.
- **Logging.** Use `[LoggerMessage]` methods only. Never use string interpolation or `Console`.
- **Formatting.** Parse and format with `CultureInfo.InvariantCulture`.
- **Wire format.**
  - The packet header is `u16 version, u16 size, u8 tos`, then `main` and `sub`.
  - An error answer is `sub | 0x80` followed by a `u16` code.
  - A new message gets a test of its exact byte layout.
- **Comments and names** describe what the code does in game terms.

### Research data stays out of git

Never put research data in code, comments, tests, commit messages or committed docs:
- addresses;
- function or symbol names from disassembly;
- names taken from other server builds;
- anything said about debug symbols.

Research data goes only to the git-ignored `docs/`. Before committing, grep your diff for addresses
(`0x` followed by 5 or more hex digits) and for those names.

## Git

- **`dev` is the main branch.** Commit there, or on a branch that is merged into `dev`. Push only when the
  user asks.
- **Commit finished parts without asking.** Split commits by meaning. Every commit builds and passes the
  tests: verify before committing, not after.
- **Message style** is gitmoji plus a conventional scope, written in plain English, one sentence:
  - `✨ feat(Scope): ...`
  - `🐛 fix(Scope): ...` or `fix(Scope): ...`
  - `♻️ refactor(Scope): ...`
  - `🚨 fix(Scope): ...` for analyzer warnings
  - `📝 docs(Scope): ...`
  - `🔀 merge(Scope): ...`

  End the message with a `Co-Authored-By:` trailer naming the model that wrote the change.
- **Never commit** `docs/`, `Reverse/`, `AGENTS.local.md`, `.claude/` or `.env`.
- **Never rewrite published history.** Keep `docs/backup-before-history-rewrite.bundle` local. Never send it
  anywhere.

## Documentation

Write documentation in English, both the committed files and the notes in `docs/`. Replies to the user follow
the user's language. Older notes in `docs/` are partly in Russian: translate a section when you rewrite it.

## Working as one of several agents

A coordinator session hands out parts of the work. Each agent:

- works in its own git worktree on a branch from the current `dev`;
- uses its own stand number;
- rebases on `dev` before reporting, resolves the conflicts itself, then builds and tests again;
- edits files of another agent's area only minimally: one call site or one DI registration. It names
  those edits in its report. The areas are listed in `docs/PLAN.md`;
- writes notes and scratch scripts to a **persistent** folder: the agent folder named in `AGENTS.local.md`.
  `/tmp` is a RAM disk on this machine, so a reboot wipes it, and notes kept there have been lost before;
- reports:
  - its commits;
  - what was verified and how, with numbers;
  - what was accepted without verification;
  - likely conflicts;
  - calls it needs from other areas;
  - where its notes are.

The coordinator:
- merges the branches into `dev`;
- generates the migrations;
- runs the full stand check;
- keeps `docs/PLAN.md` and `docs/NOTES.md` up to date.
