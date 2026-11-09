# Coverage: what the client 1.4.2.3 can do, and what the server really does

An audit of `dev` at `3c67dbc2` (2026-11-09), made without changing the server; the counts and tables were brought up to date
after `fix/session-gaps` (the list "Closed since the audit" below). It answers two questions: for every
message of the client, does the server do the real thing; and for every feature a player can reach, is it there. The
result is a backlog in the order a real player would hit the gaps.

Three branches were building while this was written and are counted as **in progress**, not as gaps:
`feature/equipment-stats` (the stats of a player from everything worn and owned, the wear of endurance),
`feature/maze-results` (score, rank, rewards and fatigue of a maze) and `feature/npc-services` (shops, the NPC behind
every service, NPC credit, the bank).

## How it was made, and how sure it is

- **The client's messages** are the export of round 1 (753 senders and receivers by name), checked against the
  client's receive switch (every case and the function it calls) and the callers of every sender. Three receivers sit
  at another case than the export says (the revive answer at 03/44, the warp of a skill at 06/46, the deck bonus answer
  at 06/79); the tables use the case the client reads. Every sender of the export has at least one caller in the
  client, so no request in the tables is dead code of the client.
- **The server's side** is joined by a script: the request and response types with their opcodes, the concrete
  handlers of each request and the servers that load them, and every place that builds each response.
- **The verdict of every request** comes from reading its handler and what it calls, not its name. A handler that
  delegates to a service whose notes describe it in detail (party, league, quests, helpers, post, trade, item work)
  was read to the service and spot-checked; the details of those rules rest on their notes.
- **The verdict of a notice** (server to client) is "real" when the server builds it from state; this was read for the
  ones marked otherwise and for the groups with a gap, and inferred from the building site for the rest of the groups
  whose requests are real. 149 notices are never built anywhere; whether the client needs each of them is said only
  where it matters (a notice nobody misses, such as the out-of-sight messages of a map with one grid, is not a gap).
- Where a verdict rests on what the client does (it waits for an answer, it shows something), the client's code was
  read; the evidence is in the git-ignored `docs/notes-coverage-research.md`.

To rerun the join after the code changes (scripts in `docs/agents/research-coverage/` of the main checkout):

```bash
cd docs/agents/research-coverage
python3 -I join_ops.py <checkout> ../feature-item-setup/ida/client_ops.json .   # ops_joined.tsv, server_only.tsv
python3 -I senders.py <checkout> ops_joined.tsv senders.tsv                    # where each notice is built
python3 -I bodies.py <checkout> bodies.tsv                                     # every handler body, empty ones flagged
python3 -I verdicts.py ops_joined.tsv senders.tsv tables.md <checkout>        # the tables below, with the counts
```

`verdicts.py` holds the verdicts read from the code as a table of overrides; a message without one gets the default of
its kind (a request with a handler: real, without: ignored; a notice that is built: real, never built: not sent). After
a feature lands, change its overrides and rerun. The client side (`ida_callers.py`, `ida_dispatch_all.py`,
`ida_decompile*.py`) runs through `docs/tools/run_ida.sh client <script> <out>`.

## The counts

**Requests** (client to server, 273 distinct; the client has two senders for one of them):

| Verdict | Count | What it means |
|---|---|---|
| real | 203 | the handler does what the request asks, with the checks the notes list |
| partial | 18 | it does part of it (what is missing is in the table) |
| invented | 2 | it answers with fixed or made-up data, or succeeds without the effect: the cash shop list, the tower ranking |
| ignored | 50 | no type (29: my room, exchange, social items, infinite tower, defensive weapons) or a handler that does nothing (21) |
| never sent by this client | 19 | request types the server has (16 of the item work, 3 of movement) that client 1.4.2.3 never sends |

**Notices** (server to client, 477 cases of the client's receive switch):

| Verdict | Count |
|---|---|
| real | 321 |
| partial | 7 |
| invented | 2 |
| not sent | 147 |

By group (requests on the left, notices on the right) the table opens the section "Messages per group" at the end.
Groups with nothing at all on the server: my room (0x26), infinite tower (0x28), exchange (0x2B), social items (0x2D),
and the defensive weapons and controlled monsters of group 0x17. Groups where requests are real but the server never
tells the others: skills (0x06: 25 notices never sent, among them what the others see of a held, charged, aimed or
synchronised skill) and items (0x08: the use of an item).

## The backlog, in the order a player meets it

Size: **S** a day or less, **M** a few days, **L** a week or more. "Original" says whether the rules have to be read
from the original server (they usually do) or the client alone decides.

### Closed since the audit

Branch `fix/session-gaps` (the comparison with the original and what is still open: `docs/notes-session-gaps.md`):

- **A1** a change of channel is answered with the map of the new channel, and refused for the same, a missing or a full channel
  and during a trade (`0b09dfff`).
- **A7** options changed in the game are saved to the account by the district and the maze (`c9a95e85`).
- **A9** the players who see a person get 08/0D with the worn slots that changed, in a district and in a maze (`bf80033a`).
- **A11** (a) a test command of the chat runs for the access level of the account, `Commands:OpenToEveryone` opens them on a
  test stand (`42f9aa43`); (b) a megaphone is taken from the bag (`73af53a2`); (c) the trade password is kept hashed and
  checked (`75bd98bf`).
- **A12** the maze answers the ping and keeps its players alive with the keep-alive of the district (`c9a95e85`).
- **B2** a person who is away is shown at the level of its row (`571cc31b`, `01db57b3`).
- The skill window, gestures and daily missions are answered in the maze through the services of the district (`38fac2ae`);
  boxes a maze client names (11/31, 11/35, 11/7E, 11/77) must be where the player stands (`992bca7d`).

### A. What blocks or breaks a normal session, from login to the first hours

**A2. Using items: potions and almost every other kind (08/11). L, original.** The use handler knows 5 kinds of use of
about 60 (boosters, akashic cards, packages, costume packages, random boxes); everything else is refused with an error.
In the client's tables that leaves out the effect items (potions and the like, 7 classifications), fatigue potions,
experience items, skill resets (single and all), name changes, inventory, bank, deck and equipment slot expansions,
character slot items, fever, the infinite tower ticket, class boxes and more. The success answer 08/11 is never sent
(it is what shows the reward window of an opened box), nor 08/13 (the motion of the use and the cooldown of the item in
the bag) nor 08/48 (the cooldowns at entry), so even the kinds that work have no cooldown. A potion clicked in a maze
fails. Needs: the effect columns of `tb_Item` and the buffs of effect items (the buff engine is there), the cooldown
groups, the original's use handler for each kind and its errors. Each kind is small once the frame (answer, broadcast,
cooldown, error codes) is built; the frame is the M part.

**A3. Shops: selling, buying back, stock, NPC (09/01-04, 09/10-12). In progress (`feature/npc-services`).** Selling and
buying back do nothing today, buying takes gold only and refuses silently. Listed here because a new player sells loot
in the first hour.

**A4. The end of a maze: score, rank, rewards, fatigue (11/75, 11/76, 03/64). In progress (`feature/maze-results`).**
The rank also feeds achievements 37 to 42, daily mission condition 9 and the time attack ranking (C-group gaps below).

**A5. The stats of gear and endurance (03/51, 18/12). In progress (`feature/equipment-stats`).**

**A6. Entering a maze checks nothing (04/01, 04/41). M, original.** Any location the client names is entered: a
district or a maze of any level, without the story step, the level, an entry item, an entry count or the opening hours
of a special maze (`tb_SpecialMaze_Open`), and from anywhere. The counts of limited entries (04/44, 04/45) are never sent,
so the maze board shows none. A normal client offers only what its board allows, so this is first a rule hole (a hostile
client walks anywhere, including into a district it has not reached) and second a missing feature (limits per day).
Needs the columns of `tb_Maze_Info` that the server does not name yet and the original's entry checks and error codes.
Depends on A4 for fatigue.

**A8. Revive (03/42). S to M, original.** A fallen player gets up free whatever the kind: the cash revive takes nothing,
there is no revive item (use kind `REVIVE`), no count of free revives per maze, no safe-zone or buff revive. The window
offers what the client computes, so the effect today is "every revive is free". Needs the original's revive rules and
the counts of `tb_Maze_Info`.

**A10. Skill points are reset and the deck grows without taking anything (06/72, 06/76). S, original.** The original
resets skills through the use of an item of that kind, and the client has an item kind for a deck slot too; whether the
original accepts the bare requests without an item was not read. Here both are answered for free. Learning, the deck and
divergence are real in a district and in a maze.

### B. Visible, but the game goes on

**B1. A party does not see each other's skills fully. M, client alone for layouts.** Never sent: the held key of a
skill (06/42), a change of target (06/44), the place of a moving skill (06/48), a sub input (06/4B), charging (06/4D,
06/50), the motion of using an object (11/79) and of using an item (08/13). The requests behind 06/44, 06/4B and 06/4D
have no handler in the maze at all. Alone in a maze this changes nothing; in a party the others see a skill start and
its hits, not what happens in between.

**B3. Rankings are always empty (2C/01-03). S once A4 exists.** The store can record a time and a kill score, but nothing
calls it; the tower ranking is zeros. Depends on A4 for times and scores, on C2 for the tower.

**B4. The cash shop is made up (09/20). L, original for the rules.** It lists one hard-coded item at a made-up price;
buying, the cart, gifts and the gacha are ignored. `tb_CashShop` and `tb_CashBilling_Info` are in the client; a source
of cash (the wallet has a cash balance) is a decision of the project, not a rule.

**B5. Cubes of the vacuum give nothing (25/01-03). M, original.** The cubes of a maze are registered and taken, but the
reward source is the empty `NoVaccumRewards`. The server-only table of interaction items is on the reference disk; it
needs a hand-written row and the original's reward rule.

**B6. Boosters are kept but do almost nothing (29/xx). M, original for the scale.** Only the experience of a quest is
raised. Experience of monsters and mazes, drop rates, gold, ether, battle points, upgrade chances and the status
boosters (a buff) are not applied; account boosters are kept per character.

**B7. Gestures need no unlock (23/01). S.** Showing a gesture works in a district and a maze for any gesture of the table.

**B8. Transports of the district (05/16) and the costume look (08/50, 08/52). M each, original.** The ride a dialog
offers is ignored (`tb_District_Transport`, `tb_Transport_Info`); appearance items are not kept, so the look of a costume
over gear is never loaded or changed.

**B9. Titles and echelon. S to M.** Titles granted by achievements are not given (the original does); the title clear,
delete and open notices (03/27-29) are never sent; the echelon of a person (03/52, 03/53) is never sent and not stored.

**B10. Small leftovers.** The dead monsters of a running instance (11/68) are always sent empty, so a member who joins
late may see monsters the others killed (S, check first). The answer of the use-effect tool (18/26) goes to a case the
client has no receiver for (S, check what the client waits for). The district answers a script box (11/7E) without
running anything (S). Character names are not filtered and deletion has no waiting time (S).

### C. What only later content needs (whole systems)

**C1. Akashic cards in combat (06/20, 06/21, 06/22, 06/7B). L, original.** The book of cards is real; summoning a card
in a fight is ignored on both servers.

**C2. The infinite tower (0x28; Steel Grave, 75 floors in three mazes). L, original.** No type for its two requests or
two notices; the tower ranking (B3) depends on it.

**C3. The other kinds of maze. L, original.** The client's maze table has, besides 115 story mazes and the 2 tutorials:
4 casual raids (with their timer window), a raid, District 6 as a maze and my room. Missing for them: hidden events
(11/37, `tb_Hidden_Event`), escort status (11/67), unity points (11/59, `tb_Unity_Event`), defensive weapons and
controlled monsters (17/41-45, 17/51-52), monster parts (17/54, needs hit bones), monster invisibility and motion
notices (17/31-34), entry limits (A6).

**C4. My room (0x26; 11 requests, 13 notices). L, original.** Nothing exists: the room map, furniture
(`tb_Myroom_*`), visits, the daily missions and achievements that count them.

**C5. The exchange (0x2B; 9 requests, 9 notices). L, original.** Nothing exists. The client asks for the interest list
and its own list at every entry and gets no answer (harmless as far as the code shows).

**C6. Social items (0x2D; 3 requests, 7 notices). M, original.** Nothing exists (`tb_Social_Item`, the exclusion boxes of
the maps are read by the batch reader).

