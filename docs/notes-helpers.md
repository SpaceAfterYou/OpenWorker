# Helpers (supporters): what they are, the messages, the rules, the plan

Research notes for the feature the game calls *supporters* (サポーター; group 0x27, "Helper" in the code). The client 1.4.2.3
decided every layout below (its senders, receivers and the readers they call were read); the reference server gave the
rules, the order of the checks and the error codes. Each fact is marked: **[client]** read in the client, **[ref]** read in
the reference server, **[table]** read in the tables, **[inferred]** not read anywhere but concluded. The evidence (where
each fact was read) is in the git-ignored `docs/notes-helpers-research.md` of the main checkout; the scripts and exports are
in `docs/agents/research-helpers/`.

## 1. What a helper is

There is **one kind of helper**: an NPC of the story that a person gets for finishing a quest and that fights beside it in
mazes. "Support" is not a second kind of bot: it is an **orb** a person offers its friends, which a friend puts on one of its
own helpers to make it stronger (section 5). Nobody calls another person's character. **[client][ref]**

The scripts `Scripts/AI/Ai_PC_*` have nothing to do with helpers: they drive three "PC-like" monsters of an arena mode
(tb_Monster 1010201..1010203, their own type and faction). **[table]**

The eight helpers (`tb_Helper`, the id is a `tb_Monster` id; `Number` is the "class" their items are limited to) **[table]**:

| Helper id | Number | Who (action file) | Type text (UI string) | AI script | Level of the monster row | Status id |
|---|---|---|---|---|---|---|
| 10105166 | 100 | Mirium | 21500 attack and recovery | `Ai_NPC_0001_Mirium` | 11 | 3001 |
| 11000007 | 101 | Ryu | 21501 ranged attack | `Ai_NPC_0102_Ryu` | 80 | 3001 |
| 10105221 | 102 | Hidinger | 21502 charge and status | `Ai_NPC_0020_Hidinger` | 11 | 3002 |
| 10105162 | 103 | Ralfh | 21503 ranged support fire | `Ai_NPC_0009_Ralfh` | 11 | 3001 |
| 10105167 | 105 | Keinbarrel | 21505 melee area | `Ai_NPC_0302_Keinbarrel` | 11 | 3002 |
| 11000009 | 106 | Punisher Zenis | 21506 melee area | `Ai_MOB_0303_PunisherZenisCustom` | 80 | 3002 |
| 10105163 | 107 | Goodboy | 21507 support and buffs | `Ai_NPC_0021_Goodboy` | 11 | 3002 |
| 11000008 | 109 | Lucy | 21509 status and attack | `Ai_NPC_0427_Lucy` | 80 | 3001 |

All eight rows of `tb_Monster` have type 10 ("helper") and faction 10. Twelve more rows of type 10 (Mirium, Keinbarrel,
Hidinger at levels 29, 32, 47, 62) are story NPCs that fight beside the player in particular mazes; they are not in
`tb_Helper`, so the "far from the owner" warp (section 3) does not apply to them. **[table][ref]**

### Where the player meets them **[client]**

- **The supporter window** (`helper.gfx`, opened from the menu or the "Helper" key): the list of the person's helpers in
  their order, the details of the selected one (illustration, type text, the two stat lists, three equipment slots: weapon
  (slot type 240), gear (241), a third (242) for which the tables have no item), buttons: summon / release (21511, 21512),
  details (21513), change of order (21542), the automatic summon setting (21553), recall (21554), "friend equipment
  selection" (21531, the list of friends' orbs, 21537) with an equip button (21532).
- **The support registration window** (`registerhelper.gfx`): "attack orb, 20% of the character's attack" (21517, 21518)
  and "defense orb, 20% of the character's defense" (21519, 21520), what is registered and until when (21523..21526), the
  reward button (21515, 21550..21552).
- **In a maze**: the helper is an actor named `<helper name>(<owner name>)` at the owner's level, shown in the party panel
  as a member of type "helper"; a quick slot key summons (`SupporterQuick Key`), another releases (`SupporterRelease Key`).
  The context menu of a helper actor offers "release" (own helpers only) and "recall all".
