# Maze game modes, cubes, boosters, rankings and achievements

What a maze plays besides its monsters and its clear, and what a person earns on the way. The reference server (the build of
the original game server on the development machine) gave the rules and the order of the checks; client 1.4.2.3 decides every
layout. Code: `OpenWorker.Lua/LuaMaze.Modes.cs`, `LuaMaze.Survival.cs`, `LuaMaze.Defence.cs`, `LuaMaze.Operation.cs`,
`OpenWorker.Lua/SpawnWaves.cs`, `OpenWorker.Gameplay/Modules/Maze/GameModes.cs`,
`OpenWorker.Gameplay/Modules/Vaccum/`, `OpenWorker.Gameplay/Modules/Boosters/`, `OpenWorker.Gameplay/Modules/Progress/`,
`OpenWorker.Gameplay/Modules/Persons/OpenTitleManager.cs`, the server rows in `OpenWorker.Res/ServerRows/ModeRows.cs` and
`InteractionRows.cs`.

## Game modes

A mode is a rule about the maze that its script starts (`StartSurvivalMode(n)`, `StartDefenceMode(n)`,
`StartOperationMode(n)`; the number is a row of a server-only table). The maze holds one mode at a time; a mode started while
another runs replaces it. The mode counts the time of the maze, and once a second (a little more) asks itself whether it is
over. A number the server has no row for starts nothing and is logged.

| Mode | Where (client 1.4.2.3 scripts) | Table (server only) |
|---|---|---|
| Survival | maze 21331 (`M04_RSQUARE_EP_03_Normal`): the script box 90202 calls `StartSurvivalMode(10)` | `tb_Mode_Survival`: the maze, the sector to clear, up to ten spawn boxes |
| Defence | mazes 23131 to 23133 (`M12_FRONTLINE_EP_03_Normal`): the script box 20005 calls `StartDefenceMode(n)` | `tb_Mode_Defence`: the monster to defend, the hate it draws (value, interval, range), the box the players start in, up to ten spawn boxes |
| Operation | the two shelter mazes (`M10_N102SHELTER_EP_04`): the boss calls `StartOperationMode(n)` | `tb_Mode_Operation`: the boss, the life shares that start a trial, the time, the reminders, the aura of a success and of a failure, the interaction boxes |

The client sends 17/126 from inside the box of a script (`VLuaFunctionBox`); the server runs the function of the box and
answers 17/127. The modes tell the clients with 17/81 (`int type, int value, float time`): 1 a defence started, 2 a defence
ended (value 1 won, anything else lost), 11 a survival started, 12 a survival ended (value 1 won), 61 a line of the operation
(the value is the text, a time above zero shows its countdown, below zero hides the line).

### Waves

The spawn boxes a mode names are boxes that wait for a signal. A box sends its first wave after its delay (after its sequence
time when it has no delay) and the next ones one sequence time apart; the number of waves is the number the box says (one when
it says none). A wave rolls the slots of the box by its sequence type: every slot by its own chance, the first slot that rolls
well starting after the one taken last, or the one slot a single roll falls in. A box a mode (or a script) has started is
started again only by a script. A mode counts the waves as done a second after the last monster has appeared.

### Survival

The sector of the table is the one to clear. When the last wave has appeared and the sector holds no monster, the sector is
completed, the script function `OnSurvivalModeSuccess(sector, maze)` runs, the clients are told (12, 1) and the quests of the
kind survive (condition type 7) for that maze count for every player on their feet. There is no way to lose: players that fall
are revived or leave, and the maze waits.

### Defence

The monster of the table that stands in the maze becomes the defended one: it gets its full health, the monsters of the maze
take it for a target (a player cannot hit it), and every call of its interval draws the hate of the monsters in range. When the
mode starts every player is put in a random place of the start box, facing as the box faces. The mode is won when all waves
have appeared and no monster of the sector the players stand in is alive while a player is up (notice 2, value 1,
`OnDefenceModeSuccess`); it is lost when the defended monster has fallen (value 2) or no player is up (value 3)
(`OnDefenceModeFail`). The sector of the defended monster is completed whatever the result. The client table of conditions has
no protect condition (type 6), so a win counts for conditions of that type only if a table has one.