**C7. Mail from the game. M.** Letters of the player are real; letters the game itself should send (rewards that do not
fit, the original's daily mission rewards, gifts, the exchange) and the sweep of expired letters are not there.

**C8. Achievements: 26 of 43 kinds are not counted. M.** The list with what each needs is in
`docs/notes-person-progress.md`; most wait for A2, A4 and the systems above.

**C9. NPC credit (03/65-67). In progress (`feature/npc-services`).**
## The features of the client

From the client's windows (the movies under `_datas/GUI/`), its tables (`_datas/bin/Table/`, the stand's
`client_res`) and its messages. "Done" means real for what a player does with it; "partial" names the gap; "missing"
means nothing on the server.

| Feature | Windows and tables | Messages | State | Missing |
|---|---|---|---|---|
| Login, server list, gate | login and server scenes | 02/xx, 01/02 | done | the NHN login is behind a setting |
| Character creation and customization | class select, customizing scene; `tb_Customize_*`, `tb_Create_Cloth` | 03/01, 03/11 | done | name filter (`tb_NameFilter`) |
| Character list, delete, second password | character select scene | 03/02, 03/11, 03/13, 03/17 | done | waiting time of a deletion |
| Prologue and tutorial | tutorial pop-ups, `T01`, `T02` mazes | 03/15, 03/19, 03/20, 11/23 | done | checks on the real client |
| District, channels, objects around | channel list, minimap, total map | F1/01, F1/02, 04/06, 04/21-23 | done | checks on the real client |
| Movement | (all) | 05/xx | done | transports (B8), ground state |
| Transport between areas | regional movement; `tb_District_Transport`, `tb_Transport_Info` | 05/16 | missing | B8 |
| Portals between districts and mazes | maze board, special maze entrance; `tb_Maze_Info`, `tb_SpecialMaze_Open` | 04/01, 04/41, 04/44-45 | partial | entry rules and limits (A6) |
| Chat, whisper, trade chat | chatting, chat log | 07/01-03 | done | |
| Megaphone | chatting | 07/06 | done | item links in the text |
| Friends, blocks, recruitment of friends | community, add/block friend | 19/xx | done | |
| League | league window, management, ranking, support | 22/xx | done | checks on the real client (notes-league) |
| Parties, recruitment board, awaiters, auto matching | party, party search, party find, auto matching | 12/xx | done | matching ignores blocks |
| Story mazes (115) | maze HUD, result window | 11/xx, 04/xx | partial | results, rank, rewards, fatigue in progress; entry rules (A6) |
| A party in one maze | party window, waiting message | 11/48-49, 12/47 | done | portals do not wait for the party (04/0D) |
| Casual raids, raid, District 6 as a maze | casual raid timer | 11/xx | partial | their own rules (C3) |
| Infinite tower (Steel Grave, 75 floors) | infinite maze window; `tb_Infinite_Tower*` | 28/xx | missing | C2 |
| Hidden events, escort, unity points | hidden quest window; `tb_Hidden_Event`, `tb_Unity_Event` | 11/37, 11/59, 11/67 | missing | C3 |
| Defensive weapons, controlled monsters | mech HUD; `tb_Defensive_Weapon` | 17/41-45, 17/51-52 | missing | C3 |
| Battle zone or arena | (no request of this client) | 03/16, 2C/02 | missing | the client has no arena request; only the ranking window shows PvP |
| Combat: skills, hits, death | skill bar, target frames, combo | 06/08-13, 03/41 | done | in the maze; what the party sees of held and charged skills (B1) |
| Skills and their deck | skill window, skill navi; `tb_Skill`, `tb_Divergence`, `tb_Deck_Bonus` | 06/71-79 | partial | reset and deck slots are free (A10) |
| Akashic cards in combat | skill window; `tb_Akashic_*` | 06/20-22, 06/7B | missing | C1 |
| Buffs | special buff, status | 06/14-17 | done | passive skills, items, auras, resistances (notes-buffs) |
| Inventory, bank, quick slots | inventory, bank | 08/01-05, 08/0F-10, 08/25-28 | done | the bank NPC is in progress |
| Using items | inventory, package box, reward window | 08/11, 08/13, 08/48-49 | partial | 5 kinds of about 60, no cooldowns (A2) |
| Equipment and its stats | equip, paper doll, status | 08/02, 08/0D, 03/51 | partial | stats in progress |
| Costume look | equip | 08/50, 08/52 | missing | B8 |
| Item work at NPCs: upgrade, sockets, broaches, repair, restore, unseal, make, exchange, disassemble, evolution, akashic | upgrade, socket, brooch, fixing, sealing, production, division, akashic record make | 18/xx | done | NPC check, credit and endurance in progress |
| Shops | shop | 09/01-04, 09/10-12 | partial | in progress |
| Cash shop and gacha | cash shop, gacha pop-up and result | 09/20-27 | invented | B4 |
| Trade and private shops | personal trade, personal shop | 0A/xx | done | the shop permit and zone; the visitor's purchase is a guess (notes-trade) |
| Trade password | (a pop-up before trading) | 03/18 | done | given again on each server; trades do not ask the server for the state |
| Mail | mail, mail read, mail write | 20/xx | done | letters of the game, expiry (C7) |
| Quests | quest, NPC talk, quest alarm | 15/xx | done | checks on the real client (notes-quests) |
| Daily missions | (quest window part) | 24/xx | done | friends and my room conditions |
| Achievements | game progress window; `tb_Achievement` | 03/70-74 | partial | 26 of 43 kinds (C8) |
| Share points of districts and maze groups | district condition | 03/58-63 | done | |
| Titles | title window; `tb_Title_*` | 03/23-29 | partial | titles of achievements, the clear and open notices (B9) |
| Echelon | character info; `tb_Echelon` | 03/52-53 | missing | B9 |
| Soul metry | soul metry window | 21/xx | done | |
| Attendance and access rewards | attendance check | 2A/01-05 | done | behind a server setting, off by default |
| Boosters | (icons) | 29/xx | partial | effects (B6) |
| Helpers | helper, register helper | 27/xx | done | |
| Vacuum (cubes in mazes) | (maze objects) | 25/xx | partial | rewards (B5) |
| My room | my room inventory; `tb_Myroom_*` | 26/xx | missing | C4 |
| Exchange | exchange | 2B/xx | missing | C5 |
| Rankings | ranking | 2C/xx | partial | nothing records (B3) |
| Gestures and social motions | gesture slots; `tb_Gesture`, `tb_Social_Motion` | 23/xx, 03/45 | partial | no unlock (B7) |
| Social items | social item; `tb_Social_Item` | 2D/xx | missing | C6 |
| Revive | revival window | 03/42, 03/44 | partial | A8 |
| Cutscenes | cinema talk | 03/19, 03/20 | done | in mazes; a district cutscene has no handler |
| Options and key settings | option setting, outgame option setting, hotkey | 01/02, 02/31-32 | done | |
| NPC credit | credit | 03/65-67 | missing | in progress |
| Fatigue | (status) | 03/64 | missing | in progress |
| GM tools and notices | (chat) | 07/04, 07/0A | done | the level of an account is set in the database |
| Help, loading tips, web notice | help, help pop-up, web notify | (none) | client only | |
## Gaps no single feature owns

### State that is never saved

- **The echelon** of a person: no storage, no notice (B9).
- **Boosters of an account**: kept per character; the original shares them across the account (B6).
- **Appearance items** (the look of a costume): not kept (B8).
- **The place in a maze**: by design not saved (a maze is left through its exit); the race between saving the place of
  a district and the character list of the gate is described in `docs/NOTES.md`.

Everything else a player changes (bags, gear, money, level, skills and deck, quests, helpers, akashic book, titles,
community memo, gestures, progress, soul metry, daily missions, attendance, boosters, mail, league) goes through the
flush or a store; this audit did not find another field kept only in memory, though not every component was traced.

### Hostile input not checked

- **Where a player goes** (04/01, 04/41): any location, reachable or not (A6).
- **Quick slots** (08/27, 08/28): stored as sent, not checked against the book or the bag; the client shows what it
  sent. S.
- Groups whose handlers showed the checks where they were spot-checked (lengths, indices, enum values, ownership): the
  item work, party, league, friends, post, trade, helpers, quests, inventory moves, skills of the maze.

### What only the actor is told, that others should see

The others are never sent: the use of an item (08/13), the hold, target, place, sub input and
charging of a skill (06/42, 06/44, 06/48, 06/4B, 06/4D, 06/50), the motion of using an object (11/79), the start of a
transport (05/16), the echelon (03/53), a gesture in a maze (23/01). The ones that are broadcast as they should be:
moves, motions, chat, titles (03/26), the warp of a skill (06/46), buffs, revive (03/44), helpers, party health, a
change of gear (08/0D).

### District here, maze there

| What | District | Maze |
|---|---|---|
| Charging, sub input, target change, pre-target list, akashic summon (06/4C, 06/4A, 06/43, 06/36, 06/20) | empty handlers | no handler |
| Attendance rewards (2A/02, 2A/05) | real | no handler (the maze does not load attendance) |
| Script box (11/7E) | echoed without running | real |
| Cutscene update (03/19) | no handler | real |
| Monster minds | none (no monsters fight in a district) | real |

The keep-alive and ping, the options, the skill window, gestures and daily missions are answered by both servers through
`OpenWorker.Server.Common` (`docs/notes-session-gaps.md`). Whether the client sends the attendance requests in a maze is
on the play-session list.

### Project constants where the game has a rule

| Constant | Where | The rule it stands for |
|---|---|---|
| 100 gold of postage | post | the original's fee (the client shows its own) |
| 1500 units to talk to a character | quests | the original's distance |
| 20% of life and 3 s for a monster's super armor | monster minds | a column the client's table lacks |
| 90 s offline before leaving a party | party | the original never removes a member |
| 40 letters per list, no paging | post | |
| A free cash revive, a free skill reset and deck slot | revive, skills | items of their kind |
| The walking speed 300 of townsfolk | prologue | no column in the tables |
| One hard-coded cash item | cash shop | `tb_CashShop` |

### Answers the client has no reader for

