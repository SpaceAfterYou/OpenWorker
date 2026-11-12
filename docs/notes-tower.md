# The infinite tower (Steel Grave) and its ranking

Branch `feature/tower` (round 12). The tower is fifteen chapters of five stages; each stage is a maze of its own (20101 to 20325,
kind 7 in the client's `tb_Maze_Info`), three batches (`S01_STEELGRAVE` to `S03_STEELGRAVE`) holding the stages in layers. A
person climbs alone: it enters a chapter from the window of the tower in a district, clears the stages one after the other and
leaves after the fifth; a fall ends the climb (the client shows its own window of the failure and leaves the maze). Client 1.4.2.3
decided every layout below; the reference server gave the checks, their order, the codes, the rewards and what is stored.

Code: `OpenWorker.Gameplay/Modules/Tower/` (`InfiniteTower`, `InfiniteTowerBook`, `InfiniteTowerRanking`), the handlers
`OpenWorker.DistrictServer/Services/InfiniteTowerService.cs` (28/02) and `OpenWorker.MazeServer/Services/InfiniteTowerService.cs`
(28/03), the messages in `OpenWorker.Protocol/Modules/InfiniteTower/`, the ranking in `RankingService` (2C/03) and
`TowerRankingStore`.

## The messages

| Message | Direction | Layout | When |
|---|---|---|---|
| 28/01 | to the client | `i16 clearChapter, i16 clearStage, i64 leftTime, i16 count` (14 bytes) | as the person enters a district; whenever the count or the climb changes |
| 28/02 | to the server | `i32 chapter` | the window of the tower asks to climb a chapter |
| 28/03 | to the server | nothing | the window of a cleared stage asks to go on |
| 28/04 | to the client | `i32 item, i16 count, i32 battlePoints, i32 extraItem, i16 extraCount, i32 seconds` (20 bytes) | a stage is cleared |
| 2C/03 | to the client | `15 × i32` friends who cleared each chapter, then `5 ×` (`i32 myTime, i32 chapter, i32 stage, i32 person, u8 hero, u8 level, name, i32 time`), then `i32 chapter` | the ranking window asks for a chapter of the tower |

The client keeps the cleared chapter of 28/01 and opens the chapters up to the next one; it shows the clear time of 28/04 in
seconds and the extra item of 28/04 (a hidden event); the reward of the stage itself it shows from its own table. Each stage of
2C/03 had one `i32` too few before this branch: the client reads the chapter before the stage.

## The rules

**Entering a chapter (28/02)**, in the order of the reference; a refusal is the error answer of 28/02 with the code, but the soul
weapon, which the reference answers on the request of a world entry (04/81):

| # | Check | Code |
|---|---|---|
| 1 | a row of the client's `tb_Infinite_Tower` for the chapter | 58103 |
| 2 | the chapter is at most the one after the last cleared | 58100 |
| 3 | a row of the server's `tb_Maze_Info` for the maze of the first stage | 58103 |
| 4 | the chapters entered today below the limit of that maze (5) | 58101 |
| 5 | not in a party of two or more | 58102 |
| 6 | the soul weapon in hand | 04/81 55016 |
| 7 | the map of the first stage has a start point | 58103 |

Then the person is sent into the maze of the first stage as a portal sends it (04/02). No level, no fatigue, no portal is asked.

**In a stage.** The first stage of a chapter counts one entry of the day and tells the client (28/01); every stage counts one for
achievement 46 (its chain opens at level 20). The entries of each maze are counted as for every maze (`MazeEntryLimits`, 5 a day
per stage). The stage is won when its one sector is cleared: it gives the item and the battle points of its row of the client's
`tb_Infinite_Tower` (by post when the item does not fit), tells the client (28/04), raises the climb (the cleared chapter after the
fifth stage, the cleared stage counted over the tower as stage + 5 × (chapter − 1)) and records the time of the stage for the
ranking. A stage gives no score, no rank and no window of a plain maze (11/75).

**Going on (28/03).** In no maze: 55003. A stage that is not cleared, or a maze that is no stage: 58104. After stages 1 to 4 the
person goes into the maze of the next stage (04/02); after the fifth it leaves the tower by the escape box of the batch (to 10021,
the district of the tower).

**The day.** The count of the day clears at the start of the game day (the day of the daily missions, as the entries into mazes),
for a person who is online too (the tick of the misc state). A ticket of the tower (an item of the use type of the tower) clears it
once the entries of the day are used up, and is used up itself; with entries left it is refused with 52613.

**What is stored.** A document of the person (`PersonStates`, kind 16 `InfiniteTower`): the cleared chapter, the cleared stage, the
day and the count. The ranking keeps the fastest clear of each stage per person (`RankingEntries`, kind 3, the scope is the maze of
the stage). **No change of the EF model**: both kinds are new values of enums stored as numbers.

**The ranking (2C/03)** is the person's and its friends': for each chapter the friends with a clear of its last stage, for each
stage of the chapter asked for the person's own time and the fastest of the person and its friends (of two equal times the one
recorded first).