### Operation

A trial the boss starts at the life shares of the table: one of the interaction boxes of the table is enabled, the players read
the text with a countdown, and clicking the box in time gives the clicker the aura of a success; otherwise the boss gets the
aura of a failure. Reminders come at the three times of the table. It has no quest and no script function.

### Compared with the reference

| What | Reference | Here | |
|---|---|---|---|
| Start | row by number, silently nothing when missing; notice at the start of a survival and a defence | the same, and a log line for a missing row | same |
| End check | about once a second | the same | same |
| Survival loss | none | none | same |
| Quest of the kind | for every player in the maze with life left; the conditions that count for the group also count the others, the fallen included | the same (`QuestManager.OnMazeModeWonAsync`) | same |
| Defence results | 1 won, 2 the defended fell, 3 nobody up; the sector of the defended monster completed whatever happens | the same | same |
| Defended monster | full health, the nation of a player, not moved by the hate of the monsters | the monsters take it for a target and the players cannot hit it; its health and nation are not reset | kept different (check on the client) |
| Operation trial that is never lost | a trial whose three reminders have all been shown before its time limit is not failed | kept as it is | kept |
| Operation with no boss in the maze | ends at once | the same | same |
| Traps of the survival maze (`CreateTrap`) | placed by the script | not built | left |

## Cubes of the vacuum

A cube is an interaction object of the maze whose row has the kind 2. Its server-only row (`tb_Interaction_Object`) says how
long a click must be held (milliseconds), how many picks a cube has (a number between a minimum and a maximum), how many cubes
of its kind may stand at once and how long a new one takes, and the row of rewards. The rewards (`tb_Interaction_Item`) are ten
slots of a rate, an item, a minimum and a maximum. A pick rolls 1 to 10000 and takes off the rates in turn; the first slot to
reach zero wins. The item 800000001 is gold, 800000003 battle points, 800000004 ether, anything else an item. A slot with no
item answers 52004; gold, battle points and ether past the limit of the purse answer 52056, 52057 and 52058; items that do not
fit answer 52010. A click answers 55800 for a cube that is not there or was released, 55801 for one somebody holds, 55802 for a
release of a cube the person does not hold. After every pick the taker is told it is released (55800); a cube is gone at no
picks left.

