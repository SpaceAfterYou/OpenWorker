# The progress of a person: achievements and share points

The window of the achievements of the client has two parts that need the server: the achievements themselves (03/70,
03/71, 03/74) and the share points of the districts and of the groups of mazes (03/61, 03/62, 03/58, 03/63). The client
1.4.2.3 decided every layout below (its receivers and the readers they call were read again for this work); the reference
server gave the rules, the order of the checks and the error codes. Code: `OpenWorker.Gameplay/Modules/Progress`, the
handlers in `OpenWorker.Server.Common/Services/ProgressService.cs` (district and maze), the entry in the `PersonService` of
each server, the counting at the places listed under "Which achievements count".

## What is stored

Two progress documents of the person (`PersonStatePersistent`, the table `PersonStates`, one row per person and kind), as
the quests, the skills and the daily missions are kept:

| Kind | Document | Holds |
|---|---|---|
| `PersonStateKind.Achievements` (9) | `AchievementBook` | a counter per chain of achievements (type, target, the achievement it counts toward, the count), and the 128 bytes of the rewards taken, one bit per achievement by its `CompleteBit` |
| `PersonStateKind.SharePoints` (10) | `SharePointBook` | per district: point, reward taken; per group of mazes: the maze cleared last, the district, clears, point, reward step |

A change marks `PersonSections.Progress` on the dirty mask; the flush writes both documents (`AchievementManager.Document`,
`SharePointManager.Document`), in the maze as in the district, so a clear in a maze is written by the flush of the maze
before the player goes back, and the district reads it at the next entry. The key of the table, `(PersonId, Kind)`, makes a
document unique; a document has one entry per chain, district and group by construction (`Normalize` drops repeats and what
the tables no longer have). **No change of the EF model**: the kinds are new values of an enum stored as a number, and
`dotnet ef migrations has-pending-model-changes` reports nothing. A table per counter would have needed its own write path
in the flush for no gain: nothing but the server that holds the person writes these.

How many achievements of each category are reached (the seven numbers the client shows) is not kept: it follows from the
counters and is counted when a message needs it.

## The tables

- `tb_Achievement` (client): columns named in `Structures.json`: category, group, order, `CompleteBit`, icon, type, next,
  name, count, gold, ether, battle points, two titles, two kinds of item, two items, two counts. The client's copy has **no
  target**. The server's own copy (`AchievementTargetRow`, `res-server/tb_Achievement.res`, `Server = true`) has the target
  and, for eleven rows, another type; only those two columns are taken from it. Rows the client lacks are not used: the
  client must know every achievement it is told about.
- `tb_Achievement_begin` (client): the first achievement of each chain and the level at which it starts counting.
- `tb_DS_Point` (client): districts (kind 0) and groups of mazes (kind 1), the points of each step, the reward of each step
  (kind 2 = a row of `tb_Quest_Reward` by class, else the item). The client's copy has **neither the district of a group nor
  the points a clear adds**. The server's own copy (`SharePointRow`, `res-server/tb_DS_Point.res`) has both, and its rewards
  differ from the client's; when it is there it is used whole, restricted to the ids the client's table has (the client
  looks every district and group up in its own table and does not check the result).

Without the server's tables: achievements count with target 0, and the six types whose chains then collide (19, 25, 47, 48,
53, 57) are left out; districts get their state and their reward, but no clear adds a point.

## Share points

- **Entry of a district.** The district sends 03/61 (every district state: `u8 count`, then `int district, int point,
  u8 rewarded`; the client counts in a signed byte) and 03/62 (every group: `u16 count`, then `int maze, int group, int
  district, int clears, int point, u8 step`), districts first: the client files a group under its district and drops a group
  whose district it does not know. The client reads both only while it enters the world from the gate (its login scene); in
  the play scene it has no receiver for them, so they are sent at every district entry and ignored after the first. The
  district the person is in gets a state when it has a row and none yet, first, so that it is in the list for a client
  that comes from the gate, and also told by 03/58 with an empty group for a client already in the world.
- **A cleared maze** (`MazeReporter.MazeClearedAsync`, maze server): the group of the maze (`tb_Maze_Info.Group`) gets the
  points of the episode of the maze (`tb_Maze_Info.Episode`, 0 to 4, more counts as 4: the first value of the row for
  episode 0, then one per episode), up to the points of its last step; one more clear; the maze cleared last. The district
  of the group gets what the group got (nothing once the group is full), up to the points of its reward; a district with no
  state gets one. 03/58 carries both states (`9 + 21` bytes).
- **03/63** (`int type, int id`): type 0 is a district, anything else a group. The answer always goes: `int id, u8 step,
  u8 result`, then `u8 count` and `(int item, i16 count)` pairs.

  | Case | step | result |
  |---|---|---|
  | district with no state | 0 | 1 |
  | district already rewarded | 1 | 1 |
  | district without a row | 0 | 3 |
  | district under the point of its reward | 0 | 1 |
  | district given | 1 | 0 |
  | items of a district do not fit | 0 | 2 |
  | group without a row | 5 | 3 |
  | group with no state | 0 | 1 |
  | group with nothing new reached (or every step taken) | its step | 1 |
  | group given: every step from its own whose points it has | the new step | 0 |
  | items of a group do not fit | its step | 2 |

  The client shows message 702 for result 2. Items go through `InventoryService`, all or none; an item the item tables do
  not have is left out, as is a reward by class with no item for the class.

## Achievements

