# The rest of the maze admission, the limits of entering, and getting up after a fall

Branch `feature/maze-entry`. What a maze asks of a player at its portal is now what the reference server asks, in its order and with
its codes; a maze that limits entering counts the entries of a day and tells the client; and a fallen player gets up only the
ways the reference allows. The client 1.4.2.3 decided every layout below (its readers of 04/43, 04/44, 04/45, the dialog of the
revive and the sender of 03/42 were read again for this work); the reference server gave the rules, the tables read and the
codes. The comparison, check by check, is at the end.

Code: `OpenWorker.Gameplay/Modules/Maze/MazeAdmission.cs` (the checks), `MazeEntryLimits.cs` with `MazeEntryBook.cs` (the counts of
a day), `BatchMazePortals.cs` (the portals of a map), `MazeRevival.cs` (when a fallen player may get up), `OpenWorker.MazeServer/Services/VitalsService.cs`
and `MazeReviveItems.cs`, the server rows in `OpenWorker.Res/ServerRows/` (`MazeRuleRow`, `MazeEntryRows.cs`), the messages in
`OpenWorker.Protocol/Modules/World/Responses/`.

## Where the data is

The client's `tb_Maze_Info` has none of what this work needs: no daily limit, no quest, no item, no revive rule. They are columns of
the server's own `tb_Maze_Info` (`MazeRuleRow`, read from `res-server/`), so nothing is named in `Structures.json` for the maze table.
What the client's table gave before still gives the level, who may enter and the fatigue. Two more server tables are read, both
optional: `tb_Maze_OpenControl` (the windows in which a maze is open) and `tb_DailyMaze_Portal` (the days of the week a casual raid is open).
The one column named in `Structures.json` is the sub type of an item (`tb_Item`), which tells a revive item.

The server table says: 85 mazes limit entering (the 75 stages of the infinite tower, 5 a day each, and ten mazes of higher levels), two
of them share a count (a group), 28 casual raids take turns by the day, one maze asks for a quest and an item, 81 mazes let nobody
get up (the stages of the tower and the mazes of the kind 15) and one lets players get up as often as they like (the mode maze).
Of these the client's own table has the stages of the tower and the first casual raid of each portal; its characters stop at level
47, below the 55 and more the rest ask for. So on this client what can be seen is the tower's limit, and the rules of the rest are
held by the tests.

## What a maze asks at its portal

In the order of the reference. `Check` is a player alone, `CheckParty` the master of a party (and every member), both from `MazeAdmission`.

| # | Check | Alone | A member of a party |
|---|---|---|---|
| 1 | The soul weapon in hand (a map of an event too) | 55016 | 55017 |
| 2 | A row of the table for the maze | 55008 | 55001 (the kinds a party cannot enter too) |
| 3 | The hours: a maze with windows today is open only in them (`tb_Maze_OpenControl`) | 55060 | 55060 (for the master) |
| 4 | Who may come (anybody, alone, a party, nobody) | 55033, 55052 | 55032, 55052 |
| 5 | The level of the maze | 55012 | 55013 |
| 6 | The maze before it, cleared | 55003 | 55048 |
| 7 | The portal: it is in the map the player stands in, and the player stands within 2000 of it (a member: it is in the map) | 55003 | 55055 |
| 8 | The quest of the maze, taken or finished | 55037 | not asked |
| 9 | The daily limit, and for a casual raid that it is open today | 55053, 53138 | 55053, 53138 |
| 10 | The item the maze asks for | 55035 | 55056 |
| 11 | The fatigue | 55044 | 55044 |

The checks 1 to 6 are asked of whoever takes the portal, in a party as alone, before the party is looked at; a member who takes the portal
of the maze the party plays is let into it after them and nothing more. The answers of 55035 and 55056 carry the item the maze asks
for after the code (a number the text of the error is filled in with); every answer is on the request that was made (04/01 or 04/41).
A district and the map of an event are entered through a portal of the map the player stands in, too (the check 7, 55003); the maze of
the tutorial is entered from wherever the story puts the player.

## Entering is counted when the player comes in

A maze that limits entering counts the entry when the player comes into the maze (`MazeEntryLimits.Entered`), not at the portal; the
portal only asks that the count is below the limit. In the same moment the item the maze asks for is taken, and a player whose item is
gone from the bag is sent away. The count is kept for the person (`PersonStates`, kind 13, written by the flush):

- by maze, or by group for mazes that share a limit; a casual raid counts for the first maze of its day;
- cleared at the start of the game day (the day of the daily missions, 11:00 unless `DailyMission:ResetHour`, as the fatigue), also for a
  person who is online when it turns (the tick of the misc state, every 30 s);
- told to the client: 04/43 as the player enters the world (the district), 04/45 for every change (the maze, once for each maze that
  shares the count), 04/44 when the day turns. The client keeps one count per maze and shows what is left of its own limit.

The free revives of the day are in the same document and clear with the day.

## Getting up

`VitalsService` asks `MazeRevival` before anything else happens; a refusal is the error answer of 03/42 (03/C3).

| Way (the byte of 03/42) | Allowed | Refused with |
|---|---|---|
| any, in a maze that lets nobody get up | never | 52617 |
| any, by a player who is on its feet | never | 52608 |
| 0, 1 (the buff of another player), 2 (the safe place of a district) | never in a maze | 51009 |
| 3, free | three times a game day | 51014 |
| 4, for cash | never (no cash shop) | 51014 |
| 5, by the maze itself | in the maze of the mode that revives its players | 51010 |
| an item of the call (08/11, sub type 2, effect 1) | as many times as the maze allows (`RevivalItemCount`, 0 for as often as the player has) | 52606 (the answer of the item) |

