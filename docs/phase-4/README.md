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
