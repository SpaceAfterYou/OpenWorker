# The end of a maze, and what a maze costs

Branch `feature/maze-results`. A cleared maze is now scored the way the reference server scores it: the server counts what
each member does while it plays, turns it into a score with the reward tables of the server, ranks it from F to SS, and
gives the experience, the gold and the item of that rank. Entering a maze spends fatigue and is refused without it, and
the portal checks what the maze asks for. The client 1.4.2.3 decided every layout below (its readers of 11/75, 11/76, 03/64
and its result window were read again for this work); the reference server gave the rules, the tables read and the codes.

Code: `OpenWorker.Gameplay/Modules/Drops/MazeScoring.cs` (the score), `MazeResults.cs` (the run and the clear),
`MazeKeyRewards.cs` (the item of the rank and the key reward), `OpenWorker.Gameplay/Modules/Maze/FatigueService.cs`,
`MazeAdmission.cs`, the server rows in `OpenWorker.Res/ServerRows/` (`MazeRuleRow`, `MazeRewardStandardRow`,
`MazeScoreRows.cs`), the records in `SharePointBook`, the reports in `OpenWorker.Lua/MazeReporter.cs`.

## What a run counts

Per member of the instance (`MazeResults`, a tally from the moment the player comes into the maze server):

| Counter | Counted where | Used for |
|---|---|---|
| Kills | `KillRewards` (the killer) | the points of kills |
| Longest combo | `LuaMaze.MonstersHit`: an attack that lands within 3 s of the last one adds the targets it hit (at most five), one that comes later starts a new combo at the targets less one | the points of the combo, the window |
| Damage done | `LuaMaze.MonstersHit` | the share of the damage of the members, in percent |
| Hits taken | `PlayerVitals.HealthChanged`, every time the life goes down | the percent of the score kept |
| Deaths | the same, when the life reaches zero | nothing yet |
| Experience, gold, ether | kills, the party share of a kill, what is picked up, and the clear itself | the totals of the window |

What the reference also counts and this server does not: the points of the skills used (a column of the server's skill table
that the client's table does not have), climax, erosion and collaboration (counted by the reference but not part of its
score).

## The score

`MazeScoring.Score`, in the order of the reference:

1. The points of the clear (`tb_MazeReward_Standard`, by maze), plus the points of the size of the party (one to four).
2. The longest combo times the factor of the hero (`tb_MazeReward_Revision`), at most 500, gives the points of row combo + 1 of
   `tb_MazeReward_Normal`; enough kills (the count of the standard row) give its points of kills, too few its other points;
   the share of the damage gives the points of row share; then the whole is kept at the percent of row hits-taken.
