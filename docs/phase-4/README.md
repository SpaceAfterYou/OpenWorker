# Phase 4: persistence and state

Goal of `REFACTORING.md`, phase 4: everything the player sees survives a logout and a crash of the process, and the
database is not the bottleneck. Branch `refactor/phase-4`, from `dev` at `914bfc6`.

## 1. What was already done when the phase started

Section 2.4 of the plan was written before the merges of 2026-11-08 (inventory, levels, rewards, quests, titles,
friends, leagues, rankings). The state of the code at `914bfc6`, read before any change:

| Item | State at `914bfc6` |
|---|---|
| 2.4.1 items | Done. `ItemPersistent` is the working model (`DbSet Items`, migration `20261108173120_Items`: storage group, slot, prototype, count, options, enhancement, serial from the sequence `ItemSerial`, index `(PersonId, Storage, Slot)`). `InventoryPersistence` loads the items at entry, the gate writes the start kit (class weapon and outfit) of a new character. |
| 2.4.1 level, experience, gold, battle points, ether, storage grades, quick slots | Done: columns of `Persons`, loaded at entry, written with the items. |
| 2.4.1 other currencies | Not kept: cash, tokens and mileages had no column, and both `PersonService` files set cash to 7 000 000 at every entry. |
| 2.4.1 skills and decks | Not kept: skill points (32 / 16), learned skills (the defaults of the class), decks (none, 4 slots) and deck bonus (`[1, 2, 3, 4]`) were hard-coded in the district's `PersonService`. |
| 2.4.1 quests | Done as a progress document (`PersonStates`, kind `Quest`, jsonb), like soul metry, daily missions, boosters, attendance and helpers. Each change awaited its own write (read the row, change it, save: two round trips). |
| fatigue points | Columns existed, both `PersonRegistry` files ignored them (100 / 100). |
| 2.4.2 position | Written only when the connection closes (`PersonPlaceStore`). |
| 2.4.3 entry without reservation | Done in phase 0: district and maze refuse a connection without its reservation. |
| Step 3 write-behind | Partial. `InventoryFlushService` wrote the items and the row every 5 s, person after person, one transaction and one `UPDATE` per changed item each, under a semaphore per person; the documents were written by the handlers themselves; no flush on a move to another server. |
| Step 5 Postgres | `PersonStates(PersonId, Kind)` is the primary key, so the unique index exists. No pool limit (the default 100 per process), tracking by default with `AsNoTracking()` per query, no compiled query. |
| Step 6 Redis | Nothing: sequential lease renewal, `GateSyncService` every second with `Include(Persons)`, maze choice by loading every record, `EVAL` with the script text, `GetDistrictAsync` with two sequential searches (the `AnyAsync` of 2.3.2 was gone). District and channel records without a lifetime. |
| Step 7 bootstrap | Destructive: the relay dropped every index with its records on start. |
| Step 8 account options | Kept on every person, loaded from the first one, written to all of them. |

So the work of this phase is: the missing model (wallet, skills, account options), loading the rest at entry, the dirty
mask and one flush for everything, the pools, and all of the Redis and bootstrap items.

## 2. Model and migration

One migration, `20261108232548_AccountOptionsAndWallet`, additive:

- `Persons.Currencies bigint[]` (default empty): the whole wallet by the currency of the shop (cash, tokens, mileages).
  Gold, battle points and ether keep their columns and win when both are there, so rows written before keep them.
- `Accounts.OptionList bytea` (default empty), filled in the migration from the person of the account played last
  (`DISTINCT ON ("AccountId") ... ORDER BY "LastSelectedAt" DESC`). `Persons.OptionList` stays, read by nothing; a
  later migration can drop it.
- Skills and decks are a progress document of their own, kind `Skills` (7) in `PersonStates`, like quests. No table: the
  document is small, read once at entry and written whole (`SkillBook`: points, learned skills with divergence, decks,
  deck slots, deck bonus).

Checked: the whole chain applied to an empty database (`openworker_p4_fresh`), rolled back one step to `Items` and
applied again; `has-pending-model-changes` reports none.

## 3. Loading at entry

- District and maze load the person and the options of its account with one compiled query (`PersonQueries`).
- The wallet comes from `Currencies` and the three columns; nobody gets 7 000 000 cash any more. A GM gives cash with
  `/currency <amount> Cash` (the command now marks the wallet, so it is kept).
- Fatigue points come from their columns.
- Skills come from the `Skills` document (`PersonSkills.LoadAsync`); a person that has none starts with the skills of
  its class and the start points (32 total, 16 free, 4 deck slots, deck bonus 1 to 4), which are then kept.
- The options of the client come from the account, in the gate as well.
- The start kit of a new character (class weapon and outfit) is not a test item: the gate writes it when it creates the
  character. There were no test items left in the `PersonRegistry` files.

## 4. Write-behind