- **Chat lines**: 21535 "{0} was released", 21534 "released because you left the maze", 21533 "{0}({1}) was released and
  disappeared; you can wear a new support equipment" (an orb used up), 22674 "summon cooldown ({0} s)".
- `leaguesupport.gfx` is not part of this feature (by its name it belongs to the league; not examined).

## 2. The messages

Sub-commands are hexadecimal (`27/10` is 0x10). Strings: u16 length then UTF-16 (the usual `WriteUtf16UnicodeString`).
Errors: `27/(sub|0x80)` + u16 code, as everywhere.

### The record of a helper (`HelperValue`), read by the client the same way in every message **[client][ref]**

| Field | Type | Meaning |
|---|---|---|
| helper | int | the helper id |
| items | 3 × item | the three equipment slots, the inventory item layout (82 bytes when empty: id -1, serial -1, ..., broach "000000000000000") |
| support friend | int | the friend whose orb the helper wears, 0 for none |
| support type | byte | 0 attack orb, 1 defense orb |
| support value | float | what the orb adds |
| support date | int64 | Unix seconds until which the orb was valid when worn |
| summoned | byte | the helper is in the maze now |
| origin stats | short n, n × (byte stat, float value) | the helper's own stats at the owner's level |
| additional stats | short n, n × (byte stat, float value) | what its items and its orb add |
| order | byte | its place in the list |

The client keeps origin and additional stats in arrays indexed by the stat number (61 entries), so a stat number must be
below 61. Stats used: 20 physical attack min, 21 physical attack max, 24 physical defense, plus whatever the origin list
carries. Empty size: 273 bytes (the current writer is right). The 27/04 receiver writes each stat into that 61-entry table
of the helper's record with no bound check, so a larger number overwrites the next fields. **[client]** The reference
server fills the origin list with all 77 stats of its own version, zeros included; this client cannot take that, so we send
the non-zero stats of the calculator, all below 45. **[ref][client]**

### Client → server

| Sub | Name | Layout | Sent when | Our declaration |
|---|---|---|---|---|
| 02 | summon | byte summon (1 call, 0 release), int helper | summon/release button, quick slot keys, context menu | right |
| 05 | support info | (empty) | the registration window opens, after a register or reward | right |
| 06 | support register | byte type (0 attack orb, 1 defense orb) | after the confirmation 21521/21522 | right |
| 07 | support reward | (empty) | after the confirmation 21550 | right |
| 08 | support list | (empty) | the supporter window opens, the refresh button | right |
| 09 | support equip | int person (own id), int helper (the selected one), int friend | equip button of the friend list | right |
| 10 | equip | byte inventory type, int item, short inventory slot, int64 inventory serial, int helper, int helper item, short helper slot, int64 helper serial (33 bytes) | dragging an item onto a slot, or off it | right |
| 12 | change order | int person (0), int first, byte 0, int second, byte 0, int 0 (18 bytes) | the up/down arrows: the selected helper and the one at the place it moves to | right |
| 14 | recall | (empty) | recall button / context menu; the client allows it once per 10 s | right (renamed `HelperRecallRequest`, opcode `Recall`) |
| 15 | auto summon | int person (own id), byte flag | the auto summon check box, only when the value changes | right |

The client sends 02, 06, 07, 08, 09, 10 through a "one request at a time" guard: a second one is not sent while the first
is unanswered. **[client]**