| What | Reference | Here | |
|---|---|---|---|
| Roll and codes | as above | the same | same |
| Picks and hold time | the rows of the server | the rows of the server (the client's table has neither) | same |
| Who sees a cube | the cube is shared; a taker blocks the others | the state is kept per person | kept different |
| New cubes after a respawn time | groups of cubes of one kind, a new one after the delay | not built | left |
| Where cubes appear | only after a command of a game master or in the world mode of District 6 | the maze registers them as the batch says | kept different |

## Boosters

A booster carries effects; the value an effect gives is `origin + origin × rate + value`. Where the original reads them, and
what is built now:

| Effect | Where | Here |
|---|---|---|
| IncExp | the experience of a kill (the bonus is told) | built (`ExperienceService`) |
| IncGoldDrop | the amount of gold a drop gives (the bonus is told) | built (`DropService`) |
| IncBattlePoint, IncEtherDrop, IncItemDrop | the chance of a drop of that kind | built (`DropService`) |
| IncUpgradeItem | the base chance of an upgrade | built (`ItemUpgradeService`) |
| Experience and gold of a maze, quest experience, fatigue | the clear, the quest, entering | built before (`MazeResults`, `QuestManager`, `FatigueService`) |
| AddFP | the pool of fatigue of an internet cafe | not built (no pool) |
| Hidden event, rate of the Grouton monsters, exchange effects, credit effects | the places of those features | left |

## Rankings

Client 1.4.2.3 has three ranking requests: the time attack (2C/01), the arena (2C/02) and the infinite tower (2C/03). The time
attack is written at the clear of a casual maze or a district raid (`notes-maze-results.md`). The arena has no request that
starts it, so no result is ever made; the tower needs the whole tower (coverage C2). The richer ranking tables of the reference
belong to a newer client. Nothing more can be built for this client.

## Achievements and titles

What counts is listed in `notes-person-progress.md` ("Which achievements count"). The services tell the manager what happened
(`AchievementManager.Count`), the manager counts it for the chain of its type and target. The minutes of play are counted on the
tick of the misc work (every 30 seconds, whole minutes).

| Type | Reference | Here | |
|---|---|---|---|
| 15 | every use of an item that starts its cooldown, fatigue items included | the same | same |
| 16 | once per purchase request | the same | same |
| 17 | the gold of a repair, in each of the four ways | the same | same |
| 18 | a death | a death in a maze (a district has none) | same |
| 26, 27 | one per monster hit; the longest combo | one per monster of an attack; the combo of the run (our combo rule: a window, five targets at most) | kept different (combo rule) |
| 33 | battle points added from wherever | the same (a listener on the purse) | same |
| 43 | a minute of play | whole minutes, counted every 30 seconds | same |
| 49, 50 | an upgrade tried, an upgrade that did not go up | the same | same |
| 51, 52 | making and taking apart, once per request | the same | same |
| 53 | a daily mission finished; a hidden event has its own chain | missions only (no hidden events) | left |
| 56 | a ride of an NPC | the same; the ride of the district transport counts only for one kind of transport, which is not built | same |
| 57 | credit points gained with a group, as the day allowed | the same | same |
| 46, 54, 55 | the tower, the visits of a room, a purchase of the exchange | not counted: the features are not built | left |
| 58 and the types past 57 | an upgrade that went up and others | not in the client's table, so not shown | none |

Titles that open: a title row asks for an achievement (condition 2, the number of the achievement) or a level (condition 3).
When a person reaches that, the first prefix and the first suffix of the row are opened: kept in a person document
(`PersonStateKind.OpenTitles`, no change of the model), told with 03/29 and listed in the open list of 03/28. A title the person
has earned is not listed as open. The titles an achievement gives as its reward are given when the reward is taken, as before.
Left: the other conditions of the table (a quest condition and the echelon), and opening the titles of the achievements a person
had reached before this existed.

## Verification

- Tests (`MazeModeTest`, `QuestManagerTest`, `VaccumTest`, `RewardsTest`, `ItemSetupTest`, `AchievementEventsTest`,
  `TitleTest`, `TransportTest`, `NpcServicesTest`, `ItemUseTest`): the waves, the three modes, the quests of a won mode, the
  roll and the codes of a cube, each booster effect where it is applied, each event that counts for an achievement, the opening
  of titles.
- The stand (`docs/agents/feature-maze-modes/e2e_modes.py`, a client of the protocol): the player makes a character, takes the
  portal into the maze, walks into the box of the script and sends 17/126. Survival (maze 21331): the start notice at 1.8 s,
  113 monsters in two waves, the sector cleared by the chat command, the end notice (12, 1) at 36 s, the step of condition
  1000901 in 21/7; the same with the player down when the mode is won: the end notice and no step. Defence (maze 23131): the
  start notice and the warp to the start box, 109 monsters in the waves, the end notice (2, 1) at 56 s; with the player
  falling at 6 s the notice (2, 3) at 10 s.

## To check on the real client

- The texts the notices 1, 2 and 11 to 12 show (1351, 1352, 1353) and the call the client makes to its own script on a defence
  (`OnDefenceMode`).
- How the defended monster looks while the players cannot hit it.
- The hold of a cube (the bar of the click) and its release after a pick.
- The bonus a booster adds to the experience and to the gold of a kill, as the window of the reward shows it.
- A title that opens: whether the title window shows it with the way to earn it.
- The play time and combo achievements climbing in the achievement window.
