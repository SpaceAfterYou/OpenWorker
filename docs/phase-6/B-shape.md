# Phase 6, part B: the shape of the code

Steps 3, 4 and 5 of phase 6 of `REFACTORING.md` ("Структура"): what the district and the maze repeated, the attribute of the
messages and the opcode, the names of the columns of the client tables and the searches through them. Also the warnings of
the client enums. Branch `refactor/phase-6-shape`, 2026-11-09, from `dev` at `0a02fc5`, rebased on `164863c`.

| Commit | What |
|---|---|
| `f4913ea` | `OpenWorker.Server.Common`, `IPlayerAudience`, `eng/check-duplicates.py`, the gate's `GateRegistrationService` |
| `7dfcfe3` | `HotspotMessageAttribute` with one constructor, `MessageOpcode` without conversions |
| `dae837d` | a fault found on the way: `LuaCreatureManager.MoveToPoint` threw on the world of id 0 |
| `a6e42ac` | the columns named in `Structures.json`, the generator rewritten, `ResourceRowsTest` |
| `d418a08` | `RowIndex` instead of the searches through the tables |
| `2a6dc8e` | the client enums in `.editorconfig`, four more projects on warnings-as-errors |

## Step 3: what the district and the maze share

`diff -r` of the two servers (the namespace, the `using` lines, the byte order mark and white space left out, over every project
of the repository) found four files that were the same but for the namespace: `PersonRegistry` (124 lines), `FriendService`,
`LeagueService`, `ItemService`. `QuestService` differed by a comment, and `ChatService`, `PersonProfileService` and
`MovementService` only in who hears a player.

| File | Before | After |
|---|---|---|
| `PersonRegistry` | `DistrictServer/Server`, `MazeServer/Services` | `Server.Common` |
| `FriendService`, `LeagueService`, `ItemService`, `QuestService` | one in each server | `Server.Common/Services` |
| `ChatService`, `PersonProfileService`, `MovementService` | one in each server, overriding the broadcast | `Server.Common/Services`, asking `IPlayerAudience` |
| `IPlayerAudience` | | `Server.Common`: `Chat`, `Nearby`, `NearbyToOthers`, `Move`, `Relocate` |
| `ChannelAudience` | | `DistrictServer/Server`: the channel for the chat, the cells in sight for the rest, the grid for a move |
| `MazeAudience` | | `MazeServer/Services`: the player alone (every player has a maze of their own for now) |

A server registers the shared part with `AddServerCommon<TAudience>()` (`PersonRegistry` and its audience); the handlers are found
by the scan of the loaded assemblies as before, and the call loads the assembly. Behaviour is unchanged: each audience does what
the overrides of its server did, line for line.

- **The name.** `OpenWorker.Server.Shared` was the name of the plan, but CA1716 refuses `Shared`, a keyword of Visual Basic, in a
  namespace, and the project is held to warnings-as-errors. `Common` says the same.
- **Why not `Channel`.** The handlers are concrete, and every concrete handler of a loaded assembly is registered. The gate
  references and loads `Channel` (`WorldManager`, `LeaveManager`), so the handlers there would be registered by the gate too.
  `Server.Common` is referenced by the district and the maze only, and sits in the Channel layer of `ProjectLayersTest`, a peer of
  `Channel` and `Commands` (its chat runs the commands).
- **The keep-alive.** It has no twin. The `KeepAliveService` of the district asks a quiet player to answer and drops one that
  never does; the maze runs none (the line is commented out in its `Program.cs`) and has no handler for the answer, so moving it
  into the shared project and registering it in the maze would drop every player of a maze after five minutes. The
  `KeepAliveService` of the gate renews the record of the gate in Redis: it is a registration and is called
  `GateRegistrationService` now.

### The duplicate check