**Mask.** `DirtyComponent` holds a `PersonDirty` (a reference, so a mark cannot be lost to a copy of a component read
before an `await`) with `PersonSections`: `Place`, `Currencies`, `Level`, `Skills`, `Quests`, `Inventory`,
`MiscState`. It is attached when the items of the person are loaded, so a connection that never entered writes nothing.
Who marks: `InventoryService` (currencies), `ExperienceService` (level), `Inventory` itself (placing, touching,
destroying, resizing, quick slots), the district's `SkillService` (learn, divergence, decks, bonus, reset),
`QuestManager` (every change), the managers of soul metry, daily missions, boosters, attendance and helpers, league
creation (gold), `/currency`. The place is not marked: `PersonPlaceStore` of the district gives the place of a player
in this district, and the snapshot writes it when it differs from the one written last (a mark on every move would be
the same thing at a higher price, and the movement code is phase 3's).

**Snapshot.** `PersonSnapshots.Take` runs on the loop: it takes the mask, the item changes of the inventory (added,
changed, removed), the row (level, experience, gold, battle points, ether, the wallet, storage grades, quick slots), the
place, and the marked documents serialized to JSON (`IPersonDocumentSource`: skills, quests, the five misc documents).
Restoring a failed write marks the sections again and gives the items back to the inventory.

**Batch.** `PersistenceWriter` (an `IHostedService` with one consumer task) takes everything queued and builds raw
statements (`ItemStore.SaveChangesAsync(IReadOnlyList<PersonChanges>)`): an item is `INSERT ... ON CONFLICT ("Id") DO
UPDATE` (so a retry of a write that did reach the database is harmless), the removed items one `DELETE ... = ANY(@ids)`,
the row and the place an `UPDATE` by key, a document `INSERT ... ON CONFLICT ("PersonId", "Kind") DO UPDATE` (this also
replaces the read-modify-write of `PersonStateStore.SaveAsync`, 2.3.4). Up to 256 statements per command, one round trip
each, one transaction for the batch (a single command is atomic by itself). A failed batch is written person by person;
a person that still fails is tried again after 2 s, up to 5 times, then given up with an error and its sections marked
again if it is still on the server. A flush with nothing to write is a barrier: it ends when the writes queued before it
are settled, retries included. The snapshot and the queue happen in one callback of the loop, so the writes of a person
reach the database in order.

**Intervals and flush points.**

| When | What | Awaited by |
|---|---|---|
| every `Persistence:FlushInterval` (30 s) | every player with a mark or a new place | nobody (the service) |
| a connection closes (logout, kick, drain) | that player (`PersonFlush` as an `ISessionCloseHandler`, where `InventorySaver` was) | the close |
| before a reservation: `WorldManager.EnterMaze`, `TryEnterDistrict`, the exit of `MazeService`, `LeaveManager` (gate, server list) | that player (`IPersonFlush`) | the handler, before the reservation is written |
| the host stops | after the drain (whose closes flush), `PersistenceFlushService.StopAsync` takes a last round, then `PersistenceWriter.StopAsync` writes what is queued | the host |

The writer is registered before the flush service, so the host, which stops services in reverse order, stops the flush
first and the writer last; the world loop is disposed after both.

**What a `kill -9` loses:** the changes since the last round, at most `FlushInterval` (30 s by default), of the players
still on the server, and a batch that was in flight. Logouts and moves to another server that finished are in the
database.

**The timer of `InventoryPersistence`** (5 s, person by person) is folded into the flush and removed, with
`InventorySaver`. `InventoryPersistence` loads, and saves the start kit for the gate (`SaveItemsAsync`), which has no flush.

**Await doors of rule 2 that are gone:** the saves of the quest book (nine places in `QuestManager`), of soul metry,
daily missions, boosters, attendance and helpers (one after every change each), the per-person writes of the old
inventory flush, and the write of the place at close (now part of the one close flush). Added: the flush before a
reservation and before leaving for the gate, where the handler already waited for Redis.

## 5. Postgres

- `Maximum Pool Size` in every `appsettings.json`: gate, district, maze 16; auth 8; relay and UpdateContent 4. With the
  compose stack (four districts) that is at most 112. `compose.yaml` runs Postgres with `max_connections=200` (the
  load-test overlay had the same override, removed there). Before: no limit, the default of Npgsql of 100 per process,
  and 96 connections held by the idle stack against the default of Postgres of 100 (2.9.5).
- `UseQueryTrackingBehavior(NoTracking)` by default (`UsePersistenceDefaults`, also used by the in-memory context of the
  tests). `AsTracking()` where a loaded entity is changed or removed: `RankingStore.SubmitRankingAsync`,
  `FriendStore.InviteAsync` and `AnswerAsync`, `LeagueStore.SetPositionNameAsync`, the creation (account, gate), deletion
  and selection of a person in the gate, the second password, and the Discord commands of the relay.
- Compiled queries (`PersonQueries`): the person with the options of its account at entry, the account with its persons
  and their worn items at the gate, the account by name at login. One compilation per model, because a compiled query is
  bound to the model it first ran with (a test host makes several).
- Indexes: `PersonStates(PersonId, Kind)` is the primary key, so it is the unique index the plan asks for. Reviewed
  against the queries of the stores: every query of an entry and of the flush runs by a key or an existing index
  (`Persons` by key and `AccountId`, `Items (PersonId, Storage, Slot)`, `PersonStates` by key, friends by
  `(OwnerId, TargetId)` and `TargetId`, league members by key, applicants by `PersonId`, rankings by
  `(Kind, Scope, PersonId)`). None was added: what scans (`Accounts.Discord` in the commands of the relay, `lower(Name)`
  when a league is founded, the sort of a ranking board) is rare and off the path of a player.

Round trips, before and after:

| Operation | Before | After |
|---|---|---|
| a quest or misc change | 2 (read the row, save), awaited by the handler | 0 in the handler; one statement in the next batch |
| the inventory flush | every 5 s for each changed person: begin, one `UPDATE` per changed item, the inserts, the row, commit | every 30 s: one command per 256 statements for all players |
| the place | one `UPDATE` at close | one statement per round when it changed, and at close |
| saving the account options | read all persons of the account, update each | one `UPDATE` of the account |
| `GateSyncService` | every 1 s: all gates with all their persons | every 15 s: the gates |

## 6. Redis

| Operation | Before | After |
|---|---|---|
| lease renewal (every 30 s) | 2 per lease, one after the other (`FindByIdAsync`, `InsertAsync`) | 1 `EVALSHA` per 500 leases ("renew if owner": `HGET Owner`, `PEXPIRE`) |
| least busy maze | 1 `FT.SEARCH` returning every record of the gate | 1 `EVALSHA` over the sorted set `ow:mazes:<gate>` (drops a member whose record expired) |
| maze registration (every 10 s) | 1 | 2, sent together (the record and `ZADD`) |
| presence removal | 2 `EVAL` with the text of the script | 1 `EVALSHA` for both keys |
| `GetDistrictAsync` | 2 `FT.SEARCH` one after the other | 2 sent together (districts and channels by location): one round trip |
| gate list of the login server | `FT.SEARCH` every 1 s | every 15 s |

`RedisScript` loads a script once (`SCRIPT LOAD`), runs it by its hash and loads it again on `NOSCRIPT`. The test of the
lease renewal profiles the multiplexer: fifty leases, one `EVALSHA`.

Lifetimes: the district record expires after 30 s and `ServerBootstrapService` renews it every 10 s; the channel records
get 30 s, renewed by the write of their count every second (`ChannelLifecycleService`, which writes a record again if it
expired); the gate record gets 30 s, renewed every second by `KeepAliveService`. The records of a crashed server go by
themselves; the orderly stop still removes them at once.

## 7. Bootstrap of the relay

`RedisIndexes.EnsureAsync`: for each document type, a signature of its schema (SHA-256 of the document attribute and of
every Redis.OM attribute of its properties with their values, plus a version constant) is compared with the one kept in
the hash `ow:index-schema`. Same signature: `FT.CREATE` only if the index is missing. Different or none: the index is
dropped without its records and created again, and Redis indexes the records that are there. No record is deleted.
Starting the relay while servers run keeps their sessions, reservations and registrations; the known limit is removed
from `ARCHITECTURE.md`. Tested against Redis with a document of the test's own: the bootstrap twice keeps the record and
its search, and a changed signature rebuilds the index over the record.

## 8. Account options

`PersonOptionStore` reads and writes `Accounts.OptionList`, one statement each; district, maze, gate and the profile of
another person read them from the account.

## 9. Tests

New: `PersistenceTest.ALogoutAndANewLoginKeepWhatThePlayerHad` and `AFlushWritesTheDirtyPlayerAndNotTheCleanOne`
(Postgres, `OPENWORKER_TEST_POSTGRES`, a database of their own per run, dropped after; CI sets the variable against its
Postgres service), `TestRegistrationRecords.TheLeastBusyMazeServerIsPickedAndOneWhoseRecordExpiredIsDropped` and
`TheBootstrapOfTheRelayKeepsTheRecordsWhenItRunsAgainAndWhenTheSchemaChanges` (Redis). Extended:
`TestSessionLease.RenewExtendsOnlyOwnedLeasesInOneRoundTrip` (was `RenewExtendsOnlyOwnedLeases`), the quest tests (a
change marks the quests and the snapshot holds it), the experience test (the level is marked).

## 10. What is left

- Not measured here: Postgres and Redis calls per bot and a `kill -9` with bots (the coordinator measures after the merge).
- The entry still reads each progress document in its own query (seven); one query for all kinds of a person would save
  six round trips.
- Skill points do not follow the level (the client counts them against it).
- `Persons.OptionList` is dead and can be dropped by a later migration; the backfill ran in the migration itself.
- The channel records are renewed by the count loop of `ChannelLifecycleService`; phase 3, which makes the counts follow
  joins and leaves, has to keep a renewal.
- A changed index schema is rebuilt under running servers: searches by that index fail for the moment of the rebuild.
- The close handlers run in the order of registration, so the lease is released before the flush of the player ends; a
  new login reaches a district through auth and the gate, which takes longer than the write.