The equip request has three shapes **[client][ref]**: both serials set (swap: the inventory item goes on, the worn one goes
to the inventory slot named); only the inventory serial (put on: the server picks the helper slot from the item's slot
type); only the helper serial (take off: the client names an empty slot of inventory type 2; the reference server ignores
it and picks an empty slot of the item's own inventory).

### Server → client

| Sub | Name | Layout | What the client does | Our declaration |
|---|---|---|---|---|
| 01 | list load | int person, short n, n × helper, byte auto summon | replaces its list, refreshes the window | right |
| 02 | summon | int owner (person id), int actor (the helper's actor), byte summon, helper | summon=1: records "actor X is helper H of owner O at level L", names the actor `name(owner)`, sets its level to the owner's, adds it to the party panel; if the owner is the player: 10 s cooldown on every helper. summon=0: forgets the actor, chat line 21535; the owner's cooldown too | right |
| 03 | add | helper | adds it to the list, refreshes the window, a sound | right |
| 04 | user stat update | short n, n × (int helper, short m, m × (byte, float)) | replaces the **origin** stats of each named helper | right |
| 05 | support info | int friend (own id when registered), byte type, float value, int64 expiry, byte reward state, byte registered (19 bytes) | fills the registration window | right |
| 06 | support register | int result, support info | result 0: shows what was registered | right |
| 07 | support reward | int result | refreshes the window, chat line (UI 47) | right |
| 08 | support list | short n, n × friend record | fills the friend orb list | right (fixed, see below) |
| 09 | support equip | int result, int helper, helper | if the helper is the selected one, refreshes it | right (fixed: `Result`, `Helper`, `Record`) |
| 10 | equip | int result, helper | refreshes the helper's slots and stats | right (fixed: `Result`) |
| 11 | update info | helper | refreshes the helper; if it had an orb and the new record has none, chat line 21533 | right |
| 12 | change order | the 18 bytes of the request, with the orders and an error | error 0: applies both orders, refreshes the quick slots | right |
| 13 | sync pos | int actor, float x, y, z | moves that helper actor there at once (only a monster actor of type helper) | right |
| 15 | auto summon | int person, byte flag | stores the flag, refreshes the check box | right |

Friend record of 08 (client 1.4.2.3; the reference server writes two more fields, awakening and profile photo, that this
client does not read): `int friend, byte type, float value (scaled to the reader, see 5), int64 expiry, float original
value, string name (at most 20 characters), byte level, byte class`. `HelperSupportFriendValue` names them `OriginalValue`
and `Class` (they were `Rate` and `Hero`). **[client][ref]**

The reference server answers some requests with nothing at all (no answer, no error): 06 when a valid registration whose
reward was already taken exists, 07 when there is no reward to take, 09 when the friend's orb was already used by this
person, 10 when the database refused, 12 when either helper is not the person's, 15 never silent. **[ref]**

## 3. A helper in a maze, step by step

### Summon (27/02 with summon=1) **[ref]**, checked in this order

1. The person is dead: nothing at all.
2. Less than **10 seconds** since this person's last summon: error **58005** ("cooldown").
3. Not in a maze: error **58006** ("supporters cannot be summoned in town").
4. The maze refuses helpers (the flag of the maze table, see 6; or the maze is of type 12), or the person does not have the
   helper, or it is summoned already: error **58007** ("cannot summon in this maze").
5. The count is full: error **58008** ("cannot summon more [{0}/{1}]"). The rule: **a maze holds at most four bodies**:
   alone, the person plus 3 helpers (`helpers in the maze + 1 >= 4` refuses); in a party, `helpers in the maze + party
   members >= 4` refuses, counting the helpers of every member. A party of 4 cannot summon.
6. Creating the actor fails: 58007.

Then **[ref]**:

- the helper becomes a **monster** of the maze (its `tb_Monster` row), created at the owner's position moved 100 units
  behind it along its facing (the point is kept only if the ground there is found within 200 units of height, else the
  owner's own position), facing where the owner moves, in the owner's sector, its parent and owner the owner's actor. The
  creation sends the usual monster spawn to the maze (`STMonsterInfo` with `uxParentActorID` = the owner's actor; the
  client handles a helper the same way in `04/15` "in info" and `04/23` "other infos", which one the reference uses for a
  single new monster was not read; our creature manager sends `04/23`);
- its stats are set (section 4), its health set to the new maximum;
- it is counted in the maze's list of helpers and in the owner's list of summoned helpers; the summon time is noted;
- `27/02` {owner person id, helper actor, 1, the helper record with `summoned` = 1} goes **to the party** if the owner is
  in one, else to the owner.

The client needs the spawn first (it renames the actor and adds it to the party panel when 27/02 finds the actor, and does
the same when a spawn finds an actor 27/02 already announced, so either order works). **[client]**

### What it is in the world **[ref][client]**

A monster whose table type is 10. The client recognises a helper by that type alone (the monster row), not by any flag of
the spawn. The server treats types 10 (helper) and 8 (mercenary) as *followers*: they have an owner player, they never die
(see damage), monsters of rank 3 or more do not pick them as targets, they count as members of the owner's party for "is
an ally" checks, and their kills are the owner's.

### Stats **[ref][table]**

At summon (and whenever equipment or orb change):

- **origin stats**: the stat calculation of an NPC at the **owner's level**, from the helper's status row (`tb_Status`, the
  monster row's status id: 3001 or 3002) and the monster row's own status type, plus the flat additions of its `tb_Monster`
  row (health, attacks, defenses, accuracy, critical, ... the `Add*` columns). Our `CombatStatCalculator.Calculate(status,
  level, statusType, StatBonuses.From(monsterRow))` is that calculation. The monster row's own level (11 or 80) is not used.
  The owner's class is passed to the calculation but **not used** by it: the "ranged" test reads the status type of the
  monster row, not the class (checked in the reference, see the evidence file). In the client tables all eight helper rows
  have no `Add*` values, so today the origin stats come from the status row alone.
- **additional stats**: for each of the three worn items, its physical attack added to stats 20 and 21 and its physical
  defense to stat 24; then the orb: attack orb adds its value to 20 and 21, defense orb to 24, only while the orb's date is
  in the future.
- origin goes to the record's first list, additional to the second; the actor's stats are origin + additional.
- `27/04` is sent when the owner levels up, with every helper's origin stats. The reference sends the origin lists it
  holds, computed when the list was loaded (or at the last summon or equipment change), so they are those of the old level,
  and it broadcasts them around the owner, whose clients then overwrite the stats of their own helpers of the same ids.
  Both look like defects: we compute the lists at the new level and send them to the owner only.

### The AI **[ref][table]**

The helper runs the AI script of its monster row (table above), the same machine as monsters. What the eight scripts use
(counted over the eight files): `RegisterSkillConditions` 124, `RegisterStateVars` 100, `RegisterConditions` 87,
`SetSkillGroupInfo` 45, `RegisterStateFunctions` 37, `RegisterActionAfterSkill` 20, and once per script `SetAiCheckTime`,
`SetFindEnemyInfo` (first attacker, period 1 s (0.2 s for Keinbarrel), sight 1000 (8000 for Zenis)), `SetDmgAggressive`,
`SetDmgAggroReset`, **`SetRecoverySkill(50000)`**, **`SetEscapeInfo(...)`**, **`SetHelperFarDist(3500, 9000)`**.
Variables: `RANDOM` 173, `TARGETDISTANCE` 20, **`GUARD_HEALTH`** 10, **`GUARD_DISTANCE`** 8, `ENEMY_COUNT` 7, `SPAWN_TIME` 7,
`GROUP_COOLTIME` 3, `TARGETDISTANCE_CAPSULE` 2, `SPAWN_MONSTER_COUNT` 1. States: WAIT, MOVE, PATROL, SELECT_ACTION, ATTACK,
BATTLE_MOVE, ATTACK_SKILL1..9.

What is particular to a follower **[ref]**:

- **Guard variables** read the **owner**: `GUARD_DISTANCE` = distance to the owner, `GUARD_DISTANCE_CAPSULE` the same minus
  the owner's radius, `GUARD_HEALTH` = the owner's health in percent (100 without an owner). The scripts use them to
  follow (`WAIT → MOVE` when the owner is farther than 200..350) and to choose skills by the owner's health (Mirium, Lucy:
  healing skills when the owner is below 41 %).
- **MOVE** goes to the **owner** (not to a target), with the state's distance and angle ranges.
- **Too far from the owner** (only the eight real helpers): in MOVE, TRACE_MOVE and at the start of ATTACK (and while
  attacking), if the distance to the owner is over the far distance (3500 out of battle, 9000 in battle; after ten failed
  "forced" checks the limit drops to 500) and the ground at the owner is found, the helper **teleports to the owner's
  position**, forgets its target, goes to SELECT_ACTION, and `27/13` {actor, owner's x, y, z} is broadcast around it.
- **Recall** (`27/14`): every summoned helper of the person teleports to the person the same way (27/13 each), if its
  warp is enabled (it is, from summon until release). Nothing else is answered.
- **Targets**: the enemy search takes the nearest live actor the helper may hit (the usual enemy rule by origin and
  faction: helpers are faction 10, enemies of monsters), within its sight. Skills whose target type is not "enemy" (buffs,
  heals) target the **owner**.
- **Damage**: when a follower's health would reach 0 it stays at **1** and enters **RECOVERY**: target forgotten, buffs
  cleared, stops, invincible, defense type 3, plays its recovery skill (50000), and heals **5 % of its maximum health every
  second** until full, then SELECT_ACTION. A follower never dies by damage; only release kills it (below).
- **Kills**: a monster killed by a helper counts as killed by the owner: drops, experience, quest kills, the title of
  kills, the killer id of the death message.

### Release **[ref]**

- `27/02` with summon=0: dead person: nothing; not in a maze: **58006**; not summoned (or not the person's): **58007**.
- Then: warp disabled; the record's `summoned` = 0; **its orb is released** (the database drops it, then `27/11` with the
  record without orb; the client tells 21533); the actor is removed from the maze by a death with a special reason (no
  drop, no reward: the killer is nobody); `27/02` {owner, actor, 0, record} to the maze's party, or to the owner.
- The client asks for a confirmation (21546) before releasing a helper that wears an orb. **[client]**
- Every helper of a person is released when the person **dies**, **leaves the maze** (exit, portal to a district, maze
  exit request), or enters another world. On leaving, the person is also sent `27/02` summon=0 for the helpers of the
  others in the maze, so its client forgets them.
- When a person **enters a maze** where party members have helpers, after the monster list it is sent `27/02` summon=1
  for each of them (so its client names them).
- When the maze is **cleared** (its reward step), every orb worn by a helper of anyone in the maze is released (27/11 each).

### Auto summon **[ref]**

When the person has finished loading a maze: if the flag is on, the person is **not in a party**, and the maze allows
helpers (same flag and type as above), the helpers are summoned in list order until the count is full. The 10 s delay is not
checked here. The flag is on by default for a new person (the reference server clears its helper state to "on"; the value
for a person without stored helpers comes from a stored procedure that was not read); ours is on for a new book too.

## 4. Growth and equipment

- A helper has **no level and no experience** of its own. Its strength follows the **owner's level and class** (origin
  stats) and its **equipment** and **orb** (additional stats). **[ref]**
- **Equipment**: 64 items in `tb_Item`, 8 per helper: 4 weapons (classify slot type 240: physical attack 157 / 315 / 472 /
  630, owner level 1 / 10 / 25 / 40) and 4 gears (slot type 241: physical defense 42 / 86 / 130 / 175, same levels). The
  item's class limit is the helper's `Number`. Slot type 242 exists (classify rows, "costume" names) but no item uses it.
  Helper slot = slot type − 240. **[table]**
- **Rules of 27/10** **[ref]**: the helper must be the person's and in `tb_Helper`; an item put on must be in the
  inventory, be of slot type 240..242, have the helper's number as class limit, and (swap) the worn item's serial must
  match the slot and both items be of the same slot type; a put-on goes only into an empty slot; take-off needs an empty slot
  in the item's inventory: **52010** if none. Any other failure: **58004**. The client also refuses on its side items of
  another class (52009) or above the owner's level. **[client]**
- Success: the items move (inventory item removed with the usual "item gone" message, the taken-off item created in the
  inventory with the usual "item created" message), the stats are recomputed (a summoned helper's actor too), and `27/10`
  {0, record} is sent. Equipping works **anywhere** (district or maze). **[ref]**
- `27/11` is sent when an orb is released (helper release, maze clear). `27/04` on level up.

## 5. Support (friend orbs)

**[ref]** unless marked.

- **Register (06)**: type 0 = attack orb, value = **20 %** of the person's physical attack (max), type 1 = defense orb,
  value = 20 % of its physical defense; valid **24 hours**. The registration is held by the cross-server part (one per
  person, in memory in the reference server, not in its database). If a still-valid registration exists: **58003** in the
  result of 06 when its reward is still to take, no answer when it was taken. On success `27/06` {0, info with registered =
  1 and reward state 1}.
- **Info (05)**: registered = 1 while valid (an expired registration is dropped on this request), the reward state
  (0 none, 1 to take, 2 taken), type, value, expiry.
- **Reward (07)**: only with reward state 1: the items of `tb_Helper_Reward` row 1 (item 800000999 × 1; friend points 0)
  are checked against the inventory (**52010** full, **52004** other failure), then the cross-server part sets the state to
  2, the items are created, the friend points added, `27/07` {0}. **[ref][table]**
- **List (08)**: the person's **friends** who have a valid registration and whose orb this person has **not used yet**
  (each orb can be used once per user for its 24 hours). The value shown is scaled when the friend is more than 5 levels
  above the reader: `value = original / friend level × reader level`. Neither strangers nor league members.
- **Equip (09)**: the helper must be the person's; the friend's registration must exist (result 2 if not) and still be
  valid (result 1 if expired: it is also dropped); the same scaling by level; then the database stores the orb on the
  helper, the use is recorded (the friend leaves this person's list), the friend whose orb was used gets the friend points
  of `tb_Helper_Reward` row 2 (0 in both copies of the table) with this person, and `27/09` {0, helper, record with the
  orb and recomputed stats}. The client sends 09 only for a friend marked usable in its list. **[client][ref]**
- **Lifetime of a worn orb**: until the helper is released or the maze is cleared (then 27/11 and chat 21533); outside
  that, the stats count it only while its date is in the future.

## 6. How a helper is obtained, and the tables

- **Quests**: the eight episodes 300005, 300010, 300015, 300020, 300025, 300030, 300035, 300040 (contents 3, "helper
  episodes") each give one helper through `tb_Quest_Episode.RewardHelper` (Mirium, Ralfh, Hidinger, Keinbarrel, Goodboy,
  Ryu, Zenis, Lucy). The server already grants it (`QuestManager` → `HelperManager.AddAsync` → `27/03`, achievement 48).
  **[table]** The quest "helper" of 15/09 and the daily mission "helper" are a different thing (the quest tracker).
- **Chat command** `/helper-add <id>` (testing). The reference server's own command (number, value) does: 1 add, 2 summon,
  3 release, 4 register, 5 reward, 6 list, 7 support equip, 8 equip.
- **Proposed column names** (do not edit `Structures.json` here; for the implementation agent):
  - `tb_Helper`: `Id` (helper = monster id), `Number` (the class number items are limited to), `Field2` → `TypeText` (UI
    string of the type), `Field3` → `Icon`, `Field4` → `Illustration`.
  - `tb_Helper_Reward`: `Id` (1 = registration reward, 2 = reward per use of the orb), `Field1` → `RewardType`, `Field2` →
    `FriendPoints`, `Field3..5` → `Item1..3`, `Field6..8` → `Count1..3`. The server's own copy is identical: no server row
    is needed.
  - `tb_Maze_Info`: `Field7` → `RefuseHelpers` (1 = helpers cannot be summoned). It matches the reference server's own
    flag on 198 of the 199 mazes of the client (it differs on 21051 `DR01_GOLDENCITADEL`, which the server refuses and the
    client allows); the client checks the same byte before sending a summon. **[client][table]**

## 7. What the server has and lacks

| Piece | State | What is missing / depends on |
|---|---|---|
| Storage of the list, order, auto flag (`HelperBook` in `PersonStates`) | done | — (auto flag on for a new book, step 2) |
| 27/01 list at district and maze entry | done | items are carried by the record but none is worn yet (step 4) |
| 27/03 add (quest reward, chat command) | done | — |
| 27/12 change order | done (district and maze) | — |
| 27/15 auto summon | done (district and maze) | flag > 1: the reference echoes the current flag |
| 27/02 summon outside a maze | done | 58006 for summon and release |
| 27/02 summon/release in a maze | missing | answered (58007 for both) by the shared handler; creature spawn with owner, limits, cooldown, maze flag — depends on the creature manager (`OpenWorker.Lua`) and the shared maze instance of `feature/party-maze` |
| 27/14 recall | missing | the AI warp; answered with nothing on both servers, as the reference does |
| 27/13 sync pos | missing | the AI warp |
| 27/04 stat update | done | sent to the owner at every level reached (`ExperienceService`) |
| Stats (origin / additional) | done | `HelperStats`: origin at the owner's level; additional from worn items and the orb, empty until they exist |
| 27/10 equipment | missing (refused 58004) | an item place for helper equipment (schema), `InventoryService` moves, stats |
| 27/11 update info | missing | orb release |
| AI of a follower | missing | owner-aware guard variables, MOVE to owner, warp, recovery state, buff target = owner, enemy search both ways (helpers hit monsters, monsters target helpers except rank ≥ 3), monster→monster damage — `feature/combat` and the AI machine |
| Kills credited to the owner | missing | the kill path of the creature manager |
| Release on death / exit / maze clear, others' helpers on join and leave | missing | maze entry/exit hooks, `feature/party-maze` |
| 27/05..09 support | partly (05, 06 answer without values; 07 and 09 refused; 08 always empty) | a registration store shared by all servers, friends (exist: `FriendBook`), friend points, item grant |

The e2e (`docs/tools/e2e_helper.py tester9 password [maze location]`, stand 9): before steps 1 to 3 **25 PASS, 0 FAIL**
(district only); after them **40 PASS, 0 FAIL** on a build of `feature/helpers-base` over `dev` `b6e00cb`: the record read
the way the client reads it, origin stats non-empty and below 61, 27/04 at a level reached (`/exp`, which now gives real
experience) with grown stats, the list again after re-entry with the new level's stats, and in a maze (M04, 21311) the list
with the same stats, 27/15 and 27/12 answered, a summon refused with 58007, a recall answered with nothing. It still asserts
today's refusals (equip, support equip, support reward), which steps 4 and 7 change.

## 8. Implementation plan

Order matters: 1–3 need no maze work and give the window its real content; 4–6 make helpers fight; 7 is independent.

1. **Protocol corrections** (S) — **done** (`feature/helpers-base`). `HelperSupportEquipResponse` is `Result`, `Helper`,
   `Record`; `HelperEquipResponse.Result`; `HelperSupportFriendValue.OriginalValue`, `Class`; 27/14 is
   `HelperRecallRequest` / `HelperOpcode.Recall`; `HelperValue.Items` carries up to three `ItemValue`s written by the
   inventory's item writer, `HelperValue.NoItem` is the empty slot (the same 82 bytes as before). Found on the way: a helper
   without an orb sent the orb date of `default(DateTimeOffset)` (a large negative number); it now sends 0. Test: one byte
   layout test of a record with a worn item.
2. **Handlers in both servers** (S) — **done**. `HelperService` is in `OpenWorker.Server.Common`. The research was wrong
   that the maze never loads the book: it did (`MiscStateLoader.EnterMazeAsync`, called per player by the maze's
   `PersonService`); the maze only lacked the handlers. The maze's load now goes through `HelperManager.EnterMazeAsync`,
   which marks the person as in a maze (`HelperComponent.InMaze`). Summon and release outside a maze → 58006; in a maze,
   until step 5, both → 58007 (the code the client shows as "cannot summon in this maze"; for a release it is also the
   reference's code for "not summoned", which is true). Recall answers nothing on both servers (the reference answers it
   with nothing but 27/13). Equip (58004), support equip (58004), support reward (52004) stay as they were. New book: auto
   flag on. Test: one unit test (every 0x27 request has its one handler in the shared assembly both servers reference; the
   codes in and outside a maze); e2e in a maze.
3. **Stats** (M) — **done**. `OpenWorker.Gameplay/Modules/Helpers/HelperStats`: origin = `CombatStatCalculator` with the
   helper's status row, the monster row's status type and `StatBonuses.From(monster row)` at the owner's level (the class is
   not used, see section 3), as `CombatStats.ToStatList()` (non-zero, stat numbers 4..44); additional = items (20, 21, 24)
   + a valid orb, both empty today. Filled into every record the manager sends (27/01, 27/03; 27/10 and 27/11 are not sent
   yet: they come with steps 4 and 7 and take the same `ToValue`); 27/04 from `ExperienceService` at a level reached, to the
   owner, with the new level's stats. `tb_Helper` columns named `TypeText`, `Icon`, `Illustration`. What our list lacks next
   to the reference's: stamina, soul gauge, move and attack speed and the attribute stats, which the calculator does not
   compute. Test: one unit test of the rule (the level raises the origin stats; the monster row's additions are in; items
   and a valid orb add to 20, 21, 24; an expired orb adds nothing).
4. **Equipment 27/10** (M–L). Helper equipment kept as items in a storage of their own: a storage number not used by the
   client for a helper's slots, slot = helper number × 3 + slot (or a storage per helper number 100..109, slot 0..2) —
   **schema**: no new column if the storage/slot encoding is used; the index `(PersonId, Storage, Slot)` should become
   unique for that storage (coordinator's migration). Moves through `InventoryService` / `Inventory.Touch` so the flush
   carries them. The three shapes of the request, the checks and codes of section 4. Tests: one unit test per shape is too
   many by rule 6 — one test of "put on, swap, take off keep one item per slot"; e2e with a granted helper item.
5. **Summon and release in the maze** (L). `OpenWorker.Gameplay/Modules/Helpers` (rules: cooldown 10 s, count of four
   bodies, maze flag, the order of checks and codes) + the maze side (`OpenWorker.MazeServer`/`OpenWorker.Lua`): spawn the
   monster row with an owner (a new owner component: owner entity, person, helper id), position behind the owner, stats,
   the spawn (04/23) with `uxParentActorID`, then 27/02 to party or owner; release by removal (no drop, no reward), 27/02; release on death,
   exit, portal; auto summon after load; 27/14 recall. Needs from `feature/party-maze`: the members of the maze and a party
   send (`PartyView`); without it, solo only. Tests: unit test of the limit rule (solo 3, party of 2 → 2, party of 4 → 0);
   e2e: enter a maze, `/helper-add`, summon, see 04/23 then 27/02, summon again → 58007, a fourth → 58008, release → 27/02.
6. **The follower AI** (L). `OpenWorker.Gameplay/Ai` (`AiMachine`, `IAiHost`): an owner on the host (position, radius,
   health); guard variables from it; MOVE to the owner for followers; `SetHelperFarDist`, `SetRecoverySkill`,
   `SetEscapeInfo` parsed (`OpenWorker.Lua/AiScriptLoader.cs`); the far-warp with 27/13; the RECOVERY state (HP floor 1,
   invincible, 5 %/s); buff skills on the owner. `OpenWorker.Lua/Managers/LuaCreatureManager.cs`: enemies of a helper are
   monsters, monsters find helpers (rank < 3), monster-to-monster hits (the damage path of `feature/combat`), kills by a
   helper credited to the owner (drops, experience, quests). Tests: `AiMachineTest`-style unit tests for "guard distance
   drives MOVE", "health 0 enters recovery and heals to full"; e2e: a summoned helper moves, uses skills on monsters,
   monsters die to it, the owner gets experience.
7. **Support** (M–L). A registration store readable by every server: a table `HelperSupports` (person key, type, value,
   expiry, reward state) and `HelperSupportUses` (supporter, user; unique pair) — **schema change**, the coordinator's
   migration — or Redis with a 24 h expiry for the registration and a set for the uses (uniqueness then rests on Redis
   set semantics, not memory). `OpenWorker.Channel` (shared, next to friends): register, info, reward, list (friends via
   `FriendBook`, scaling by level), equip (orb stored on the helper's entry in the book), friend points to the supporter
   (`FriendStore`), orb release on helper release and maze clear (27/11). Tests: one unit test of the scaling rule and one
   of "used once per user"; e2e with two testers who are friends.

### Open questions (only a run with the real client can answer)

- Whether the client accepts 27/02 before 04/23 for its own helper in practice (both orders are coded; the reference
  sends the spawn first).
- What the party panel shows for a helper (HP bar from which message) and whether `17/xx` stat updates of the helper actor
  are needed after the summon (the reference sets the stats after the spawn went out).
- Whether the client's recall button really hides itself in a party of two or more (its guard reads a count that looks
  like the party size: not sure).
- The reward timing of an orb: the reference lets it be taken while the registration is valid, but the window's texts
  (21548, 21549) speak of "after the support ends"; check what the window enables.
- Whether the support registration should survive a restart (the reference kept it only in memory).
- Maze type 12 (refused by the reference) does not occur in the client's maze table: ignore unless the table grows.