A player gets up where it fell, with all its life and the gauges full, for five seconds nothing can hurt it, and the maze is told (03/44).
No item of the tables has the use type `REVIVE`: the calls are effect items (sub type 2) whose use state lets them be used when fallen.
The client's own dialog sends only 1 and 2 (and 08/11 for the item, and leaves the maze on the other button or when its 100 seconds
are over). So with this client a player in a maze gets up with an item; 3 is for the scripted clients and a later client.

## Compared with the reference

| What | Reference | Here | |
|---|---|---|---|
| Order and codes of the portal | as the table above | the same | same |
| The portal of the party master | forwarded before the position is asked | the same | same |
| The district asks for the quest of the district | a column that is 0 for every district | not asked | kept: nothing in the data asks |
| The player in the matching, a trade or a card game | refused with 55003 and 55093 | not asked | kept different: the states are not seen from here |
| Who may come: a full party | 55051 | treated as a party (55033) | kept different (`notes-maze-results.md`) |
| The count of entries is taken | when the player comes into the maze | the same | same |
| The item is taken | as the player comes in; a missing item kicks | the same, the player is sent away | same |
| Count of a casual raid | the first maze of the day holds it | the same | same |
| Count of a group, of the account | a group of mazes; the account or the character | a group; the character only | kept different: no group is counted by account in the data |
| Count in a net cafe | a second count | not kept | kept different: no net cafe here |
| Clear of the count | at the server's init date | at the game day of the daily missions | kept different (as the fatigue) |
| Windows of a maze | a window per day, ends at the first missing number, none means open | the same, on the server clock (UTC) | same |
| 04/43, 04/45 | a person, a result, a list of (maze, count, count in a net cafe) | a list of (actor, maze, count): the client reads this | client wins |
| 04/46 to 04/48 | the counts of a group | not sent: the client has no reader | client wins |
| A revive of the kind 0 from a client | passes every check | refused with 51009 | fixed |
| A revive by a buff | the buff of another player, found on the player | refused: nothing casts it | kept different: the revive skill of the classes is not built |
| A revive item | gives a revival buff; the player confirms with the next request | gets the player up at once | kept different: the dialog of the client closes with the revive |
| A revive item past the count of the maze | 52606 on the use, nothing spent | the same | same |
| A revive item in a maze that lets nobody up | spent, then refused | refused before it is spent | fixed |
| Number of items used, shown to the client | sent after each use | not sent: the client has no reader | client wins |
| Free revives of a day | three, the stack of an item row; the count is sent (03/08) | three; not sent: the client has no reader | client wins |
| Revive for cash | takes a cash item, JP and KR only | refused (51014) | kept different: no cash shop |
| State after getting up | full life and gauges, buffs of the revive, five seconds of invincibility | the same without the buffs of the revive | kept: the buffs wait for the buff work |
| Revive window buttons | | | to check with the real client |

## For the other work on items

- The call that gets a fallen player up reaches `MazeRevival` through the use of items (`ItemUseService`, the effect items whose sub type is 2 and
  effect is 1, `IReviveItems`, implemented by the maze server); the other calls (the party's, the buffs) are still refused with 52607.
- The items that renew a casual raid (the use types `CASUAL_INIT` and `CASUAL_DECREASE_ENTER_LIMIT`) are not handled: `MazeEntryLimits.Reopen`
  sets the count of a maze or of its group back to nothing for the first.

## Assumptions

- The portal of a casual raid in the table of the days is the column of the maze that names its portal.
- The hours of a window are the server clock, UTC here.
- The soul weapon is the item in the weapon place of the person.
- 04/43 is read while the client enters the world from the gate (the reader is in the scene of the loading, not in the scene of the
  game), so it is sent with the other lists of the district; the counts that change afterwards go by 04/45.
- The number after the code of 55035 and 55056 is read by the client the way the other errors with a number are.

## To check with the real client

- The board of the mazes: the count of the tower after an entry, after a new game day, after a relog.
- The texts of 55016, 55017, 55008, 55060, 55037, 55035 (with the item name), 55056, 55053, 53138, 55003 at the portals, and of
  52617, 52608, 51009, 51014, 52606 after a fall.
- The dialog of the revive: the buttons it shows with and without a call in the bag, the 100 seconds, and the dialog closing when the
  item gets the player up.
- That a portal of the district is taken from the place the player stands (the 2000 units), and what the client sends as the portal
  of the maze board.

## Tests and scripts

`MazeEntryLimitsTest`: an entry counted as the player comes in and refused at the limit; mazes of a group sharing a count and the
client told for each; a new game day clearing the counts and telling the client; a casual raid open by the day of the week; a maze open
only in its windows; the layouts of the three messages and of an error with a number. `MazeAdmissionTest`: the checks of a player alone
in the order of the reference, the checks of the members of a party, the item taken as the player comes in. `MazeRevivalTest`: the
ways of getting up and their codes, the free revives of a day, the revive items of a maze. `PersistenceTest` keeps the book through a
logout. `ServerTablesTest` reads the two new server tables to their trailer.

`docs/agents/feature-maze-entry/e2e_entry.py` (local): the portals, the limit of the tower through a relog and a new day, the revive
(not fallen, nobody may ask, free three times, a revive item). The scripted clients that enter a maze by a portal now name a portal the
player stands at (21511 of the first district, within reach of where a player comes in; the script of the quests walks back to it
first); copies are in `docs/agents/feature-maze-entry/e2e/`. `/die` brings a player in a maze down for real (it only told the client
before), which the revive checks need.