`eng/check-duplicates.py`, run by CI after the forbidden patterns: every file of every folder `OpenWorker.*` but the test projects
(`*.Test`), `obj/` and `bin/`; two files of different projects are the same when they are equal after the byte order mark is
dropped and white space folded, and, for C# files, after the `using` and `namespace` directives are dropped, so a copy whose
namespace was changed is still a copy. It found the four files above before the move and nothing after; a copy of
`FriendService` put back into the maze turns it red. `EXEMPT` takes a path and a reason; it is empty: the `Program.cs`,
`appsettings.json` and `Dockerfile` of the servers each name their server, and no project file repeats another.

## Step 4: the attribute and the opcode

| File | Before | After |
|---|---|---|
| `OpenWorker.Protocol/Handler/Attributes/HotspotMessageAttribute.cs` | 511 lines, 50 constructors, one per command enum | 18 lines, `(GroupOpcode group, byte command, HotspotMessageDirection direction)` |
| `OpenWorker.Protocol/SoulWorker/Network/DataTypes/MessageOpcode.cs` | 415 lines: 52 implicit operators (to `short`, to `GroupOpcode`, to each of the 50 command enums), 50 typed constructors, a switch of 50 cases for the names | 80 lines |

`MessageOpcode` now has the constructors `(BinaryReader)`, `(byte, byte)` and `(GroupOpcode, byte)`, the generic
`From<TCommand>(GroupOpcode, TCommand) where TCommand : unmanaged, Enum` for code that holds a command in a variable (every command
enum is a byte; a wider one throws), and a public `Value`, the two bytes as a `short`, which indexes the handler table and goes on
the wire. The name of a command for the logs and the metrics (`ToCommandString`) is looked up in the enum named after its group,
`{Group}Opcode` in the namespace of `GroupOpcode`, found once by reflection into an array of 256 types; `MessageOpcodeTest`
checks that every group finds its enum, so a group whose enum is named otherwise is caught.

The messages keep their typed constants: `private const SystemOpcode Command = SystemOpcode.KeepAlive;` stays, and the two uses
give it as a byte, `[HotspotMessage(Group, (byte)Command, ...)]` and `Opcode => new(Group, (byte)Command)`. A generic attribute,
`HotspotMessage<SystemOpcode>`, would have needed the type of the command in every message; the cast is the change a regular
expression makes alone. 686 messages and the 2 test messages were changed by a script; the other callers of the typed
constructors use `From` (16 places), and those of the conversion to `short` use `Value` (the encoder, the handler table, the load
test).

`MessageOpcode.cs` is not generated: nothing in the repository writes it. Its first comment said that it used to carry an
`<auto-generated>` header, and that text in the leading comment is what makes Roslyn take a file for generated, so the analyzers
had never looked at it. The comment is reworded, and the file now holds to CA1305 like the rest of the protocol.

`HandlerTableBuilder` and `HandlerBuilder` read the attribute as before (`Opcode`, `Direction`); the generator of
`OpenWorker.Wire.SourceGenerators` does not read the attribute. The wire tests, the dumps and `HandlerExecutorTest` pass
unchanged, which is the proof that every opcode maps to the index it had; no test was added for the mapping.

## Step 5: the columns of the client tables

### The definition

`Structures.json` declared the types of the columns and nothing else, and the rows no longer matched it: they had been written by
the generator once, then given signed types, names, enum columns and members by hand, so the generator could not be run again.
The definition now says what the rows are, one string per column:

```json
"tb_Item_Classify": [ "Int32", "Byte", ..., "Byte StorageKind", "Byte SlotType", ..., "Byte ItemUseType as OpenWorker.Res.Types.ItemUseType", ... ]
```