## Layers of a batch

A batch keeps the boxes of several mazes in layers: the normal and the very hard episode 4 of each story (layers `Normal` and
`VeryHard`), the casual raids (`Expert`), the twenty-five stages of a tower batch (`Stage01` to `Stage25`). The layer of a maze is
its difficulty in the client's `tb_Maze_Info` (0 normal, 1 very hard, 2 expert, 3 to 27 the stages). A maze now loads the common
layer and its own; before this branch every layer was loaded, so an episode 4 spawned the monsters of both difficulties and a
stage of the tower those of all twenty-five stages. A map with a single start point (the tower batches) is entered by it whatever
its id.

## Compared with the reference

| What | Reference | Here | |
|---|---|---|---|
| Checks of 28/02 and their codes | as the table above | the same | same |
| A second 28/02 while one runs, 28/02 from inside a tower maze | ignored | the requests of a connection run one after the other; 28/02 has no handler in a maze | same |
| The count of the day | one count for all chapters, against the limit of the first stage's maze, a second count in a net cafe | one count, no net cafe | kept different: no net cafe here |
| The time left in 28/01 | what is left of a time set at the start of the day, always past: 0 | 0 | same |
| 28/01 sent | at every world entry, on a request the client 1.4.2.3 never sends, after a change | at every district entry and after a change | same |
| Reward of a stage | the server's own tower table (items the client's tables do not have) | the client's table | kept different: the client must know the item, and its window shows its own table |
| Item that does not fit | by post | by post | same |
| Extra item of a hidden event | given and shown | always none | left (hidden events are not built) |
| Clear time in 28/04 | the play time of the maze in tenths of a second | whole seconds | client wins: it shows the value as seconds |
| Cleared chapter and stage | raised as above, saved, 28/01 | the same | same |
| Weekly mission, daily mission of a clear | counted | the daily mission as for every maze; no weekly missions here | kept different: no weekly missions |
| Achievement 46 | each stage entered | the same | same |
| Buffs of one kind cleared at the start of a stage | yes | no | left (the buff work) |
| Monsters at the clear | every monster of the maze dies, the players become invincible | the monsters of the sector go | kept different: the sector is the whole stage |
| 28/03 | 55003 out of a maze, 58104 before the clear, the next stage or out | the same | same |
| Out of the tower | back where the person came in | the escape box of the batch (10021) | kept different: the tower is entered from its district |
| Ticket of the tower | usable when the count is 3 or more | usable when the entries of the day are used up | kept different: the count's limit is 5 in the data, and 52613 says entries remain |
| Ranking of the tower | not in this version of the reference (its rankings are of a newer client) | the person and its friends, from the recorded clears | built from the client's reader |
| Layers of a batch | the layer of the maze | the same | fixed |

## To check with the real client

- The window of the tower at the portal of jump type 6 in Candus: the chapters open up to the one after the last cleared, the
  count of the day.
- The texts of 58100 to 58104 and of 55016 when a chapter is refused; 52613 for a ticket with entries left.
- Entering a chapter: the loading into the first stage, the window of the stage in the tower, the monsters of that stage only.
- The window of a cleared stage: the item and battle points of the table, the time, no extra item; the button that goes on
  (28/03) into the next stage, and after the fifth out of the tower into Candus.
- A fall in a stage: the client's window of the failure and its button out of the maze.
- The ranking window of the tower: the friends who cleared each chapter, the person's time and the fastest friend of each stage.
- An episode 4 (normal and very hard): only the monsters of its difficulty.

## Tests and scripts

`InfiniteTowerTest`: the checks of a chapter in order; the first stage counting an entry and telling the client; a cleared stage's
reward, its message, the climb and the ranking; the ranking of the person and its friends; a new day and a ticket.
`RankingWireFormatTest` holds 2C/03 to the client's reader. The scripted client of the stand (in the agent's folder) climbs chapter
1 through its five stages and checks every answer byte by byte.
