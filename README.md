# OpenWorker

![Lines of code](https://img.shields.io/endpoint?url=https://ghloc.vercel.app/api/SpaceAfterYou/OpenWorker/badge?style=for-the-badge) ![MatWorker](https://img.shields.io/badge/Mat-Worker-lightgrey?style=for-the-badge&logo=data:image/webp;base64,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) ![Discord](https://img.shields.io/discord/606442027873206292?style=for-the-badge) ![Twitch Status](https://img.shields.io/twitch/status/sawich?style=for-the-badge)

A server emulator for **SoulWorker**, written in C# on .NET 10. It answers what game client **1.4.2.3** sends: the
client decides every wire format, and the server is built to match it.

> **Status: in development. Again.** The servers run, a character can be made and played through districts and mazes,
> but the project has not reached alpha: features are missing, data is not kept between releases, and much of what is
> listed below has been checked with scripted clients only, not yet with the real one.

## What works

| Area | What is there |
|---|---|
| Login and characters | Accounts, the gate list, character creation, selection and deletion, the second password |
| Districts | Channels, movement with a pace check, chat, NPC shops, banks, transports, the prologue |
| Mazes | Maze scripts, monster AI (hate, escape, reactions, grabs, carries), skills, combos, projectiles and traps, buffs and stats, results and ranks, survival, defence and operation modes, cubes, boosters |
| Party and community | Parties and party mazes, recruitment and matching, friends, leagues, whispers, looking at other players |
| Items | Bags and equipment, binding rules, item use, upgrade, sockets, broaches, evolution, disassembly, production, akashic cards (the book and calling a card into a fight) |
| Trade | Trades between players, private shops, the exchange, letters with items and gold |
| Progress | Quests, daily missions, attendance, achievements and titles, soul metries, helpers, the infinite tower and its ranking, the room of a player |
| Operation | Health and readiness probes, metrics for Prometheus with Grafana dashboards, graceful shutdown, a load test with scripted bots |

Not there yet: social items, the other kinds of maze (raids, District 6), hidden events, defensive weapons, the arena.

## How it is built

Each hop of the client is its own process:

| Server | What it does | Port in `compose.yaml` |
|---|---|---|
| `OpenWorker.AuthServer` | Login and the list of gates | 10000 |
| `OpenWorker.GateServer` | Characters of an account | 10010 |
| `OpenWorker.DistrictServer` | A town and its channels, one process per district | 10011 to 10014 |
| `OpenWorker.MazeServer` | Maze instances | 10101 |
| `OpenWorker.RelayServer` | What the servers pass to each other; migrates the database | 10003 |

Every server runs its game logic on one thread (a world loop over an entity component system), keeps players in
PostgreSQL through EF Core and shares sessions, reservations and community state through Redis.
[`ARCHITECTURE.md`](ARCHITECTURE.md) explains the processes, the layers of the projects and the rules the code follows.

## Running it

You need Docker or Podman with compose, and the files of game client 1.4.2.3 **unpacked** (the folders `bin/Table`,
`World/Table`, `World/Navmesh`, `Character/ActionData` and `Scripts/Server` of its `datas`). The client is not part of
this repository.

```bash
# .env next to compose.yaml
SOULWORKER_DATAS=/path/to/unpacked/datas

docker compose up -d --build
docker compose ps        # every OpenWorker-* service turns healthy; the relay first, it migrates the database
```

The stack also starts Redis (with Redis Insight on 5540), PostgreSQL (pgAdmin on 5050), RabbitMQ, Prometheus (9090)
and Grafana (3000). Point the client at `127.0.0.1:10000`.

- There is no registration yet: an account is a row of the `Accounts` table.
- A few tables exist only on the original server (the size of the soul gauge, the rewards of a maze). Without them the
  server still runs, with those features off; `compose.yaml` says how to mount them (`SOULWORKER_SERVER_RES`).
- To run one server outside compose, start the relay first, or give that server `Persistence__Migrate=true`.

## Building and testing

```bash
dotnet build OpenWorker.slnx
dotnet test --solution OpenWorker.slnx
```

The SDK is .NET 10 (see `global.json`) with the ASP.NET Core runtime. Tests that need client data, Redis or PostgreSQL
skip themselves unless their environment variable is set; [`AGENTS.md`](AGENTS.md) lists the variables, the checks CI
runs and how to add a database migration.

## Documentation

| File | What it holds |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Processes, the world loop, the database, settings, metrics, and the rules for code |
| [`AGENTS.md`](AGENTS.md) | The guide for contributors and coding agents: layout, build, tests, code rules, git |
| [`INSTALL.md`](INSTALL.md) | Setting up a development machine |
| [`REFACTORING.md`](REFACTORING.md) | The refactoring plan and what is left of it |

`OpenWorker.Content.Tool` is a desktop viewer for the client's data: items, recipes, maps and navigation meshes.

## Known limits

- Maze scripts run on LuaCSharp, which has no method overloading, no enums and no unsigned numbers except `byte`.

## Chat 🙋

Want to talk about changes, or just hang out? There is a Discord server for that.

[![!](https://invidget.switchblade.xyz/SequFJP)](http://discord.gg/SequFJP)

## ❤️ Contributors

<table>
  <tr>
    <td align="center">
        <a href="https://github.com/fearek">
            <img src="https://avatars.githubusercontent.com/u/46384967?v=4?s=100" width="100px;" alt=""/>
            <br>
            <sub><b>fearek</b></sub>
        </a>
    </td>    
    <td align="center">
        <sub><b>Xillith Arclight</b></sub>
    </td>
    <td align="center">
        <a href="https://github.com/Kimjunseo319">
            <img src="https://avatars.githubusercontent.com/u/59918240?v=4?s=100" width="100px;" alt=""/>
            <br>
            <sub>Kimjunseo319</sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/RutsuKun">
            <img src="https://avatars.githubusercontent.com/u/14915249?v=4?s=100" width="100px;" alt=""/>
            <br>
            <sub>RutsuKun</sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/Zevister">
            <img src="https://avatars.githubusercontent.com/u/44536442?v=4?s=100" width="100px;" alt=""/>
            <br>
            <sub>Sang</sub>
        </a>
    </td>
  </tr>
</table>

## License

[WTFPL](LICENSE).