The type is the type the file stores (`Int16`, `Single`, `String`, ...), then the name, then the enum the column is read as. A
column without a name is `Id` when it is the first and `Field` with its place otherwise. The generator
(`OpenWorker.SourceGenerator.ResourceStructures`, an executable: `dotnet run --project
OpenWorker.SourceGenerator.ResourceStructures`) writes `OpenWorker.Res/Rows/<Table>Row.cs` from it, a
`public readonly partial struct` with `[BinaryResourceTable("<table>")]`. What a row gives beyond its columns is written by hand in
a partial struct of `OpenWorker.Res/Members` (`CharacterInfoRow.Character` and `GetSkillList`). The generator lost its writers of
seed classes and EF models, which nothing called, and the `System.CodeDom` package it used for the names of the types.
`ResourceRowsTest` fails when a row differs from what the definition writes or when `Rows/` has a file the definition does not
know; changing a name in a row by hand turns it red. The tables of the server (`ServerRows/`) have arrays and comments and stay
written by hand.

The first version of the new definition was made from the rows as they were, and the generator wrote them back unchanged but for
the header and `partial`; the names came after.

### The names

Every column the game or a test reads got a name, 397 columns of 26 tables, found by marking the unnamed columns and building:
the compiler listed every use, and the uses were renamed at the places it reported. The names come from what the code does with
the column (the record it fills, the comment that explained the numbers); none comes from reverse engineering. The comments that
said which numbered column was what went away with the numbers.

| Table | Columns named |
|---|---|
| `ItemRow` | `BindType`, `LevelLimit`, `ClassLimit`, `Endurance`, `PhysicalAttack`, `MagicalAttack`, `PhysicalDefense`, `MagicalDefense`, `StatClass1..5`, `StatType1..5`, `StatValue1..5`, `EffectType`, `EffectValue` |
| `ItemClassifyRow` | `StorageKind`, `SlotType` |
| `InvenSlotExtendRow` | `Step`; for `Common`, `Costume`, `Cash`, `Cube`: `NeedLevel`, `Payment`, `Price`, `Slots` |
| `BankSlotExtendRow` | `Step`; for `Common`, `Costume`: `NeedLevel`, `Payment`, `Price`, `Slots` |
| `ItemPackageRow` | `Gold`, `BattlePoint`, `Ether`, `Item1..10`, `Count1..10` |
| `ItemRandomBoxRow` | `Rolls`, `TotalRate`, `Gold`, `GoldRate`, `BattlePointRate`, `EtherRate`, `ItemRate1..15`, `BattlePoint`, `Ether`, `Item1..15`, `Count1..15` |
| `CreateClothRow` | `Item1..5` |
| `QuestEpisodeRow` | `CompleteBit`, `Contents`, `PreviousEpisode1..5`, `Level`, `Hero`, `FinishesByItself`, `CannotGiveUp`, `AcceptGives1..2`, `AcceptItem1..2`, `TakenItem1..5`, `TakenCount1..5`, `RewardExperience`, `RewardGold`, `RewardEther`, `RewardBattlePoints`, `RewardPrefix`, `RewardSuffix`, `RewardItemsBy`, `RewardItem1..5`, `RewardCount1..5`, `SelectItemsBy`, `SelectItem1..5`, `SelectCount1..5`, `RewardHelper`, `Condition1..10` |
| `QuestConditionRow` | `Type`, `Previous`, `Target`, `TargetId`, `Maze`, `Count`, `GivenItem1..2`, `GivenCount1..2`, `TakenItem1..2`, `TakenCount1..2` |
| `QuestRewardRow` | `HaruItem`, `ErwinItem`, `LilyItem`, `JinItem`, `StellaItem`, `IrisItem` |
| `MonsterRow` | `ActionFile`, `Faction`, `QuestGroup1..5`, `MissionGroup1..3`, `WalkSpeed`, `RunSpeed`, `TurnSpeed`, `GazeSpeed`, `AiScript`, `StandType`, `Skill1..10` |
| `SkillRow` | `SkillPoints`, `Angle`, `RotationType`, `Cooldown`, `CooldownGroup`, `GlobalCooldown`, `Duration` |
| `BuffRow` | `DurationMilliseconds` |
| `BoosterRow` | `Group`, `Effect1..8`, `Apply1..8`, `Value1..8`, `TimeType`, `DurationMilliseconds` |
| `DailyMissionRow` | `Type`, `District`, `Finish`, `Target`, `TargetId`, `TargetFaction`, `ConditionType`, `ConditionValue`, `ConditionClass`, `ConditionCount`, `MissionCount`, `OpenDate`, `CloseDate`, `RewardItem1..3`, `RewardCount1..3` |
| `SoulMetryRow` | `CompleteBit`, `Maze1..5`, `Object1..5`, `SpawnRate1..5`, `RewardType1..2`, `RewardItem1..2`, `RewardCount1..2` |
| `TitleInfoRow` | `EffectGroup1..5`, `EffectType1..5`, `EffectValue1..5` |
| `CheckAttendanceInfoRow` | `Days` |
| `CheckAttendanceRewardRow` | `Gold`, `Ether`, `Item1..5`, `Count1..5` |
| `CheckAccessRewardRow` | `Seconds1..3`, `Item1..3` |
| `CustomizeHairRow` | `Style1..10`, `Color1..10` |
| `CustomizeEyesRow`, `CustomizeSkinRow` | `Color1..10` |
| `HelperRow` | `Number` |
| `LeagueInfoRow` | `MemberLimit` |
| `LevelupPointRow` | `Experience` |