- **Chains.** Achievements follow each other by number in a chain of one type and target: a counter of the person goes
  through the chain, and an achievement is reached when the counter is at its count (the counts of a chain grow, so the
  counter is a running total). The level (32) and the combo (27) keep the highest value instead of a sum.
- **Entry** (district and maze): the book is loaded, the chains whose begin level the person has start, the counter of the
  level is set to the level, every counter moves past what it has reached, and a reward bit of an achievement not reached is
  cleared. 03/70: `128 bytes of reward bits, u8 count, (int achievement, i64 count) per chain, 7 × u16 per category`.
- **An event** moves the counter of its type and target. 03/71 carries what moved: `u8 count`, then per counter `int
  achievement, i64 count, int next, u8 category, u16 count of the category` (next is 0 while the achievement is not
  reached), then the reward bits and the seven counts. When a counter reaches an achievement it moves to the next one of the
  chain, past every one it has also reached; at the end of the chain it stays on the last one, at its count. A level that
  opens new chains sends 03/70 again first.
- **03/74** (`int achievement`): given once, when the counter of its chain is at its count and its bit is not set. Checked in
  this order: gold, battle points, ether of the reward over 99 999 999 999 999 with the wallet → error 52056, 52057, 52058;
  items that do not fit → error 52013; then the items (plain or by class), the bit, the gold, battle points and ether
  through the wallet, the answer `int achievement`, then the titles (`TitleBook`, those the class can use). An achievement
  that is unknown, not reached or already rewarded gets no answer, as with the reference server; the client asks only for
  one it shows as ready.

## Which achievements count

| Type | Counts | Where |
|---|---|---|
| 19 | a monster killed, by its number and by its type | `MazeReporter.MonsterKilledAsync` |
| 20, 21, 22, 44, 23, 24 | a monster of rank 0, 1, 2, 3, 4, 5 killed | same |
| 25 | a monster killed, by its faction | same |
| 28, 30, 29 | a main, side, repeat quest finished (the client's table has 30 only) | `QuestManager` after the reward |
| 31, 45 | gold, ether picked up in a maze | `DropService` |
| 32 | the level | `ExperienceService` at a level change |
| 35 | a maze cleared, by maze and for any maze | `MazeReporter.MazeClearedAsync` |
| 36 | a maze cleared within three minutes | same |
| 37 to 42 | a maze cleared at rank SS, S, A, B, C, F (`docs/notes-maze-results.md`) | same, with the rank of the run |
| 47 | a maze of a kind other than 0 cleared, by kind | same |
| 48 | a helper gained, by helper | `HelperManager.AddAsync` |
| 15 | an item used, whatever its kind, once the use starts its cooldown | `ItemUseService` |
| 16 | a purchase at a shop, once per request | `NpcShopService.Buy` |
| 17 | the gold a repair costs, by any of the four ways | `ItemRepairService` |
| 18 | a death in a maze | `MazeResults.OnHealthChanged` |
| 26 | a monster hit by an attack of the person | `MazeResults.OnMonstersHit` |
| 27 | the longest combo (the counter keeps the highest) | same |
| 33 | battle points added to the purse from anywhere | `InventoryService`, through `ItemArrivals` |
| 43 | the minutes of play | the tick of the misc work, whole minutes |
| 49, 50 | an upgrade tried, an upgrade that did not go up | `ItemUpgradeService` |
| 51, 52 | a request to make items, a request to take items apart | `ItemMakeService`, `ItemDisassembleService` |
| 53 | a daily mission finished (target 0) | `DailyMissionManager` |
| 56 | a ride of an NPC | `TransportService` |
| 57 | the credit points gained with a group, as far as the day allowed (target is the group) | `NpcCreditManager.Raise` |

Not counted, with what they need: 46 (a maze of the infinite tower), 54 (visits of a room) and 55 (purchases of the exchange)
wait for their features, and the hidden events of 53 (target 1) for hidden events. The reference server also opens titles when
an achievement is reached or a level is: that is built (`OpenTitleManager`, `notes-maze-modes.md`); the titles of the reward are
given when the reward is taken.

## Assumptions and what to check with the real client

- That the client in the play scene ignores 03/61 and 03/62 (it has no receiver there) without a message.
- That 03/58 with an empty group, sent for the first visit of a district, shows nothing wrong: the client then looks up a
  group 0, which it does not have.
- That 03/70 sent again after a level opens chains rebuilds the window cleanly.
- That the client shows the reward bit taken after 03/74 (it sets it itself from the answer).
- 52013 for items of an achievement that do not fit is ours; the reference server answers nothing.
- A group cleared for the first time starts at one clear (the reference starts at zero); the client does not show the count.
- A group with nothing new reached answers result 1; the reference answers 2 (message 702). The client sends the request only
  for a group it shows as ready.
- The reward of a share point comes from the server's table, which differs from the client's: the window may preview other
  items than those given.

## Verified

- Unit tests (`ProgressTest`): a cleared maze grows its group and district, the reached step is given once; the district
  reward is given once; an achievement reached gives its reward once, the next is refused until reached, an overflow of gold
  is refused with 52056 before anything is given. `PersistenceTest` keeps a counter, a reward bit, a district and a group
  through a flush and a new entry against Postgres. `ServerTablesTest` reads both server tables to their trailer.
- Stand with the server tables mounted (`docs/agents/feature-person-progress/e2e_progress.py`, 17 checks): 03/61, 03/62,
  03/70, 03/58 at the first entry in the client layouts, 03/63 answers for a district, a group and an unknown group, no
  answer to an achievement not reached, the maze entry, and the state of the district back from the database after a
  logout, without 03/58 the second time.