- 18/26 (the use-effect tool's answer): the client's switch of the item work stops at 18/24.
- The server-only request types (16 of the item work and 3 of movement) are typed and some are handled, but this client
  never sends them: their code is dead for this client.
## One play session: what only the real client can settle

Gathered from every notes file and from this audit (marked **new**), grouped by the window where it shows, in the order
of a session. Two accounts help: most party, trade, league and helper items need a second player.

**Login and character select**
- The flag after the account in 03/13 is always 0 (notes-prologue): nothing odd on the select screen.
- A new character with untouched customization (zeros) is created and looks like the preview (NOTES).
- Where a new character first stands in the district: the hard-coded point or the table's (NOTES).

**Prologue and tutorial** (notes-prologue, notes-combat, notes-buffs)
- 03/15 with change type 0 and map type maze loads `T01_TUTORIAL` and plays the first cutscene group.
- The guide of `T02_TUTORIAL` walks visibly and turns; the tutorial buff keeps life and gauges full.
- Skills are in the deck and Spin Cutter works in `T02`; the soul gauge bar follows 03/34 and empties by the cost.
- The tutorial end button and Esc on a cutscene both send 11/23 and the client lands in the district.

**District and channels**
- Change the channel: the window closes, the people of the new channel appear, the old ones go; the same channel and a
  full one show 51008 (`docs/notes-session-gaps.md`).
- Open the options in town and in a maze, change a key, relog: it is kept.
- Sight: the bounds of the grid cells and whether 5000 units is enough; 04/08 as a correction in a district; running,
  dashing and knock-back are not rolled back; how often the client sends its keep-alive (REFACTORING 8).
- The 07/xx notice of a server shutdown from the GM tool shows (REFACTORING 8).
- Equip a weapon and a costume while a second player watches, in town and in a party maze: the second player sees the
  new look at once (08/0D); a third who comes later sees it on arrival.
- **new** The exchange lists asked at every entry get no answer: no hang or error message (C5).

**Quest window and NPC talk** (notes-quests)
- The quest window is full after entering (15/02 then 15/01 sent with the character info).
- A talk from a little farther than 1500 units is ignored without an answer; the client does not wait.

**Skill window**
- Open the skill window, a gesture and the daily missions in a maze: they work as in town (the client's senders check no
  kind of map; whether its windows open there is what this settles).
- A refused skill (06/09 with a code) shows its message and the character stays usable; the evade spends stamina as
  the bar shows (notes-combat).

**Inventory and item use**
- **new** Use a potion in a maze: the error message today (A2); open a box: no reward window (08/11 never sent).
- The key reward window shows `CashItemCount` and `CashItem`; 52011 and 52013 are shown sensibly; 08/47 is sent once per
  window and never from a district (notes-party-rewards).

**Item work at NPCs** (notes-item-setup-handlers)
- The chat lines of 04 and 18 name what was taken apart, what came out, its count and the ether.
- The upgrade dialog shows a successful and a failed evolution from 18/13 (item 113000030 at upgrade 9).
- The count the exchange dialog sends is the count of products.
- The akashic book loads at entry and 08/58 adds a card after the use dialog; the make dialog closes after 18/14 with
  and without extra cards; the compose gauge shows the new experience; the disassemble window shows on result 1.
- Setting a broach shows it and the success message; the compose window shows the result for the serial; an error
  answer of 20 and 21 closes the wait of the broach window.
- **new** The use-effect tool: does the dialog close although its answer 18/26 has no reader?

**Maze**
- The answer to a failed maze (REFACTORING 8).
- 06/14 shows a buff's icon and time on the player and on a monster and 06/15 removes it; a stunned monster plays its
  stun animation; 06/17 shows the damage of a burn (notes-buffs).
- Leave the client idle in a maze for several minutes: the keep-alive of the maze is answered, no disconnect.
- Walk into the event, script and quest boxes and use an interaction: nothing is refused (the server checks that the
  player stands at the box, with a slack of 1000 units for moves it has not heard yet).
- **new** Revive with the cash option: free today (A8).

**Party and party maze** (notes-party, notes-party-maze)
- Errors as 12/(sub | 0x80) and those the original sends inside 12/01 show the same; the inviter waits for nothing.
- A recruitment of somebody alone shows, and the client accepts its number in 12/38 and 12/43.
- The question window of 12/47 for master and members, the answer 11/48 and the close on 11/49 with the right name.
- 04/11 shows a member arriving while the other is in the maze and 04/12 removes it; the monsters a late member sees
  stand where they stand and fight the newcomer; 12/12 updates the party window in the maze and 12/46 in the district;
  11/76 lists the members; the error codes 55004, 55040, 55041, 55047 on the master's 04/01.
- **new** A late member: are monsters killed before shown (11/68 always empty)?
- **new** What the other member sees of a held or charged skill (B1).

**Helpers** (notes-helpers)
- The client accepts 27/02 before 04/23 for its own helper; what the party panel shows for a helper; whether the recall
  button hides in a party of two or more; whether the window's text of an orb changes at the end of the day.

**League** (notes-league)
- A maze with fatigue while in a league changes gold and rank of the league window at entry and end; a level shows the
  new level, percentage and room.
- Giving and taking a position from the member pop-up; named positions show once named; bit 64 alone gives the position
  menu and permission page, without it no button and 57006; offline members show their real levels.

**Progress, titles, share points** (notes-person-progress)
- 03/61 and 03/62 are ignored in the play scene; 03/58 with an empty group shows nothing wrong; 03/70 sent again after
  a level rebuilds the window; the reward bit shows taken after 03/74; the share point window may preview other items
  than those given.

**Chat, megaphone, commands, trade password** (`docs/notes-session-gaps.md`)
- A megaphone from the bag speaks to everybody and one is gone; without one the client shows 52011.
- A test command of a player whose account has level 0 does nothing (no answer, as the original); with
  `Commands:OpenToEveryone` it runs.
- Whether the client shows anything for the GM byte of the person record, now 0 for players (it was always 1).
- Set a trade password from the pop-up (four of the digits 0 to 7), trade, change server, trade again: the pop-up asks
  again on the new server; a wrong one shows 49730, five wrong ones lock (49732).

**Mail and trade** (notes-post, notes-trade)
- The flags, the times, the trailing byte of a list and the fee of a letter.
- A trade and a private shop end to end with two clients (no stand run yet); whether a visitor's purchase is sent the
  way the server guesses.
## Messages per group

One line per message the client sends or reads. "Servers" are those that load a handler; "Built in" names the
classes that build a notice. Opcodes in hex, as in the code.

Requests (client to server): 273. real 203, partial 18, invented 2, ignored 50; never sent by this client but typed on the server: 19.

Answers and notices (server to client): 477. real 321, partial 7, invented 2, not sent 147.

| Group | Requests | real | partial | invented | ignored | Notices | real | partial | invented | not sent |
|---|---|---|---|---|---|---|---|---|---|---|
| 01 System | 4 | 3 | 0 | 0 | 1 | 3 | 2 | 0 | 0 | 1 |
| 02 Login | 6 | 6 | 0 | 0 | 0 | 5 | 5 | 0 | 0 | 0 |
| 03 Character | 18 | 15 | 3 | 0 | 0 | 43 | 29 | 1 | 0 | 13 |
| 04 World | 7 | 4 | 2 | 0 | 1 | 21 | 12 | 0 | 0 | 9 |
| 05 Move | 12 | 10 | 0 | 0 | 2 | 22 | 12 | 0 | 0 | 10 |
| 06 Skill | 21 | 10 | 5 | 0 | 6 | 44 | 20 | 0 | 0 | 24 |
| 07 Chat | 5 | 5 | 0 | 0 | 0 | 7 | 5 | 0 | 0 | 2 |
| 08 Item | 13 | 11 | 1 | 0 | 1 | 38 | 25 | 0 | 0 | 13 |
| 09 Shop | 10 | 0 | 1 | 1 | 8 | 17 | 1 | 1 | 1 | 14 |
| 0A Trade | 11 | 9 | 2 | 0 | 0 | 15 | 14 | 0 | 0 | 1 |
| 11 Maze | 13 | 11 | 0 | 0 | 2 | 30 | 16 | 3 | 0 | 11 |
| 12 Party | 22 | 22 | 0 | 0 | 0 | 34 | 32 | 0 | 0 | 2 |
| 13 Item upgrade | 0 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 3 |
| 14 Drop | 1 | 1 | 0 | 0 | 0 | 3 | 3 | 0 | 0 | 0 |
| 15 Quest | 6 | 6 | 0 | 0 | 0 | 11 | 10 | 0 | 0 | 1 |
| 17 Monster | 4 | 0 | 0 | 0 | 4 | 12 | 4 | 0 | 0 | 8 |
| 18 Item setup | 22 | 22 | 0 | 0 | 0 | 21 | 20 | 0 | 0 | 1 |
| 19 Friend | 14 | 14 | 0 | 0 | 0 | 17 | 17 | 0 | 0 | 0 |
| 20 Post | 15 | 15 | 0 | 0 | 0 | 17 | 17 | 0 | 0 | 0 |
| 21 Soul metry | 1 | 1 | 0 | 0 | 0 | 6 | 6 | 0 | 0 | 0 |
| 22 League | 20 | 20 | 0 | 0 | 0 | 36 | 35 | 0 | 0 | 1 |
| 23 Gesture | 2 | 1 | 1 | 0 | 0 | 2 | 2 | 0 | 0 | 0 |
| 24 Daily mission | 2 | 2 | 0 | 0 | 0 | 4 | 4 | 0 | 0 | 0 |
| 25 Vacuum | 2 | 1 | 1 | 0 | 0 | 6 | 6 | 0 | 0 | 0 |
| 26 My room | 11 | 0 | 0 | 0 | 11 | 13 | 0 | 0 | 0 | 13 |
| 27 Helper | 10 | 10 | 0 | 0 | 0 | 14 | 14 | 0 | 0 | 0 |
| 28 Infinite tower | 2 | 0 | 0 | 0 | 2 | 2 | 0 | 0 | 0 | 2 |
| 29 Booster | 0 | 0 | 0 | 0 | 0 | 4 | 4 | 0 | 0 | 0 |
| 2A Event | 2 | 2 | 0 | 0 | 0 | 6 | 4 | 0 | 0 | 2 |
| 2B Exchange | 9 | 0 | 0 | 0 | 9 | 9 | 0 | 0 | 0 | 9 |
| 2C Ranking | 3 | 0 | 2 | 1 | 0 | 3 | 0 | 2 | 1 | 0 |
| 2D Social item | 3 | 0 | 0 | 0 | 3 | 7 | 0 | 0 | 0 | 7 |
| F1 Channel | 2 | 2 | 0 | 0 | 0 | 2 | 2 | 0 | 0 | 0 |

#### 01 System

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 01/02 | SystemOptionUpdateRequest | Auth,District,Gate,Maze | SystemService | real | saved to the account by every server; a district and a maze also update the person and what the others read from it |
| 01/03 | SystemXignCodeRequest | Auth,District,Gate,Maze | SystemService | ignored | anti-cheat token, accepted and dropped by design |
| 01/05 | SystemKeepAliveRequest | District,Maze | SystemService | real | district and maze; the client sends it only in answer to the keep-alive of the server |
| 01/06 | SystemPingRequest | District,Maze | SystemService | real | district and maze |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 01/05 | SystemKeepAliveResponse | KeepAliveService | real |  |
| 01/06 | SystemPingResponse | SystemService | real |  |
| 01/07 | SystemServerOptionUpdateResponse | - | not sent |  |

#### 02 Login

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 02/01 | LoginAuthRequest | Auth | LoginService | real |  |
| 02/03 | LoginGateListRequest | Auth | GateService | real |  |
| 02/05 | LoginGateConnectRequest | Auth | GateService | real |  |
| 02/13 | LoginEnterServerRequest | Gate | LoginService | real |  |
| 02/15 | LoginNextHumanNetworkRequest | Auth | LoginService | real | behind a setting, off by default |
| 02/32 | LoginOptionUpdateRequest | Auth,Gate | LoginService, SystemService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 02/02 | LoginResponse | LoginGameplay | real |  |
| 02/04 | LoginGateListResponse | GateGameplay | real |  |
| 02/11 | LoginGateConnectResponse | GateGameplay | real |  |
| 02/14 | LoginEnterGateResponse | LoginGameplay | real |  |
| 02/31 | LoginOptionLoadResponse | GateGameplay, OptionCommand | real |  |

#### 03 Character

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 03/01 | PersonCreateRequest | Gate | PersonService | partial | no filter of forbidden names (`tb_NameFilter`) |
| 03/02 | PersonDeleteReqRequest | Gate | PersonService | partial | no check of the waiting time after creation |
| 03/11 | PersonListRequest | Gate | PersonService | real |  |
| 03/13 | PersonSelectRequest | Gate | PersonService | real |  |
| 03/17 | PersonSecondPasswordRequest | Gate | PersonService | real |  |
| 03/18 | PersonTradePasswordRequest | District,Maze | TradePasswordService | real | kept hashed on the account and checked; five wrong ones lock it on that server; given again on each server |
| 03/19 | PersonUpdateCutsceneRequest | Maze | MazeService | real | maze only; the district has no handler |
| 03/21 | PersonEnterGameServerRequest | District,Maze | PersonService | real |  |
| 03/23 | PersonLoadTitleRequest | District,Maze | PersonProfileService | real |  |
| 03/25 | PersonUpdateTitleRequest | District,Maze | PersonProfileService | real |  |
| 03/31 | PersonInfoReqRequest | District,Maze | PersonService | real |  |
| 03/42 | PersonReviveReqRequest | Maze | VitalsService | partial | cash revive is free; no revive item, no free revive count of the maze |
| 03/45 | PersonChangeMotionReqRequest | District,Maze | MovementService | real |  |
| 03/60 | PersonChangeServerRequest | District,Gate,Maze | PersonService | real |  |
| 03/63 | PersonGetRewardSharePointRequest | District,Maze | ProgressService | real |  |
| 03/74 | PersonAchieveRewardRequest | District,Maze | ProgressService | real |  |
| 03/75 | PersonOtherInfoRequest | District,Maze | PersonProfileService | real |  |
| 03/77 | PersonCommunityRequest | District,Maze | PersonProfileService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 03/04 | PersonKickOutResponse | GateGameplay, ServerService | real |  |
| 03/12 | CharacterListResponse | PersonSnapshotMapper | real |  |
| 03/14 | CharacterSelectResponse | PersonGameplay, WorldManager | real |  |
| 03/15 | PersonEnterPrologueResponse | PrologueEntry | real |  |
| 03/16 | PersonEnterBattleZoneResponse | - | not sent |  |
| 03/17 | CharacterSecondPasswordResponse | SecondPasswordGameplay | real |  |
| 03/18 | CharacterTradePasswordResponse | TradePasswordService | real |  |
| 03/20 | PersonPlayCutsceneResponse | LuaMaze | real |  |
| 03/22 | PersonEnterGameServerResponse2 | - | not sent |  |
| 03/23 | PersonLoadTitleResponse | TitleBook | real |  |
| 03/24 | PersonAddTitleResponse | TitleBook | real |  |
| 03/25 | PersonUpdateTitleResponse | PersonProfileServiceBase | real |  |
| 03/26 | PersonUpdateTitleBroadcastResponse | PersonProfileServiceBase | real |  |
| 03/27 | PersonClearTitleResponse | - | not sent |  |
| 03/28 | PersonDeleteTitleResponse | - | not sent |  |
| 03/29 | PersonUpdateOpenTitleResponse | - | not sent |  |
| 03/32 | CharacterInfoResponse | PersonSnapshotMapper | real |  |
| 03/34 | PersonUpdateStatListResponse | LuaCreatureManager, MazeBuffs, PlayerGauges | real |  |
| 03/35 | PersonUpdateStatResponse | - | not sent |  |
| 03/36 | PersonLevelResponse | ExperienceService, PersonLevelCommand | real |  |
| 03/37 | PersonExpResponse | ExperienceService | real |  |
| 03/41 | PersonDieResponse | LuaCreatureManager, MazeBuffs, PersonDieCommand | real |  |
| 03/44 | PersonReviveResponse | LuaCreatureManager | real | the export labels it 03/43; the client reads it at 03/44 |
| 03/46 | PersonChangeMotionBroadcastResponse | MovementServiceBase | real |  |
| 03/51 | PersonUpdateOriginStatListResponse | PlayerStatsService, ServiceChannel | partial | origin stats without gear: in progress (feature/equipment-stats) |
| 03/52 | PersonUpdateEchelonInfoResponse | - | not sent |  |
| 03/53 | PersonUpdateEchelonBroadcastResponse | - | not sent |  |
| 03/57 | PersonCheckNameResponse | - | not sent |  |
| 03/58 | PersonUpdateSharePointResponse | SharePointManager | real |  |
| 03/60 | PersonGobackLobbyResponse | LeaveManager | real |  |
| 03/61 | PersonLoadDistrictStateResponse | SharePointManager | real |  |
| 03/62 | PersonLoadMazeStateResponse | SharePointManager | real |  |
| 03/63 | PersonGetRewardSharePointResponse | SharePointManager | real |  |
| 03/64 | PersonFpUpdateResponse | - | not sent | fatigue: in progress (feature/maze-results) |
| 03/65 | PersonNpcCreditLoadResponse | - | not sent | NPC credit: in progress (feature/npc-services) |
| 03/66 | PersonNpcCreditUpdateResponse | - | not sent | NPC credit: in progress (feature/npc-services) |
| 03/67 | PersonNpcCreditInitResponse | - | not sent | NPC credit: in progress (feature/npc-services) |
| 03/68 | PersonOtherInfoRefuseResponse | OtherPersonService | real |  |
| 03/70 | PersonAchieveSelectResponse | AchievementManager | real |  |
| 03/71 | PersonAchieveUpdateResponse | AchievementManager | real |  |
| 03/74 | PersonAchieveRewardResponse | ProgressService | real |  |
| 03/75 | PersonOtherInfoResponse | OtherPersonService | real |  |
| 03/77 | PersonCommunityResponse | PersonProfileServiceBase | real |  |

#### 04 World

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 04/01 | WorldEnterRequest | District,Maze | MazeService, WorldService | partial | any location is entered: no level, story, entry item, entry count or distance check |
| 04/06 | WorldObjectRequest | District | ChannelService | real |  |
| 04/07 | WorldWarpRequest | Maze | MazeService | real |  |
| 04/0B | WorldCheckSectorRequest | Maze | MazeService | real |  |
| 04/0D | WorldWarpOutRequest | Maze | MazeService | ignored | portals do not wait for the party |
| 04/33 | WorldEscapeRequest | Maze | MazeService | real |  |
| 04/41 | MazeCreateRequest | District | WorldService | partial | as 04/01: no entry rule of the maze is checked |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 04/02 | WorldEnterResponse | MazeService, WorldManager | real |  |
| 04/03 | WorldCurrentDateClientMessage | LoginGameplay | real |  |
| 04/04 | WorldVersionResponse | - | not sent |  |
| 04/08 | WorldWarpResponse | MazeService, MovementServiceBase | real |  |
| 04/09 | WorldWarpEnableResponse | LuaMaze | real |  |
| 04/0A | WorldGateEnableResponse | LuaMaze | real |  |
| 04/0E | WorldWarpMazeResponse | MazeService | real |  |
| 04/0F | WorldWarpMazeTimeResponse | - | not sent |  |
| 04/11 | WorldInInfoPcResponse | MazeService, ServiceChannel | real |  |
| 04/12 | WorldOutInfoPcResponse | LuaMaze, ServiceChannel | real |  |
| 04/13 | WorldInInfoNpcResponse | - | not sent |  |
| 04/14 | WorldOutInfoNpcResponse | - | not sent |  |
| 04/15 | WorldInInfoMonsterResponse | HelperSquad | real |  |
| 04/16 | WorldOutInfoMonsterResponse | - | not sent |  |
| 04/18 | WorldInInfoSpawnBoxResponse | - | not sent |  |
| 04/21 | WorldOtherPersonListResponse | ServiceChannel | real |  |
| 04/22 | WorldOtherInfosNpcResponse | ChannelService, LuaCreatureManager | real |  |
| 04/23 | WorldOtherInfosMonsterResponse | HelperSquad, LuaCreatureManager | real |  |
| 04/24 | WorldOutOtherInfosPcResponse | - | not sent |  |
| 04/44 | WorldEnterMazeLimitCountResetResponse | - | not sent | entry limits of mazes |
| 04/45 | WorldEnterMazeLimitCountUpdateResponse | - | not sent | entry limits of mazes |

#### 05 Move

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 05/01 | MovementMoveRequest | District,Maze | MovementService | real |  |
| 05/03 | MovementStopRequest | District,Maze | MovementService | real |  |
| 05/05 | MovementJumpRequest | District,Maze | MovementService | real |  |
| 05/07 | MovementBattleRequest | District,Maze | MovementService | real |  |
| 05/10 | MovementMotionRequest | District,Maze | MovementService | real |  |
| 05/13 | MovementIgnoreMotionDeltaRequest | District,Maze | MovementService | real |  |
| 05/16 | MovementTransportTakeRequest | District,Maze | MovementService | ignored | transports of the district are not run |
| 05/22 | MovementPositionRequest | District,Maze | MovementService | real |  |
| 05/30 | MovementLoopMotionStartRequest | District,Maze | MovementService | real |  |
| 05/32 | MovementLoopMotionEndRequest | District,Maze | MovementService | real |  |
| 05/36 | MovementAttacedEndRequest | District,Maze | MovementService | real |  |
| 05/40 | MovementGroundStatusRequest | District,Maze | MovementService | ignored | ground state kept by nobody |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 05/02 | MovementMoveBtResponse | LuaCreatureManager, MonsterBody, MovementServiceBase | real |  |
| 05/04 | MovementStopBtResponse | LuaCreatureManager, MonsterBody, MovementServiceBase | real |  |
| 05/06 | MovementJumpBtResponse | MovementServiceBase | real |  |
| 05/08 | MovementBattleBtResponse | MonsterBody, MovementServiceBase | real |  |
| 05/0A | MovementGazeBtResponse | MonsterBody | real |  |
| 05/0B | MovementTraceBtResponse | - | not sent |  |
| 05/0C | MovementIdleBtResponse | MonsterBody | real |  |
| 05/0D | MovementInfoBtResponse | - | not sent |  |
| 05/0F | MovementIdlesBtResponse | - | not sent |  |
| 05/11 | MovementMotionBtResponse | MovementServiceBase | real |  |
| 05/12 | MovementStiffenBtResponse | - | not sent |  |
| 05/13 | MovementIgnoreMotionDeltaResponse | MovementServiceBase | real |  |
| 05/14 | MovementUpdateDirResponse | LuaCreatureManager, MonsterBody | real |  |
| 05/15 | MovementDropResponse | - | not sent |  |
| 05/16 | MovementTransportTakeResponse | - | not sent |  |
| 05/17 | MovementTransportOffResponse | - | not sent |  |
| 05/20 | MovementGrapResponse | - | not sent |  |
| 05/21 | MovementRotationResponse | - | not sent |  |
| 05/31 | MovementLoopMotionStartBtResponse | MovementServiceBase | real |  |
| 05/33 | MovementLoopMotionEndBtResponse | MovementServiceBase | real |  |
| 05/34 | MovementAttacedBtResponse | - | not sent |  |
| 05/35 | MovementAttacedEndBtResponse | MovementServiceBase | real |  |

#### 06 Skill

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 06/05 | SkillPassiveEndRequest | District,Maze | MazeService, SkillService | real |  |
| 06/06 | SkillPassiveRequest | District,Maze | MazeService, SkillService | partial | acknowledged with no check of its own |
| 06/08 | SkillActiveSkillRequest | District,Maze | MazeService, SkillService | real | maze: owned, cost, cooldown, state (notes-combat); district: acknowledged |
| 06/20 | SkillAkashicRecordRequest | District | SkillService | ignored | akashic card summon in combat: empty in the district, no handler in the maze |
| 06/31 | SkillProjectileRequest | District,Maze | MazeService, SkillService | real |  |
| 06/33 | SkillTrapPosUpdateRequest | District,Maze | MazeService, SkillService | real |  |
| 06/36 | SkillPreTargetListRequest | District | SkillService | ignored | empty in the district, no handler in the maze |
| 06/37 | SkillChainRequest | District,Maze | MazeService, SkillService | real |  |
| 06/41 | SkillKeypressRequest | District,Maze | MazeService, SkillService | partial | maze runs the hold; the others are not told (06/42 never sent) |
| 06/43 | SkillTargetChangeRequest | District | SkillService | ignored | no handler in the maze; 06/44 never sent |
| 06/45 | SkillWarpPositionRequest | District,Maze | MazeService, SkillService | real |  |
| 06/47 | SkillSyncPositionRequest | District,Maze | MazeService, SkillService | partial | place taken when reachable; the others are not told (06/48 never sent) |
| 06/4A | SkillSubInputRequest | District | SkillService | ignored | empty in the district, no handler in the maze |
| 06/4C | SkillChargingStartRequest | District | SkillService | ignored | charging: empty in the district, no handler in the maze; 06/4D and 06/50 never sent |
| 06/71 | SkillLearnRequest | District,Maze | SkillWindowService | real | district and maze |
| 06/72 | SkillResetRequest | District,Maze | SkillWindowService | partial | district and maze; free: the original resets through the use of an item of that kind |
| 06/75 | SkillUpdateDeckRequest | District,Maze | SkillWindowService | real | district and maze |
| 06/76 | SkillAddDeckSlotRequest | District,Maze | SkillWindowService | partial | district and maze; free: whether the original takes an item for it was not read |
| 06/77 | SkillDivergenceLearnRequest | District,Maze | SkillWindowService | real | district and maze |
| 06/79 | SkillDeckBonusRequest | District,Maze | SkillWindowService | real | stored as sent, checked when a skill reads it |
| 06/7A | SkillResetTagetRequest | District | SkillService | ignored | empty in the district, no handler in the maze |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 06/07 | SkillPassiveResponse | MazeService, SkillService | real |  |
| 06/09 | SkillActiveResponse | MazeService, SkillService | real |  |
| 06/10 | SkillActiveBroadcastResponse | MonsterBody | real |  |
| 06/12 | SkillActionResponse | - | not sent |  |
| 06/13 | SkillActionBroadcastResponse | HelperSquad, LuaCreatureManager, LuaMaze, LuaMaze.Helpers | real |  |
| 06/14 | SkillUpdateBuffResponse | MazeBuffs | real |  |
| 06/15 | SkillBuffDeleteBroadcastResponse | MazeBuffs | real |  |
| 06/16 | SkillBuffChangeBroadcastResponse | MazeBuffs | real |  |
| 06/17 | SkillBuffDamageBroadcastResponse | MazeBuffs | real |  |
| 06/18 | SkillAuraUpdateBroadcastResponse | - | not sent |  |
| 06/21 | SkillAkashicRecordResponse | - | not sent | akashic card summon |
| 06/22 | SkillAkashicRecordBroadcastResponse | - | not sent | akashic card summon |
| 06/32 | SkillProjectileBroadcastResponse | LuaMaze, MonsterBody | real |  |
| 06/34 | SkillTrapPosUpdateBroadcastResponse | - | not sent |  |
| 06/35 | SkillProjectileAttachBroadcastResponse | - | not sent |  |
| 06/36 | SkillPreTargetListResponse | - | not sent |  |
| 06/38 | SkillChainBroadcastResponse | LuaMaze | real |  |
| 06/39 | SkillChainUpdateTargetResponse | LuaMaze | real |  |
| 06/3A | SkillProjectileUpdateTargetResponse | - | not sent |  |
| 06/3B | SkillProjectileRemoveResponse | - | not sent |  |
| 06/42 | SkillKeypressBroadcastResponse | - | not sent | party members do not see a held skill |
| 06/44 | SkillTargetChangeBroadcastResponse | - | not sent | the client reads it with the reader of 17/22; never sent for a player |
| 06/46 | SkillWarpPositionBroadcastResponse | MazeService, MonsterBody | real | the export puts another receiver here; the server sends the warp of a skill at this case |
| 06/48 | SkillSyncPositionBroadcastResponse | - | not sent | party members are not told the sync place |
| 06/49 | SkillMoveWithTimeBroadcastResponse | - | not sent |  |
| 06/4B | SkillSubInputBroadcastResponse | - | not sent | sub input of others |
| 06/4D | SkillChargingStartBroadcastResponse | - | not sent | charging of others |
| 06/50 | SkillChargingEndBroadcastResponse | - | not sent | charging of others |
| 06/61 | SkillStartSoulViperBroadcastResponse | - | not sent |  |
| 06/62 | SkillEndSoulViperBroadcastResponse | - | not sent |  |
| 06/63 | SkillDeleteResponse | - | not sent |  |
| 06/70 | SkillLoadInfoResponse | PersonService, PersonSkills | real |  |
| 06/71 | SkillLearnResponse | SkillWindowService | real |  |
| 06/72 | SkillResetResponse | SkillWindowService | real |  |
| 06/73 | SkillUpdatePointResponse | SkillLearning, SkillWindowService | real |  |
| 06/74 | SkillLoadDeckResponse | - | not sent |  |
| 06/75 | SkillUpdateDeckResponse | SkillWindowService | real |  |
| 06/76 | SkillAddDeckSlotResponse | SkillWindowService | real |  |
| 06/77 | SkillDivergenceLearnResponse | SkillWindowService | real |  |
| 06/79 | SkillDeckBonusResponse | SkillService | real | the export labels it 06/78; the client reads it at 06/79 |
| 06/7A | SkillResetTagetResponse | - | not sent |  |
| 06/7B | SkillSummonAkashicResponse | - | not sent | akashic card summon |
| 06/7C | SkillCooltimeReduceResponse | - | not sent |  |
| 06/7D | SkillDefenceTypeResponse | - | not sent |  |

#### 07 Chat

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 07/01 | ChatNormalRequest | District,Maze | ChatService | real |  |
| 07/02 | ChatWhisperRequest | District,Maze | ChatService | real |  |
| 07/03 | ChatTradeRequest | District,Maze | ChatService | real |  |
| 07/06 | ChatMegaphoneRequest | District,Maze | ChatService | real | takes one megaphone (use kind 71) from the slot it names; 52011 or 52002 without one |
| 07/0A | ChatStuffRequest | District,Maze | ChatService | real | a command runs for the access level of the account (a game master by default); `Commands:OpenToEveryone` opens them on a test stand |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 07/01 | ChatNormalResponse | ChatServiceBase | real |  |
| 07/02 | ChatWhisperResponse | ChatServiceBase | real |  |
| 07/03 | ChatTradeResponse | ChatServiceBase | real |  |
| 07/04 | ChatNoticeResponse | LuaMaze, NoticeCommand | real |  |
| 07/05 | ChatNotifyResponse | - | not sent |  |
| 07/06 | ChatMegaphoneResponse | ChatServiceBase | real |  |
| 07/07 | ChatMemoryResponse | - | not sent |  |

#### 08 Item

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 08/01 | ItemInventoryInfoRequest | District,Maze | ItemService | real |  |
| 08/02 | ItemMoveRequest | District,Maze | ItemService | real | the others who see the player get 08/0D with the worn slots that changed |
| 08/03 | ItemCombineRequest | District,Maze | ItemService | real |  |
| 08/04 | ItemDivideRequest | District,Maze | ItemService | real |  |
| 08/05 | ItemBreakRequest | District,Maze | ItemService | real |  |
| 08/0F | ItemAddSlotRequest | District,Maze | ItemService | real |  |
| 08/10 | ItemBankInfoRequest | District,Maze | ItemService | real | NPC of the bank not checked: in progress (feature/npc-services) |
| 08/11 | ItemUseRequest | District,Maze | ItemService | partial | 5 kinds of use of about 60 (booster, akashic card, package, costume package, random box); no potions or effects, no cooldowns, no 08/11 or 08/13 answer |
| 08/25 | ItemLineUpRequest | District,Maze | ItemService | real |  |
| 08/27 | ItemUpdateQuickslotCardRequest | District,Maze | ItemService | real | contents not checked against the book |
| 08/28 | ItemUpdateQuickslotItemRequest | District,Maze | ItemService | real | contents not checked |
| 08/47 | ItemMazeRewardItemRequest | Maze | MazeRewardService | real |  |
| 08/52 | ItemAppearanceEquipRequest | District,Maze | ItemService | ignored | appearance items not kept |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 08/01 | ItemInventoryInfoResponse | ItemServiceBase | real |  |
| 08/02 | ItemMoveResponse | InventoryService | real |  |
| 08/03 | ItemCombineResponse | InventoryService | real |  |
| 08/04 | ItemDivideResponse | InventoryService | real |  |
| 08/05 | ItemBreakResponse | InventoryService, TradeGoods | real |  |
| 08/06 | ItemCreateResponse | AkashicService, InventoryService, ItemEvolutionService, PostService | real |  |
| 08/0D | ItemMoveBtResponse | ItemServiceBase | real |  |
| 08/0E | ItemOpenSlotInfoResponse | InventoryService | real |  |
| 08/0F | ItemAddSlotResponse | InventoryService | real |  |
| 08/10 | ItemBankInfoResponse | ItemServiceBase | real |  |
| 08/11 | ItemUseResponse | - | not sent | the reward window of an opened box never shows |
| 08/12 | ItemReduceResponse | InventoryService | real |  |
| 08/13 | ItemUseBtResponse | - | not sent | no use motion and no cooldown of a used item |
| 08/14 | ItemEquipSlotOpenResponse | - | not sent |  |
| 08/15 | ItemUpdateResponse | InventoryService, ItemBroachService, ItemRestoreService, ItemSocketService | real |  |
| 08/20 | ItemUpdateInvenMoneyResponse | CurrencyCommand, InventoryService, LeagueServiceBase | real |  |
| 08/25 | ItemLineUpResponse | InventoryService | real |  |
| 08/26 | ItemLoadQuickslotResponse | ItemServiceBase | real |  |
| 08/27 | ItemUpdateQuickslotCardResponse | ItemServiceBase | real |  |
| 08/28 | ItemUpdateQuickslotItemResponse | ItemServiceBase | real |  |
| 08/31 | ItemUpdateBpResponse | InventoryService | real |  |
| 08/32 | ItemUpdateEtherResponse | InventoryService | real |  |
| 08/33 | ItemUpdateCashResponse | InventoryService | real |  |
| 08/34 | ItemUpdateFriendPointResponse | - | not sent |  |
| 08/47 | ItemMazeRewardItemResponse | MazeRewardService | real |  |
| 08/48 | ItemUseInfoListResponse | - | not sent | item cooldowns |
| 08/49 | ItemUseInfoUpdateResponse | - | not sent |  |
| 08/4A | ItemCubeReqResponse | - | not sent |  |
| 08/50 | ItemAppearanceLoadResponse | - | not sent |  |
| 08/52 | ItemAppearanceEquipResponse | - | not sent |  |
| 08/55 | ItemSocketLoadResponse | ItemServiceBase | real |  |
| 08/56 | ItemBroachLoadResponse | ItemServiceBase | real |  |
| 08/57 | ItemAkashicRecordLoadResponse | ItemServiceBase | real |  |
| 08/58 | ItemAkashicRecordAddResponse | AkashicService | real |  |
| 08/59 | ItemUnknown1Response | - | not sent |  |
| 08/60 | ItemUnknown2Response | - | not sent |  |
| 08/61 | ItemSocketUpdateResponse | - | not sent |  |
| 08/62 | ItemBroachUpdateResponse | - | not sent |  |

#### 09 Shop

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 09/01 | ShopBuyRequest | District | ShopService | partial | in progress (feature/npc-services): gold only, no NPC, silent refusals |
| 09/02 | ShopSellRequest | District | ShopService | ignored | in progress (feature/npc-services) |
| 09/03 | ShopRepurchaserListRequest | District | ShopService | ignored | in progress (feature/npc-services) |
| 09/04 | ShopRepurchaserRequest | District | ShopService | ignored | in progress (feature/npc-services) |
| 09/20 | ShopCashLoadRequest | District | CashShopService | invented | one hard-coded item with a made-up price |
| 09/21 | ShopCashBuyRequest | District | CashShopService | ignored | cash purchase |
| 09/23 | ShopCashSetRequest | District | CashShopService | ignored | cash cart |
| 09/24 | ShopCashSetDelRequest | District | CashShopService | ignored | cash cart |
| 09/25 | ShopCashGiftRequest | District | CashShopService | ignored | cash gift |
| 09/26 | ShopGachaRequest | District | ShopService | ignored | gacha |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 09/01 | ShopBuyResponse | ShopService | real |  |
| 09/02 | ShopSellResponse | - | not sent | in progress (feature/npc-services) |
| 09/03 | ShopRepurchaserListResponse | - | not sent | in progress (feature/npc-services) |
| 09/04 | ShopRepurchaserResponse | - | not sent | in progress (feature/npc-services) |
| 09/05 | ShopRepurchaserAddResponse | - | not sent | in progress (feature/npc-services) |
| 09/06 | ShopRepurchaserDeleteResponse | - | not sent | in progress (feature/npc-services) |
| 09/10 | ShopItemLoadResponse | - | not sent | limited stock of a shop: in progress (feature/npc-services) |
| 09/11 | ShopItemUpdateResponse | - | not sent | in progress (feature/npc-services) |
| 09/12 | ShopItemInitResponse | - | not sent | in progress (feature/npc-services) |
| 09/20 | ShopCashLoadResponse | CashShopService | invented | one hard-coded cash item |
| 09/21 | ShopCashBuyResponse | - | not sent |  |
| 09/22 | ShopCashSetLoadResponse | - | not sent |  |
| 09/23 | ShopCashSetResponse | - | not sent |  |
| 09/24 | ShopCashSetDelResponse | - | not sent |  |
| 09/25 | ShopCashGiftResponse | - | not sent |  |
| 09/27 | ShopGachaResResponse | - | not sent |  |
| 09/28 | ShopBannerLoadResponse | CashShopService | partial | always empty |

#### 0A Trade

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 0A/01 | TradeRequest | District | TradeService | real |  |
| 0A/02 | TradeAcceptRequest | District | TradeService | real |  |
| 0A/03 | TradeUpdateItemRequest | District | TradeService | real |  |
| 0A/04 | TradeUpdateMoneyRequest | District | TradeService | real |  |
| 0A/05 | TradeCheckRequest | District | TradeService | real |  |
| 0A/06 | TradeConfirmRequest | District | TradeService | real |  |
| 0A/08 | TradeCancelRequest | District | TradeService | real |  |
| 0A/09 | TradePrivateShopStartRequest | District | TradeService | partial | no shop permit used, no shop zone checked |
| 0A/0A | TradePrivateShopItemRequest | District | TradeService | partial | a purchase by a visitor is a guess at the request |
| 0A/0B | TradePrivateShopStateRequest | District | TradeService | real |  |
| 0A/0E | TradePrivateShopSelectRequest | District | TradeService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 0A/01 | TradeResponse | Trades | real |  |
| 0A/02 | TradeAcceptResponse | Trades | real |  |
| 0A/03 | TradeUpdateItemResponse | Trades | real |  |
| 0A/04 | TradeUpdateMoneyResponse | Trades | real |  |
| 0A/05 | TradeCheckResponse | Trades | real |  |
| 0A/06 | TradeConfirmResponse | Trades | real |  |
| 0A/07 | TradeResultResponse | Trades | real |  |
| 0A/08 | TradeCancelResponse | TradeService, Trades | real |  |
| 0A/09 | TradePrivateShopStartResponse | PrivateShops | real |  |
| 0A/0A | TradePrivateShopItemResponse | PrivateShops | real |  |
| 0A/0B | TradePrivateShopStateResponse | PrivateShops | real |  |
| 0A/0C | TradePrivateShopBuyResponse | PrivateShops | real |  |
| 0A/0D | TradePrivateShopSellResponse | PrivateShops | real |  |
| 0A/0E | TradePrivateShopSelectResponse | PrivateShops | real |  |
| 0A/0F | TradePrivateShopNameResponse | - | not sent |  |

#### 11 Maze

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 11/22 | MazeCompleteRequest | Maze | MazeService | real |  |
| 11/23 | MazeExitRequest | Maze | MazeService | real |  |
| 11/31 | MazeEventSpawnBoxRequest | Maze | MazeService | real | the player must stand at the box |
| 11/33 | MazeSceneDirectingRequest | Maze | MazeService | real |  |
| 11/35 | MazeCheckEventSpawnBoxRequest | Maze | MazeService | real | the player must stand at the box |
| 11/48 | MazeEnterPartyAnswerRequest | District,Maze | MazeService, WorldService | real |  |
| 11/62 | MazeOperationEndRequest | Maze | MazeService | real |  |
| 11/65 | MazeQuestMoveCheckRequest | Maze | MazeService | real | the player must stand at the box |
| 11/71 | MazeMonsterClientSpawnRequest | Maze | MazeService | ignored | by design: the server keeps every monster |
| 11/77 | MazeInteractionClickRequest | Maze | MazeService | real | the player must stand at the object |
| 11/79 | MazeInteractionMotionRequest | Maze | MazeService | ignored | the others do not see the motion |
| 11/7C | MazeSwitchNpcClickRequest | Maze | MazeService | real |  |
| 11/7E | MazeLuaFunctionRequest | District,Maze | MazeService | real | maze runs the script when the player stands at the box; the district echoes the box without running anything |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 11/34 | MazeSceneDirectingResponse | MazeService | real |  |
| 11/36 | CheckEventSpawnBoxBt | - | not sent |  |
| 11/37 | HiddenEventInfo | - | not sent |  |
| 11/38 | MazeTimerResponse | LuaMaze | real |  |
| 11/42 | - | - | not sent |  |
| 11/44 | MazeEnterResponse | - | not sent |  |
| 11/49 | MazeCancelEnterPartyResponse | PartyMazeEntry | real |  |
| 11/50 | - | - | not sent |  |
| 11/51 | MazeNotice | - | not sent |  |
| 11/52 | MazeClearSectorResponse | LuaMaze | real |  |
| 11/53 | MazeSectorInfosResponse | MazeService | real |  |
| 11/54 | MazeGateInfosResponse | MazeService | real |  |
| 11/55 | MazePortalInfosResponse | MazeService | real |  |
| 11/56 | MazeInteractionInfosResponse | MazeService | real |  |
| 11/57 | MazeInteractionEnableResponse | LuaMaze | real |  |
| 11/58 | MonsterHpchangeState | - | not sent |  |
| 11/59 | MazeUpdateUnityPoint | - | not sent |  |
| 11/60 | MazeLuaClientSyncResponse | LuaMaze | real |  |
| 11/63 | MazeWarpInSectorResponse | LuaMaze | real |  |
| 11/64 | MazeClearInfoResponse | LuaMaze | real |  |
| 11/66 | MazeQuestMoveCheckResponse | MazeService | real |  |
| 11/67 | MazeEscortStatus | - | not sent |  |
| 11/68 | MazeDieMonstersResponse | MazeService | partial | always empty, also for a member who joins a running instance |
| 11/75 | MazeRewardInfoResponse | MazeResults | partial | in progress (feature/maze-results) |
| 11/76 | MazeRewardPartyInfoResponse | MazeResults | partial | in progress (feature/maze-results) |
| 11/78 | MazeInteractionRunResponse | MazeService | real |  |
| 11/79 | MazeInteractionMotion | - | not sent |  |
| 11/7A | MazeSwitchNpcEnableResponse | - | not sent |  |
| 11/7B | MazeSwitchNpcStatusResponse | LuaMaze | real |  |
| 11/7F | MazeLuaFunctionResponse | MazeService | real |  |

#### 12 Party

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 12/01 | PartyInviteRequest | District,Maze | PartyService | real |  |
| 12/02 | PartyAcceptRequest | District,Maze | PartyService | real |  |
| 12/03 | PartyChangeMasterRequest | District,Maze | PartyService | real |  |
| 12/04 | PartyKickOutRequest | District,Maze | PartyService | real |  |
| 12/05 | PartyLeaveRequest | District,Maze | PartyService | real |  |
| 12/08 | PartyCancelRequest | District,Maze | PartyService | real |  |
| 12/09 | PartyUpdateInfoRequest | District,Maze | PartyService | real |  |
| 12/30 | PartyMatchingEnterRequest | District,Maze | PartyMatchingService | real |  |
| 12/31 | PartyMatchingExitRequest | District,Maze | PartyMatchingService | real |  |
| 12/32 | PartyMatchingCheckRequest | District,Maze | PartyMatchingService | real |  |
| 12/36 | PartyRecruitListRequest | District,Maze | PartyRecruitService | real |  |
| 12/37 | PartyRecruitAddRequest | District,Maze | PartyRecruitService | real |  |
| 12/38 | PartyRecruitApplyRequest | District,Maze | PartyRecruitService | real |  |
| 12/39 | PartyRecruitApplyAcceptRequest | District,Maze | PartyRecruitService | real |  |
| 12/3A | PartyRecruitApplyRejectRequest | District,Maze | PartyRecruitService | real |  |
| 12/3C | PartyRecruitDelRequest | District,Maze | PartyRecruitService | real |  |
| 12/3D | PartyRecruitApplyListRequest | District,Maze | PartyRecruitService | real |  |
| 12/40 | PartyAwaiterAddRequest | District,Maze | PartyRecruitService | real |  |
| 12/41 | PartyAwaiterDelRequest | District,Maze | PartyRecruitService | real |  |
| 12/42 | PartyAwaiterListRequest | District,Maze | PartyRecruitService | real |  |
| 12/43 | PartyRecruitApplyInfoRequest | District,Maze | PartyRecruitService | real |  |
| 12/44 | PartyAwaiterInfoRequest | District,Maze | PartyRecruitService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 12/01 | PartyInviteResponse | PartyServiceBase | real |  |
| 12/02 | PartyAcceptResponse | PartyServiceBase | real |  |
| 12/03 | PartyChangeMasterResponse | PartyNotifier | real |  |
| 12/05 | PartyLeaveResponse | PartyNotifier | real |  |
| 12/06 | PartyUpdateMemberInfoResponse | PartyNotifier | real |  |
| 12/07 | PartyDeleteResponse | PartyNotifier | real |  |
| 12/08 | PartyCancelResponse | PartyServiceBase | real |  |
| 12/09 | PartyUpdateInfoResponse | PartyNotifier | real |  |
| 12/10 | PartyAddMemberResponse | PartyNotifier | real |  |
| 12/12 | PartyUpdateMemberHpResponse | PartyMazeEvents, PartyNotifier | real |  |
| 12/30 | PartyMatchingEnterResponse | PartyMatching | real |  |
| 12/31 | PartyMatchingExitResponse | PartyMatching, PartyMatchingServiceBase | real |  |
| 12/32 | PartyMatchingCheckResponse | PartyMatching | real |  |
| 12/33 | PartyMatchingResetResponse | - | not sent |  |
| 12/34 | PartyMatchingWaitResponse | PartyMatching | real |  |
| 12/35 | PartyRecruitPenaltyResponse | - | not sent |  |
| 12/36 | PartyRecruitListResponse | PartyRecruitServiceBase | real |  |
| 12/37 | PartyRecruitAddResponse | PartyRecruitServiceBase | real |  |
| 12/38 | PartyRecruitApplyResponse | PartyRecruitServiceBase | real |  |
| 12/39 | PartyRecruitApplyAcceptResponse | PartyRecruitServiceBase | real |  |
| 12/3A | PartyRecruitApplyRejectResponse | PartyRecruitServiceBase | real |  |
| 12/3B | PartyRecruitApplyUpdateResponse | PartyRecruitNotifier | real |  |
| 12/3C | PartyRecruitDelResponse | PartyRecruitNotifier | real |  |
| 12/3D | PartyRecruitApplyListResponse | PartyRecruitServiceBase | real |  |
| 12/3E | PartyRecruitInfoResponse | PartyRecruitNotifier, PartyRecruitServiceBase | real |  |
| 12/3F | PartyRecruitApplyDelResponse | PartyRecruitNotifier | real |  |
| 12/40 | PartyAwaiterAddResponse | PartyRecruitServiceBase | real |  |
| 12/41 | PartyAwaiterDelResponse | PartyRecruitServiceBase | real |  |
| 12/42 | PartyAwaiterListResponse | PartyRecruitServiceBase | real |  |
| 12/43 | PartyRecruitApplyInfoResponse | PartyRecruitServiceBase | real |  |
| 12/44 | PartyAwaiterInfoResponse | OtherPersonService | real |  |
| 12/45 | PartyRecruitApplyNoticeResponse | PartyRecruitServiceBase | real |  |
| 12/46 | PartyMazeClearResponse | PartyNotifier | real |  |
| 12/47 | MazeEnterPartyAskResponse | PartyMazeEntry | real |  |

#### 13 Item upgrade

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 13/01 | - | - | not sent |  |
| 13/02 | - | - | not sent |  |
| 13/03 | - | - | not sent |  |

#### 14 Drop

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 14/02 | DropPickUpRequest | Maze | DropRequestService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 14/01 | DropInfosResponse | DropService | real |  |
| 14/02 | DropPickUpResponse | DropService | real |  |
| 14/03 | DropDeleteResponse | DropService | real |  |

#### 15 Quest

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 15/03 | QuestAcceptRequest | District,Maze | QuestService | real |  |
| 15/05 | QuestEpisodeCompleteRequest | District,Maze | QuestService | real |  |
| 15/06 | QuestGiveUpRequest | District,Maze | QuestService | real |  |
| 15/08 | QuestEventUpdateRequest | District,Maze | QuestService | real |  |
| 15/09 | QuestHelperRequest | District,Maze | QuestService | real |  |
| 15/11 | QuestFailRequest | District,Maze | QuestService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 15/01 | QuestCompleteListResponse | QuestManager | real |  |
| 15/02 | QuestListResponse | QuestManager | real |  |
| 15/03 | QuestAcceptResponse | QuestManager | real |  |
| 15/05 | QuestEpisodeCompleteResponse | QuestManager | real |  |
| 15/06 | QuestGiveUpResponse | QuestManager | real |  |
| 15/07 | QuestUpdateResponse | QuestManager | real |  |
| 15/08 | QuestEventUpdateResponse | - | not sent |  |
| 15/09 | QuestHelperResponse | QuestManager | real |  |
| 15/10 | QuestResetResponse | QuestManager | real |  |
| 15/11 | QuestFailResponse | QuestManager | real |  |
| 15/12 | QuestEpisodeResetResponse | QuestManager | real |  |

#### 17 Monster

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 17/41 | DefensiveWeaponStartReq | - | - | ignored | no type: defensive weapons |
| 17/43 | DefensiveWeaponEndReq | - | - | ignored | no type: defensive weapons |
| 17/45 | DefensiveWeaponAttackReq | - | - | ignored | no type: defensive weapons |
| 17/52 | ControlMonsterAttackReq | - | - | ignored | no type: controlled monsters |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 17/11 | MonsterDieResponse | HelperSquad, LuaMaze | real |  |
| 17/12 | MonsterUpdateStatListResponse | MazeBuffs, MonsterBody | real |  |
| 17/22 | MonsterTargetChangeResponse | MonsterBody | real |  |
| 17/31 | Unknown31 | - | not sent |  |
| 17/32 | MonsterInvisibleResponse | - | not sent |  |
| 17/33 | MonsterChangeMotionResponse | - | not sent |  |
| 17/34 | MonsterEscapeDamageResponse | - | not sent |  |
| 17/42 | DefensiveWeaponStartBt | - | not sent |  |
| 17/44 | DefensiveWeaponEndBt | - | not sent |  |
| 17/51 | ControlMonsterBt | - | not sent |  |
| 17/53 | MonsterSuperArmorGageResponse | LuaCreatureManager | real |  |
| 17/54 | MonsterPartsHpResponse | - | not sent |  |

#### 18 Item setup

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 18/01 | ItemSetupMakeRequest | District | ItemCraftService | real | NPC not checked: in progress (feature/npc-services) |
| 18/02 | ItemSetupUpgradeRequest | District | ItemSetupService | real |  |
| 18/03 | ItemSetupExchangeRequest | District | ItemCraftService | real | NPC not checked: in progress (feature/npc-services) |
| 18/04 | ItemSetupDisassembleRequest | District | ItemCraftService | real | NPC not checked: in progress (feature/npc-services) |
| 18/05 | ItemSetupSocketEquipRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/06 | ItemSetupSocketActiveRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/08 | ItemSetupRepairRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/09 | ItemSetupRepairNpcRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/10 | ItemSetupRepairEquipRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/11 | ItemSetupRepairAllRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/13 | ItemSetupEvolutionRequest | District | ItemCraftService | real | NPC not checked: in progress (feature/npc-services) |
| 18/14 | ItemSetupAkashicMakeRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/15 | ItemSetupAkashicDisassembleRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/17 | ItemSetupAkashicComposeRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/18 | ItemSetupDisassembleExRequest | District | ItemCraftService | real | NPC not checked: in progress (feature/npc-services) |
| 18/20 | ItemSetupBroachEquipRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/21 | ItemSetupBroachActiveRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/22 | ItemSetupRestoreRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/23 | ItemSetupBroachComposeRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/24 | ItemSetupUnsealRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/25 | ItemSetupAkashicMakeExRequest | District | ItemSetupService | real | NPC not checked: in progress (feature/npc-services) |
| 18/26 | ItemSetupUseEffectRequest | District | ItemSetupService | real | its answer 18/26 has no receiver in the client (the group switch ends at 0x24) |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 18/01 | ItemSetupMakeResponse | ItemMakeService | real |  |
| 18/02 | ItemSetupUpgradeResponse | ItemUpgradeService | real |  |
| 18/03 | ItemSetupExchangeResponse | ItemExchangeService | real |  |
| 18/04 | ItemSetupDisassembleResponse | ItemDisassembleService | real |  |
| 18/05 | ItemSetupSocketEquipResponse | ItemSocketService | real |  |
| 18/06 | ItemSetupSocketActiveResponse | ItemSocketService | real |  |
| 18/08 | ItemSetupRepairResponse | ItemRepairService | real |  |
| 18/09 | ItemSetupRepairNpcResponse | ItemRepairService | real |  |
| 18/10 | ItemSetupRepairEquipResponse | ItemRepairService | real |  |
| 18/11 | ItemSetupRepairAllResponse | ItemRepairService | real |  |
| 18/12 | ItemSetupEnduranceResponse | - | not sent | endurance: in progress (feature/equipment-stats) |
| 18/13 | ItemSetupEvolutionResponse | ItemEvolutionService | real |  |
| 18/14 | ItemSetupAkashicMakeResponse | AkashicService | real |  |
| 18/15 | ItemSetupAkashicDisassembleResponse | AkashicService | real |  |
| 18/17 | ItemSetupAkashicComposeResponse | AkashicService | real |  |
| 18/18 | ItemSetupDisassembleExResponse | ItemDisassembleService | real |  |
| 18/20 | ItemSetupBroachEquipResponse | ItemBroachService | real |  |
| 18/21 | ItemSetupBroachActiveResponse | ItemBroachService | real |  |
| 18/22 | ItemSetupRestoreResponse | ItemRestoreService | real |  |
| 18/23 | ItemSetupBroachComposeResponse | ItemBroachService | real |  |
| 18/24 | ItemSetupUnsealResponse | ItemRestoreService | real |  |

#### 19 Friend

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 19/01 | FriendLoadRequest | District,Maze | FriendService | real |  |
| 19/02 | FriendLoadBlocklistRequest | District,Maze | FriendService | real |  |
| 19/11 | FriendInviteRequest | District,Maze | FriendService | real |  |
| 19/13 | FriendInviteAcceptRequest | District,Maze | FriendService | real |  |
| 19/15 | FriendDeleteRequest | District,Maze | FriendService | real |  |
| 19/21 | FriendBlockAddRequest | District,Maze | FriendService | real |  |
| 19/22 | FriendBlockDelRequest | District,Maze | FriendService | real |  |
| 19/32 | FriendInfoRequest | District,Maze | FriendService | real |  |
| 19/33 | FriendFindRequest | District,Maze | FriendService | real |  |
| 19/41 | FriendRecruitListRequest | District,Maze | FriendService | real |  |
| 19/42 | FriendRecruitAddRequest | District,Maze | FriendService | real |  |
| 19/43 | FriendRecruitDelRequest | District,Maze | FriendService | real |  |
| 19/44 | FriendRecruitInfoRequest | District,Maze | FriendService | real |  |
| 19/51 | FriendRecommendListRequest | District,Maze | FriendService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 19/01 | FriendLoadResponse | FriendServiceBase | real |  |
| 19/02 | FriendLoadBlocklistResponse | FriendServiceBase | real |  |
| 19/11 | FriendInviteResponse | FriendServiceBase | real |  |
| 19/12 | FriendInviteReqResponse | FriendServiceBase | real |  |
| 19/13 | FriendInviteAcceptResponse | FriendServiceBase | real |  |
| 19/14 | FriendAddResponse | FriendServiceBase | real |  |
| 19/15 | FriendDeleteResponse | FriendServiceBase | real |  |
| 19/21 | FriendBlockAddResponse | FriendServiceBase | real |  |
| 19/22 | FriendBlockDelResponse | FriendServiceBase | real |  |
| 19/31 | FriendUpdateInfoResponse | FriendNotifier, FriendServiceBase | real |  |
| 19/32 | FriendInfoResponse | OtherPersonService | real |  |
| 19/33 | FriendFindResponse | FriendServiceBase | real |  |
| 19/41 | FriendRecruitListResponse | FriendServiceBase | real |  |
| 19/42 | FriendRecruitAddResponse | FriendServiceBase | real |  |
| 19/43 | FriendRecruitDelResponse | FriendServiceBase | real |  |
| 19/44 | FriendRecruitInfoResponse | FriendServiceBase | real |  |
| 19/51 | FriendRecommendListResponse | FriendServiceBase | real |  |

#### 20 Post

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 20/01 | PostSendListRequest | District | PostService | real |  |
| 20/02 | PostRecvListRequest | District | PostService | real |  |
| 20/03 | PostSendRequest | District | PostService | real |  |
| 20/04 | PostReadRequest | District | PostService | real |  |
| 20/05 | PostReceiptRequest | District | PostService | real |  |
| 20/06 | PostSendDelRequest | District | PostService | real |  |
| 20/07 | PostRecvDelRequest | District | PostService | real |  |
| 20/08 | PostSendBackRequest | District | PostService | real |  |
| 20/10 | PostInfoRequest | District | PostService | real |  |
| 20/12 | PostSaveRequest | District | PostService | real |  |
| 20/13 | PostSaveListRequest | District | PostService | real |  |
| 20/14 | PostAccountListRequest | District | PostService | real |  |
| 20/16 | PostAccountReadRequest | District | PostService | real |  |
| 20/17 | PostAccountReceiptRequest | District | PostService | real |  |
| 20/18 | PostAccountDelRequest | District | PostService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 20/01 | PostSendListResponse | PostService | real |  |
| 20/02 | PostRecvListResponse | PostService | real |  |
| 20/03 | PostSendResponse | PostService | real |  |
| 20/04 | PostReadResponse | PostService | real |  |
| 20/05 | PostReceiptResponse | PostService | real |  |
| 20/06 | PostSendDelResponse | PostService | real |  |
| 20/07 | PostRecvDelResponse | PostService | real |  |
| 20/08 | PostSendBackResponse | PostService | real |  |
| 20/09 | PostRecvResponse | PostService | real |  |
| 20/10 | PostInfoResponse | PostService | real |  |
| 20/12 | PostSaveResponse | PostService | real |  |
| 20/13 | PostSaveListResponse | PostService | real |  |
| 20/14 | PostAccountListResponse | PostService | real |  |
| 20/15 | PostAccountRecvResponse | MailCommand | real |  |
| 20/16 | PostAccountReadResponse | PostService | real |  |
| 20/17 | PostAccountReceiptResponse | PostService | real |  |
| 20/18 | PostAccountDelResponse | PostService | real |  |

#### 21 Soul metry

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 21/05 | SoulCompleteRequest | District | SoulMetryService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 21/01 | SoulMetryListResponse | PersonService, SoulMetryManager | real |  |
| 21/02 | SoulMetryCompleteListResponse | SoulMetryManager | real |  |
| 21/03 | SoulMetryAddResponse | SoulMetryManager | real |  |
| 21/04 | SoulMetryUpdateResponse | SoulMetryManager | real |  |
| 21/05 | SoulMetryCompleteResponse | SoulMetryManager | real |  |
| 21/06 | SoulMetryResetResponse | SoulMetryManager | real |  |

#### 22 League

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 22/01 | LeagueCreateRequest | District,Maze | LeagueService | real |  |
| 22/02 | LeagueDeleteRequest | District,Maze | LeagueService | real |  |
| 22/03 | LeagueListRequest | District,Maze | LeagueService | real |  |
| 22/05 | LeagueApplicantRequest | District,Maze | LeagueService | real |  |
| 22/08 | LeagueBoardRequest | District,Maze | LeagueService | real |  |
| 22/09 | LeagueWithdrawRequest | District,Maze | LeagueService | real |  |
| 22/0F | LeagueKickRequest | District,Maze | LeagueService | real |  |
| 22/13 | LeagueInviteRequest | District,Maze | LeagueService | real |  |
| 22/14 | LeagueInviteAcceptRequest | District,Maze | LeagueService | real |  |
| 22/15 | LeagueInviteRejectRequest | District,Maze | LeagueService | real |  |
| 22/18 | LeagueApplicantAcceptRequest | District,Maze | LeagueService | real |  |
| 22/19 | LeagueApplicantRejectRequest | District,Maze | LeagueService | real |  |
| 22/20 | LeagueSearchRequest | District,Maze | LeagueService | real |  |
| 22/21 | LeagueOverlapNameRequest | District,Maze | LeagueService | real |  |
| 22/29 | LeagueNoticeChangeRequest | District,Maze | LeagueService | real |  |
| 22/32 | LeagueAuthChangeRequest | District,Maze | LeagueService | real |  |
| 22/33 | LeaguePositionNameChangeRequest | District,Maze | LeagueService | real |  |
| 22/39 | LeagueMemberPositionChangeRequest | District,Maze | LeagueService | real |  |
| 22/46 | LeagueOpenOrNotRequest | District,Maze | LeagueService | real |  |
| 22/47 | LeagueRecruitNoticeRequest | District,Maze | LeagueService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 22/01 | LeagueCreateResponse | LeagueServiceBase | real |  |
| 22/02 | LeagueDeleteResponse | LeagueServiceBase | real |  |
| 22/03 | LeagueListResponse | LeagueServiceBase | real |  |
| 22/07 | LeagueInfoResponse | LeagueNotifier | real |  |
| 22/08 | LeagueBoardResponse | LeagueServiceBase | real |  |
| 22/09 | LeagueWithdrawResponse | LeagueServiceBase | real |  |
| 22/0A | LeagueMasterChangeResponse | - | not sent |  |
| 22/0F | LeagueKickResponse | LeagueServiceBase | real |  |
| 22/13 | LeagueInviteResponse | LeagueServiceBase | real |  |
| 22/14 | LeagueInviteAcceptResponse | LeagueServiceBase | real |  |
| 22/15 | LeagueInviteRejectResponse | LeagueServiceBase | real |  |
| 22/17 | LeagueJoinUserResponse | LeagueServiceBase | real |  |
| 22/20 | LeagueSearchResponse | LeagueServiceBase | real |  |
| 22/21 | LeagueOverlapNameResponse | LeagueServiceBase | real |  |
| 22/22 | LeagueMemberUpdateResponse | LeagueNotifier | real |  |
| 22/23 | LeagueApplicantResResponse | LeagueServiceBase | real |  |
| 22/24 | LeagueApplicantAddResponse | LeagueServiceBase | real |  |
| 22/25 | LeagueApplicantRejectResResponse | LeagueServiceBase | real |  |
| 22/26 | LeagueApplicantDeleteResponse | LeagueServiceBase | real |  |
| 22/27 | LeagueWithdrawResResponse | LeagueServiceBase | real |  |
| 22/28 | LeagueKickResResponse | LeagueServiceBase | real |  |
| 22/29 | LeagueNoticeChangeResponse | LeagueServiceBase | real |  |
| 22/32 | LeagueAuthChangeResponse | LeagueServiceBase | real |  |
| 22/33 | LeaguePositionNameChangeResponse | LeagueServiceBase | real |  |
| 22/34 | LeaguePositionNameChangeResResponse | LeagueServiceBase | real |  |
| 22/35 | LeagueAuthChangeResResponse | LeagueServiceBase | real |  |
| 22/36 | LeagueNoticeChangeResResponse | LeagueServiceBase | real |  |
| 22/37 | LeagueInviteResResponse | LeagueServiceBase | real |  |
| 22/38 | LeagueBoardResResponse | LeagueServiceBase | real |  |
| 22/39 | LeagueMemberPositionChangeResponse | LeagueServiceBase | real |  |
| 22/41 | LeagueMemberLeaveResponse | LeagueServiceBase | real |  |
| 22/43 | LeagueInfoUpdateResponse | LeagueMapper | real |  |
| 22/44 | LeagueInfoAlertResponse | LeagueNotifier | real |  |
| 22/45 | LeagueCreateResResponse | LeagueServiceBase | real |  |
| 22/46 | LeagueOpenOrNotResponse | LeagueServiceBase | real |  |
| 22/47 | LeagueRecruitNoticeResponse | LeagueServiceBase | real |  |

#### 23 Gesture

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 23/01 | GestureShowRequest | District,Maze | GestureService | partial | district and maze; any gesture of the table, owned or not |
| 23/03 | GestureSlotUpdateRequest | District,Maze | GestureService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 23/01 | GestureShowResponse | GestureService | real |  |
| 23/02 | GestureSlotLoadResponse | GestureService, PersonService | real |  |

#### 24 Daily mission

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 24/02 | DailyMissionAcceptRequest | District,Maze | DailyMissionService | real |  |
| 24/04 | DailyMissionHelperRequest | District,Maze | DailyMissionService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 24/01 | DailyMissionListResponse | DailyMissionManager | real |  |
| 24/02 | DailyMissionAcceptResponse | DailyMissionManager | real |  |
| 24/03 | DailyMissionUpdateResponse | DailyMissionManager | real |  |
| 24/04 | DailyMissionHelperResponse | DailyMissionManager | real |  |

#### 25 Vacuum

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 25/01 | VaccumClickStartRequest | Maze | VaccumService | partial | the cube is taken but gives nothing (no reward table behind `NoVaccumRewards`) |
| 25/02 | VaccumClickCancelRequest | Maze | VaccumService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 25/01 | VaccumClickStartResponse | VaccumManager | real |  |
| 25/02 | VaccumClickCancelResponse | VaccumManager | real |  |
| 25/03 | VaccumClickCompleteResponse | VaccumManager | real |  |
| 25/11 | VaccumInResponse | VaccumManager | real |  |
| 25/12 | VaccumOutResponse | VaccumManager | real |  |
| 25/13 | VaccumInfosResponse | VaccumManager | real |  |

#### 26 My room

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 26/05 | EditStart | - | - | ignored | no type: my room |
| 26/06 | EditEnd | - | - | ignored | no type: my room |
| 26/10 | CreateReq | - | - | ignored | no type: my room |
| 26/11 | EnterReq | - | - | ignored | no type: my room |
| 26/13 | ExitReq | - | - | ignored | no type: my room |
| 26/14 | Setup | - | - | ignored | no type: my room |
| 26/15 | KickOut | - | - | ignored | no type: my room |
| 26/20 | ItemAdd | - | - | ignored | no type: my room |
| 26/21 | ItemDel | - | - | ignored | no type: my room |
| 26/22 | ItemUse | - | - | ignored | no type: my room |
| 26/23 | ItemReleaseUsed | - | - | ignored | no type: my room |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 26/01 | Load | - | not sent |  |
| 26/05 | EditStart | - | not sent |  |
| 26/06 | EditEnd | - | not sent |  |
| 26/10 | CreateReq | - | not sent |  |
| 26/11 | EnterReq | - | not sent |  |
| 26/12 | LoadIndex | - | not sent |  |
| 26/14 | Setup | - | not sent |  |
| 26/20 | ItemAdd | - | not sent |  |
| 26/21 | ItemDel | - | not sent |  |
| 26/22 | ItemUse | - | not sent |  |
| 26/23 | ItemReleaseUsed | - | not sent |  |
| 26/24 | ItemUsedUsers | - | not sent |  |
| 26/25 | OutReason | - | not sent |  |

#### 27 Helper

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 27/02 | HelperSummonRequest | District,Maze | HelperService | real |  |
| 27/05 | HelperSupportInfoRequest | District,Maze | HelperService | real |  |
| 27/06 | HelperSupportRegisterRequest | District,Maze | HelperService | real |  |
| 27/07 | HelperSupportRewardRequest | District,Maze | HelperService | real |  |
| 27/08 | HelperSupportListRequest | District,Maze | HelperService | real |  |
| 27/09 | HelperSupportEquipRequest | District,Maze | HelperService | real |  |
| 27/10 | HelperEquipRequest | District,Maze | HelperService | real |  |
| 27/12 | HelperChangeOrderRequest | District,Maze | HelperService | real |  |
| 27/14 | HelperRecallRequest | District,Maze | HelperService | real |  |
| 27/15 | HelperAutoSummonRequest | District,Maze | HelperService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 27/01 | HelperListLoadResponse | HelperManager | real |  |
| 27/02 | HelperSummonResponse | HelperSquad | real |  |
| 27/03 | HelperAddResponse | HelperManager | real |  |
| 27/04 | HelperUserStatUpdateResponse | HelperManager | real |  |
| 27/05 | HelperSupportInfoResponse | HelperSupports | real |  |
| 27/06 | HelperSupportRegisterResponse | HelperSupports | real |  |
| 27/07 | HelperSupportRewardResponse | HelperSupports | real |  |
| 27/08 | HelperSupportListResponse | HelperSupports | real |  |
| 27/09 | HelperSupportEquipResponse | HelperSupports | real |  |
| 27/10 | HelperEquipResponse | HelperGear | real |  |
| 27/11 | HelperUpdateInfoResponse | HelperGear | real |  |
| 27/12 | HelperChangeOrderResponse | HelperManager | real |  |
| 27/13 | HelperSyncPosResponse | HelperBody | real |  |
| 27/15 | HelperAutoSummonResponse | HelperManager | real |  |

#### 28 Infinite tower

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 28/02 | EnterChapter | - | - | ignored | no type: infinite tower |
| 28/03 | EnterNextStage | - | - | ignored | no type: infinite tower |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 28/01 | LoadInfo | - | not sent |  |
| 28/04 | Reward | - | not sent |  |

#### 29 Booster

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 29/01 | BoosterLoadResponse | BoosterManager | real |  |
| 29/02 | BoosterAddResponse | BoosterManager | real |  |
| 29/03 | BoosterRemoveResponse | BoosterManager | real |  |
| 29/04 | BoosterClearResponse | BoosterManager | real |  |

#### 2A Event

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 2A/02 | EventAttendanceRewardRequest | District | EventService | real |  |
| 2A/05 | EventAttendancePlayTimeRewardRequest | District | EventService | real |  |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 2A/01 | EventAttendanceLoadResponse | AttendanceManager | real |  |
| 2A/02 | EventAttendanceRewardResponse | AttendanceManager | real |  |
| 2A/03 | EventAttendanceNoticeResponse | - | not sent |  |
| 2A/04 | EventAttendancePlayTimeInitResponse | AttendanceManager | real |  |
| 2A/05 | EventAttendancePlayTimeRewardResponse | AttendanceManager | real |  |
| 2A/20 | EventLuaValuesResponse | - | not sent |  |

#### 2B Exchange

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 2B/01 | Search | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/02 | PriceHistory | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/03 | InterestList | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/04 | InterestItem | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/05 | WaitRegister | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/06 | SellRegister | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/07 | ItemBuy | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/08 | ItemRecall | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |
| 2B/09 | MyList | - | - | ignored | no type: exchange; 43/03 and 43/09 are sent at every entry |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 2B/01 | Search | - | not sent |  |
| 2B/02 | PriceHistory | - | not sent |  |
| 2B/03 | InterestList | - | not sent |  |
| 2B/04 | InterestItem | - | not sent |  |
| 2B/05 | WaitRegister | - | not sent |  |
| 2B/06 | SellRegister | - | not sent |  |
| 2B/07 | ItemBuy | - | not sent |  |
| 2B/08 | ItemRecall | - | not sent |  |
| 2B/09 | MyList | - | not sent |  |

#### 2C Ranking

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 2C/01 | RankingTimeAttackListRequest | District | RankingService | partial | nothing ever records a time, so the list is empty |
| 2C/02 | RankingPvpListRequest | District | RankingService | partial | nothing ever records a kill, so the list is empty |
| 2C/03 | RankingInfiniteTowerListRequest | District | RankingService | invented | zeros for the chapter asked |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 2C/01 | RankingTimeAttackListResponse | RankingService | partial | empty: nothing records |
| 2C/02 | RankingPvpListResponse | RankingService | partial | empty: nothing records |
| 2C/03 | RankingInfiniteTowerListResponse | RankingService | invented | zeros |

#### 2D Social item

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| 2D/01 | Start | - | - | ignored | no type: social items |
| 2D/02 | Use | - | - | ignored | no type: social items |
| 2D/03 | Stop | - | - | ignored | no type: social items |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| 2D/01 | Start | - | not sent |  |
| 2D/02 | Use | - | not sent |  |
| 2D/03 | Stop | - | not sent |  |
| 2D/04 | End | - | not sent |  |
| 2D/05 | In | - | not sent |  |
| 2D/06 | Infos | - | not sent |  |
| 2D/07 | Out | - | not sent |  |

#### F1 Channel

| Request | Name | Servers | Handler | Verdict | Note |
|---|---|---|---|---|---|
| F1/01 | ChannelInfoRequest | District | ChannelService | real |  |
| F1/02 | ChannelChangeRequest | District | ChannelService | real | answered with the map of the new channel; 51008 for the same or a full channel, 50004 for a missing one, 55003 in a trade |

| Notice | Name | Built in | Verdict | Note |
|---|---|---|---|---|
| F1/01 | ChannelInfoResponse | ServerChannels | real |  |
| F1/02 | ChannelChangeResponse | ServerChannels | real |  |

#### Request types this client never sends

05/17 MovementTransportOffRequest, 05/21 MovementRotationRequest, 05/50 MovementJumpQuickDownRequest, 18/07 ItemSetupSocketDetachRequest, 18/12 ItemSetupEnduranceRequest, 18/16 ItemSetupUpgradeLimitRequest, 18/27 ItemSetupRenovateRequest, 18/28 ItemSetupBroachRemoveRequest, 18/29 ItemSetupRefineRequest, 18/30 ItemSetupSocketExchangeRequest, 18/31 ItemSetupSocketUpgradeRequest, 18/32 ItemSetupSocketExtractRequest, 18/33 ItemSetupAkashicComposeExRequest, 18/34 ItemSetupAkashicGetInfoAddRequest, 18/35 ItemSetupAkashicGetInfoLoadRequest, 18/36 ItemSetupDyeRequest, 18/37 ItemSetupTitleChangeRequest, 18/38 ItemSetupRenovateCompleteRequest, 18/39 ItemSetupBroachRemoveExRequest.