The names given by hand before stay (`CharacterInfoRow.Default_Cloth_ID_01..04`, `District_ID`, the speeds, `ItemRow.Classify`,
`Price`, `Stack`, the columns of `MonsterRow`, `SkillRow`, `StatusRow`, `DivergenceRow` and the others). `CharacterInfoRow.Field24`,
which a test reads only to say that it is no costume, keeps its number, as does every column nothing reads, such as the fourth of the five
columns of a step of a storage (`Field5`, `Field10`, ...).

### The searches

`RowIndex<TKey, TRow>` (`OpenWorker.Res`, made with `rows.IndexBy(e => e.Id)`) is a dictionary made once by the owner of a table,
when the singleton is made. It keeps the first row of each key, which is the row a search from the top of the table found, and a
key the table does not have is `TryGet` returning `false` or `GetOrDefault` returning the empty row (every column zero, which the
callers already tested for). `RowIndexTest` checks both.

| Where | Was | Now |
|---|---|---|
| `ItemCatalog.Extension` (the size of every storage, at entry and at every extension) | `foreach` over `InvenSlotExtendRow` / `BankSlotExtendRow` until `Step` matched | `RowIndex<int, …>` by `Step` |
| `ItemCatalog` items and kinds | `GroupBy(Id).ToDictionary(First)` | `RowIndex` by `Id` (same result, the shared accessor) |
| `ItemUseService.OpenPackage`, `OpenRandomBox` (every use of a box) | `foreach` until `Id` matched | `RowIndex` by `Id` |
| `ShopService` buy | `shopList.First(Id)`, `itemList.First(Id)`: an exception on the loop for a line the client made up | `RowIndex` of the shop, `ItemCatalog.TryGet`; a missing line or item buys nothing, as a light purse does |
| `SkillService` learn and starting skills, `PersonSkills.DefaultSkills` | `FirstOrDefault` / `foreach` over skills and heroes | `RowIndex` by `Id` and by `Character` |
| `CombatTables.GetPerson` with gear | `FirstOrDefault` over `CharacterInfoRow` | `RowIndex` by `Id` |
| `SoulMetryManager` reward of a class | `foreach` over `QuestRewardRow` | `RowIndex` by `Id` |
| `BuffManager.Apply` (a script asks) | `FirstOrDefault` over `BuffRow` | `RowIndex` by `Id` |
| `LeagueServiceBase.MaxMembers` | `FirstOrDefault` over `LeagueInfoRow` | `RowIndex` by `Id` |
| `InventoryFactory` outfit of a new person | `FirstOrDefault` over `CreateClothRow` | `RowIndex` by `Id` |
| `PersonCreationCatalog` hero, hair, eyes, skin (every character created) | `FirstOrDefault` × 4 | `RowIndex` × 4 |
| `MazeResourceProvider`, `DistrictResourceProvider` (the map of a world) | `FirstOrDefault` | `RowIndex` by `Id` |
| maze `PersonService.TryGetMazeInfo` (every entry to a maze) | `foreach` over `MazeInfoRow` | `RowIndex` by `Id` |
| `LuaMaze.RegisterCubes`, `MazeProgress` (every maze) | `FirstOrDefault` per cube, a `Join` over the whole table per maze | one `RowIndex` of `InteractionObjectRow` for the server, a probe per box |