3. Times one plus `steps × value` (`tb_MazeReward_Time`, its first row): the steps are the whole seconds left of the clear time
   of the maze (the server's `tb_Maze_Info`) divided by the step, at most the most steps; a run past its clear time counts
   one second left.
4. Times the factor of the difficulty of the maze (`tb_MazeReward_Difficulty`, row difficulty + 1), then of how far the level
   of the member is above the level of the maze (`tb_MazeReward_Level`, row = the gap).
5. The rank is the first of the five points of the standard row (C, B, A, S, SS) that the score does not pass: 0 (F) when it
   does not pass the first, 5 (SS) above them all.

A row that is missing counts as a factor of one; a maze without a standard row has no score: F, with the whole of its
experience and gold, as the reference leaves it.

**A worked example** (the unit test): 21311, Haru at level 14, a longest combo of 50 (factor 1.5: row 76, 4950 points),
six kills (2000), all of the damage (row 100, 10000), 21 hits taken (97 percent), alone, 200 seconds of 369: `(1000 + 4950 +
2000 + 10000) × 0.97 × (169 × 0.006 + 1) × 0.94 ≈ 32963`, above the 27646 of SS. The same run in 400 seconds scores 16465: A.

## The rewards

At the clear, for every member in the maze then (`MazeResults.Complete`), once:

| Reward | How much | Table |
|---|---|---|
| Experience | the experience of the maze × (the share of the rank + the share of the place in a party), rounded up, plus what a booster of maze experience adds | server `tb_Maze_Info`, `tb_MazeReward_Rank`, `tb_MazeReward_PartyValue` |
| Gold | the gold of the maze × (the share of the rank + the share of the place), plus a booster of maze gold; past what a person can hold, by post | the same |
| Item of the rank | a bucket of `tb_MazeReward_Item` by its rate, of those the rank reaches (the rates are rolled against 10, 40, 160, 640, 2560 and 10240 for F to SS, which the rates of nearly every row add up to bucket by bucket), then one item of the box of the bucket by the rates of its items; when it does not fit, by post | server `tb_MazeReward_Item`, `tb_Item_RandomBox` |
| Key reward | as before (`docs/notes-party-rewards.md`) | the same row |
| Fatigue | the second half of the cost of the maze is spent | `tb_Maze_Info` |
| League | half the fatigue cost and a hundredth of the gold of the clear | `docs/notes-league.md` |

The experience goes through `ExperienceService.Add`, the gold through the wallet of `InventoryService`, the item through
`InventoryService.TryGive`. What goes by post is one letter of the game to the account (`IPostStore.SendToAccountAsync`, the kind
of a letter of maze rewards), and the client is told of it.

In a party the members are put in the order of their scores, highest first; the share of a place comes from the row of the
size of the party (`tb_MazeReward_PartyValue`: four places of experience, four of gold).

## The messages

| Message | When | What it carries now |
|---|---|---|
| 11/75 `MazeRewardInfoResponse` | at the clear, to each member | rank, score, points of the clear, the time in seconds (a `u16`: the client splits it into hours, minutes and seconds), the longest combo, the experience and gold of the clear and of boosters, the totals of the run, then three pairs of an item and its count (the item of the rank, the key reward, a hidden event) and a byte |
| 11/76 `MazeRewardPartyInfoResponse` | at the clear, to each member of a party of two or more; a player alone gets none | every member: place, rank, actor, name, score. The client sorts the list by the score itself |
| 03/64 `PersonFpUpdateResponse` | whenever the fatigue changes: spent, filled for a new day, restored by an item, set by a command | the points of the day, the saved points, a byte the client shows a line with when set (0) |
| 04/01 error | at the portal | see the admission |

**Fixed:** the window put the time where the client reads a number, and the key reward where the client reads the item of
the hidden event: the client never showed the key reward, and so never sent 08/47. The fields are now in the client's places;
`ItemWireFormatTest` checks them.

The other messages of a maze during the play (the timer 11/38, the notice 11/51, the unity points 11/59) are sent by the
scripts as before; the reference sends no kill score in this client (it has no reader for one).

## Fatigue

**State**: the row of the person, as before (`FatiguePointCommon`, `FatiguePointBonus`), and one column more,
`FatigueRefilledAt` (when the points were last filled for a new game day). A column because the points were already columns
of the row, read by the gate for the list of characters and by every server at entry, and written with the wallet; a
document would have split one value over two places. The flush writes the three with the row (`PersonStateChanges.Fatigue`).

| Rule | Value |
|---|---|
| Points of the day | at most 200 |
| Saved points | at most 400, spent before the points of the day |
| Cost of a maze | `FatiguePoint` of the client's `tb_Maze_Info` (4 to 18) |
| Entering | asks for the whole cost (less what a booster of fatigue takes off); half of it is spent when the player comes into the maze (none for a maze that costs one), half at the clear (each half less half of the booster) |
| Refused | 04/01 (or 04/41) error 55044 |
| A new game day (the day of the daily missions, 11:00 unless `DailyMission:ResetHour`) | the points of the day fill up to 200; half of what was left of them, at most 100, is saved, and 100 more for each whole day away, at most 400 in all |
| An item of fatigue (`ItemUseType.FP`) | adds its use value to the points of the day; refused when the person has saved points or a full day |
| A person never filled | starts its days from its first entry, with what it was made with |

When: the fatigue is filled when it is next looked at (the portal, a spend) and by a tick of the misc state every 30 s.

## Admission at the portal

`MazeAdmission`, in the district, for a player alone (`WorldService`, 04/01 and 04/41) and for the members of a party when its
master takes the portal (`PartyMazeEntry`, before anybody is asked):

| Check | Alone | A member of a party |
|---|---|---|
| Who may enter (`Admission`: 0 anybody, 1 alone, 2 a party, 3 a full party, 4 nobody) | 55033 for a maze of a party, 55052 for none | 55032 for a maze for one, 55052 for none |
| The level of the maze (the client's `tb_Maze_Info`, `RequiredLevel`) | 55012 | 55013 |
| The maze before it, cleared (the server's `tb_Maze_Info`) | 55003 | 55048 |
| The fatigue | 55044 | 55044 |

The clears are the records of the person (below), so the maze before is checked against what the person has cleared on this
server. The rest of what a maze asks at its portal (the weapon in hand, the hours, the quest, the daily limit, the item, the place
of the portal) and getting up after a fall are in `notes-maze-entry.md`.

## Records, achievements, ranking

- **The record of a maze** (`SharePointBook.Mazes`: clears, best rank, fastest time) is kept with the progress of the mazes of
  the person, in the document of the share points, written by the flush of the maze and read at the next entry. No change of
  the model.
- **Achievements**: a clear counts for the type of its rank, 37 (SS) to 42 (F), through one optional parameter of
  `AchievementManager.OnMazeCleared`. The daily missions get the rank of the clear (they had 0).
- **Ranking**: the clear of a casual maze (kind 2) or of a district raid (kind 8) is written into the time attack ranking of
  the maze (`IRankingStore`, the best time per person), which is what the ranking window of the client lists; it refuses any
  other kind of maze.

## Compared with the reference

| What | Reference | Here | |
|---|---|---|---|
| Score and rank | points, combo, kills, skills, damage share, hits; time, difficulty, level; ranks by the standard row | the same without the points of the skills | kept different: the client's skill table has no such column |
| Rewards of the rank | experience and gold of the maze at the share of the rank and the place | the same | same |
| Item of the rank | a bucket the rank reaches, an item of its box, by post when it does not fit | the same | same |
| Key reward | two key rewards, the keys spent by id | one, the keys taken from the slot the client names | kept different (`notes-party-rewards.md`) |
| Window | rank, score, clear points, time in tenths of a second, difficulty, totals, rewards | the client's layout: time in seconds, no difficulty | client wins |
| List of members | to the members of a party only | the same | fixed (it went to a player alone too) |
| League | half the fatigue and a hundredth of the gold of the clear | the same | fixed (it was the gold picked up) |
| Fatigue spent | half at load, half at the clear, saved points first, a net cafe pool first | the same without a net cafe pool | kept different: no net cafe here |
| Fatigue of a day | filled at the daily start, part of what was left saved | the same | same |
| Fatigue item | refused with saved points or a full day | the same, refused with the generic code of the item use | kept different: the item use answers one code |
| Message of fatigue | sent at the daily fill | sent at every change | kept different: the client keeps the last values, more of them only shows the true ones sooner |
| Admission | weapon, open hours, who may enter, level, maze before, quest, daily limits, fatigue, item | the same (`notes-maze-entry.md`) | same |
| Records and clears | a list of cleared mazes by group, sent as 11/64 | a record by maze (clears, best rank, fastest time) | kept: the window of the clear info is sent as before |
| Ranking | written for the mazes its ranking table names | written for casual mazes and district raids | kept different: the client's ranking list reads only those kinds |
| Party drops | the killer's | the killer's | same |

## Left for the party maze

- **Drops in a party** stay the killer's, as the reference has them; nothing to change.
- **The party values of the reward tables** are used: the points of the size of the party and the shares of the places.
- The strength of the monsters by the size of the party is still not applied (`docs/notes-party-maze.md`).

## Assumptions

- The rows of the server's score tables are keyed by their first column (for the combo, the count + 1); every table read
  this way gives the rows the reference's code asks for.
- A combo starts at the targets of the first hit less one, as the reference counts it.
- The share of the damage counts the damage the members did, not what the monsters lost.
- The time attack ranking keeps seconds and the score; the unit the client shows is not checked.
- The gold of a clear past the limit of the wallet, and the item of the rank that does not fit, go in one letter.
- The byte of 03/64 is a flag the client shows a line with; it is always 0.
- 55044 is sent without the actor the reference adds after the code.

## To check with the real client

- The window: the rank letter, the score, the time, the item of the rank and the key reward, the totals; the gold line in the chat.
- The list of the members in a party, sorted by score.
- The fatigue gauge after entering, after the clear, after a new day, and after an item of fatigue.
- The texts of 55003, 55012, 55013, 55032, 55033, 55044, 55048 and 55052 at the portal.
- The time attack ranking of a casual maze after a clear.

## Tests and scripts

`MazeResultsTest`: a clear scored by the counters and ranked by the table on the worked example; the reward of a rank given
once with the item of its bucket; each member of a party with its own window and the list of all of them with their ranks;
entering spends half the fatigue, the clear the other half, and the portal refuses without it; the fatigue of a new game
day; a clear by rank counted for its achievement. `PersistenceTest` keeps the fatigue and a record of a maze through a logout.
`ItemWireFormatTest` holds the window to the client's places. `ServerTablesTest` reads the nine server tables to their trailer.

`docs/tools/e2e_results.py` (local): the refusals at the portal, the fatigue in and out, a clear with a real rank checked
against the tables, the rewards arriving, the database after leaving, and a new game day.