Left as they are: `NpcManager` (`First` over `NPCRow` once at start, where a missing row should stop the server),
`SkillActionProvider` (at start), `ItemCommand` (`All` over the items in a test command of the chat; its dependencies come from the
command generator), and the catalogs that already made dictionaries (`DropService`, `ExperienceService`, `CombatTables`,
`MonsterBehaviorTables`, the catalogs of boosters, missions, soul metries, titles, attendance, helpers).

## Warnings

`dotnet build OpenWorker.slnx -c Release --no-incremental`: 169 warnings before, 64 after, 0 errors.

| | Before | After |
|---|---|---|
| CA1707, CA1708, CA1069 on client enums (`ChangeSpawnActionType`, `LayerBitmask`, `ItemUseType`) | 92 + 1 + 4 | 0, path-scoped in `.editorconfig` with the reason |
| CA2201 (`ApplicationException`) | 5 | 0: the generator lost them, a batch file without its boxes is `InvalidDataException` |
| CA1848 | 14 | 13: `SkillActionProvider` logs through `[LoggerMessage]` |
| CA1720 | 5 | 3: `TimeSpanSize.Int32` and `Int64` in `.editorconfig` (the width a time span is written in) |
| the rest | 48 | 48, in `AuthServer`, `DependencyInjection`, `DistrictServer`, `GateServer.Gameplay`, `Havok`, `RelayServer` and the tests |

`Domain`, `Extensions`, `Res` and `Batch` build clean now and treat warnings as errors, as do the two projects that are new or
rewritten here, `Server.Common` and `SourceGenerator.ResourceStructures`.

## Found on the way

`LuaCreatureManager.MoveToPoint` took `FirstOrDefault` of the creatures and asked the result `IsAlive()`. For no match that is the
default entity, which belongs to the world of id 0; once a test (or a maze) had destroyed that world, the call threw
`NullReferenceException`. `LuaMemberTest` failed on `dev` every time the client data was set (the coordinator's run sets it). The
search is an `Exists` over the living creatures now; the answer is the same, a log line when the creature is not there.

## Verification

- Build: 0 errors, 64 warnings.
- Tests with Redis, Postgres and the client data (`RES`, `AI`, `ACTION`, `NAVMESH`): 1060, 1055 passed, 0 failed, 5 skipped
  (`OPENWORKER_TEST_SERVER_RES`); `check-trx.py --allow-skip 'OPENWORKER_TEST_SERVER_RES is not set'` exits 0.
- Tests with Redis and Postgres only: 1060, 971 passed, 0 failed, 89 skipped (78 `RES`, 2 `AI`, 1 `ACTION`, 3 `NAVMESH`, 5
  `SERVER_RES`); `check-trx.py` with the allow list of CI exits 0.
- `eng/check-forbidden.sh`: no forbidden patterns. `eng/check-duplicates.py`: no file repeated across projects.
- `dotnet ef migrations has-pending-model-changes`: no changes.
- `wc -l`: `HotspotMessageAttribute.cs` 18, `MessageOpcode.cs` 80.

## Not done here

- `SkillService.GetDefaultSkills` of the district and `PersonSkills.DefaultSkills` of the game logic say the same thing twice; the
  district's could call the other.
- `ShopService` keeps a `Debug.Assert` on the count the client sends and two constructor parameters it never reads.
- The columns nothing reads keep their numbers; naming them waits for code that reads them.
- 64 warnings are left, none of them in a project that treats warnings as errors.
