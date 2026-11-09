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
person or the helper is not the person's, 10 when the database refused, 12 when either helper is not the person's, 15
never silent. **[ref]** Since 02, 06, 07, 08, 09 and 10 go through the client's "one request at a time" guard, a silent
answer leaves the guard shut; we answer each of them (see sections 4 and 5 and step 7 of the plan).

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

- the helper becomes a **monster** of the maze (its `tb_Monster` row), created 100 units **ahead of** the owner along its
  facing (the reference turns (0, -100, 0) by the owner's facing; in this client's convention facing 0 looks along -Y, so the
  point is in front, not behind as this section said before) — the point is kept only if the ground there is found within
  200 units of height, else the owner's own position — facing where the owner moves, its parent and owner the owner's
  actor. The creation sends the spawn of **one new monster, `04/15`** "in info", to everybody in the maze (`STMonsterInfo`
  with `uxParentActorID` = the owner's actor). **[ref][client]** `04/15` is a byte count then the records of `04/23`, which
  counts with a short; the client renames a helper and adds it to the party panel from either;
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
  faction: helpers are faction 10, at war with the sides `tb_Faction` marks 1 against it — 1, 6, 7, 8, 9, 12, 15..19 — and
  at peace with the rest, among them side 3 of the training dummies and boxes), within its sight. Skills whose target type
  (`tb_Skill.TargetType`, column 78) is not "enemy" (buffs, heals) target the **owner**. **[ref][table]**
- **`ENEMY_COUNT` and `USER_COUNT`** count, within the distance the script gives as its second number, the monsters of a side
  at war with the counting one and the players; **without a distance they are 0**. The scripts of the helpers wait in WAIT
  with `ENEMY_COUNT = 0` and `GUARD_DISTANCE > 320..350` and then MOVE to the owner. **[ref]**
- **Escape**: `SetEscapeInfo(points, chance, min, max, reset, kind, value)`: every blow adds points (skill 1, projectile 0.5,
  trap 0.2); full points start again from a random half and, with the chance, kind 2 plays skill `value` (kinds 0 and 1:
  super armor or invincible for `value` ms); points start again after `reset` seconds without a blow. **[ref]**
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
- **The life of a helper in the party panel** follows the hits it takes (06/13, the life after the hit), its healing (17/12)
  and 12/12, all three by actor and whatever the kind of the panel entry; the entry starts from the life and maximum of the
  spawn. No message of its own is needed. **[client]**
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
  in the item's inventory (the inventory of its classify row, the bag for every helper item): **52010** if none. Any other
  failure: **58004**. The client also refuses on its side items of another class (52009) or above the owner's level.
  **[client]** Checked again for step 4 **[ref]**: the reference does not check the level of the item, takes the helper slot
  of a swap from the request without a bound, and on a take-off checks only that the slot holds an item the tables know,
  not the serial the request names. We check all three (see the comparison in step 4).
- Success: the items move (inventory item removed with the usual "item gone" message, the taken-off item created in the
  inventory with the usual "item created" message), the stats are recomputed (a summoned helper's actor too), and `27/10`
  {0, record} is sent. Equipping works **anywhere** (district or maze). **[ref]**
- `27/11` is sent when an orb is released (helper release, maze clear). `27/04` on level up.

## 5. Support (friend orbs)

**[ref]** unless marked.

- **Register (06)**: type 0 = attack orb, value = **20 %** of the person's physical attack (max), type 1 = defense orb,
  value = 20 % of its physical defense; valid **24 hours**. Another type registers an orb worth 0 in the reference; we refuse
  it. The registration is held by the cross-server part (one per
  person, in memory in the reference server, not in its database). If a still-valid registration exists: **58003** in the
  result of 06 when its reward is still to take, no answer when it was taken. On success `27/06` {0, info with registered =
  1 and reward state 1}.
- **Info (05)**: registered = 1 while valid (an expired registration is dropped on this request), the reward state
  (0 none, 1 to take, 2 taken), type, value, expiry.
- **Reward (07)**: only with reward state 1: the items of `tb_Helper_Reward` row 1 (item 800000999 × 1; friend points 0)
  are checked against the inventory (**52010** full, **52004** other failure), then the cross-server part sets the state to
  2, the items are created, the friend points added, `27/07` {0}. **[ref][table]** When it may be taken (checked for step
  7): the reference allows it as soon as the orb is registered, at any time while the state is 1, and drops an expired
  registration (with a reward never taken) when the window asks for the info. The client enables the reward button from
  the reward state alone (state 1, with an orb kind), whatever the time **[client]**; the texts 21527, 21548 and 21549 ("the
  reward can be taken after the support ends") are the window's wording, not a rule the client applies.
- **List (08)**: the person's **friends** who have a registration and whose orb this person has **not used yet** (each
  orb can be used once per user for its 24 hours). The value shown is scaled when the friend is more than 5 levels above
  the reader: `value = original / friend level × reader level`. Neither strangers nor league members. The reference does not
  check the time here (the research said "valid"): an expired orb is listed until something drops it, and then refused on
  use with result 1; we list valid orbs only.
- **Equip (09)**: the helper must be the person's; the friend's registration must exist (result 2 if not) and still be
  valid (result 1 if expired: it is also dropped); the same scaling by level; then the database stores the orb on the
  helper, the use is recorded (the friend leaves this person's list), the friend whose orb was used gets the friend points
  of `tb_Helper_Reward` row 2 (0 in both copies of the table) with this person, and `27/09` {0, helper, record with the
  orb and recomputed stats}. The client sends 09 only for a friend marked usable in its list. **[client][ref]** Checked for
  step 7 **[ref]**: the reference does not check that the one who offers is a friend of the user (only that it has a
  registration), and gives the friend points only when the one who offers is online at that moment.
- **Lifetime of a worn orb**: until the helper is released or the maze is cleared (then 27/11 and chat 21533); outside
  that, the stats count it only while its date is in the future. At the clear of a maze the reference releases the orbs of
  the **called** helpers only (the research said every helper of anyone in the maze): a helper left out of the maze keeps
  its orb. A new orb replaces the one a helper wore.

## 6. How a helper is obtained, and the tables

- **Quests**: the eight episodes 300005, 300010, 300015, 300020, 300025, 300030, 300035, 300040 (contents 3, "helper
  episodes") each give one helper through `tb_Quest_Episode.RewardHelper` (Mirium, Ralfh, Hidinger, Keinbarrel, Goodboy,
  Ryu, Zenis, Lucy). The server already grants it (`QuestManager` → `HelperManager.AddAsync` → `27/03`, achievement 48).
  **[table]** The quest "helper" of 15/09 and the daily mission "helper" are a different thing (the quest tracker).
- **Helper items**: the NPC shops 1001, 1002 and 1003 of `tb_Shop` sell ranks 2 to 4 of the weapons and gears (48 rows);
  the rank 1 items (level 1) appear in no shop, quest, reward or drop table of either copy of the tables, and the 242 slot
  has no item at all. **[table]** The chat command `/item <id>` gives any of them (it existed already).
- **Chat command** `/helper-add <id>` (testing). The reference server's own command (number, value) does: 1 add, 2 summon,
  3 release, 4 register, 5 reward, 6 list, 7 support equip, 8 equip.
- **Proposed column names** (do not edit `Structures.json` here; for the implementation agent):
  - `tb_Helper`: `Id` (helper = monster id), `Number` (the class number items are limited to), `Field2` → `TypeText` (UI
    string of the type), `Field3` → `Icon`, `Field4` → `Illustration`.
  - `tb_Helper_Reward`: `Id` (1 = registration reward, 2 = reward per use of the orb), `RewardType`, `FriendPoints`,
    `Item1..3`, `Count1..3` — **named** (step 7). The server's own copy is identical: no server row is needed.
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
| 27/02 summon/release in a maze | done (step 5) | — |
| 27/14 recall | done (step 5) | answered with 27/13 per called helper; nothing in a district |
| 27/13 sync pos | done (step 6) | the far warp and the recall |
| Auto summon after loading a maze | done (step 5) | — |
| 27/04 stat update | done | sent to the owner at every level reached (`ExperienceService`) |
| Stats (origin / additional) | done | `HelperStats`: origin at the owner's level; additional from worn items and the orb, empty until they exist |
| 27/10 equipment | done (district and maze) | — (step 4) |
| 27/11 update info | done | sent by `HelperGear.ReleaseOrb`: at the release of a helper by any way, and at a maze clear for the called helpers |
| AI of a follower | done (step 6) | — |
| Kills credited to the owner | done (step 6) | — |
| Release on death / exit / portal / change of server, others' helpers on join and leave | done (step 5) | — |
| 27/05..09 support | done | the total friend points of the reward row 1 (0 in both tables; no wallet for them) |

The e2e (`docs/tools/e2e_helper.py tester9 password [maze location]`, stand 9): before steps 1 to 3 **25 PASS, 0 FAIL**
(district only); after them **40 PASS, 0 FAIL** on a build of `feature/helpers-base` over `dev` `b6e00cb`: the record read
the way the client reads it, origin stats non-empty and below 61, 27/04 at a level reached (`/exp`, which now gives real
experience) with grown stats, the list again after re-entry with the new level's stats, and in a maze (M04, 21311) the list
with the same stats, 27/15 and 27/12 answered, a summon refused with 58007, a recall answered with nothing.

After steps 4 and 7 (`feature/helpers-gear`, stand 3, `e2e_helper.py tester9 password 21311 tester10`, the last argument the
friend who offers the orb): **70 PASS, 0 FAIL**. The old refusals are replaced by the equipment (items given by `/item`, put on, refused for
another helper, a higher level and a taken slot, swapped, a gear in its slot, taken off, put back on; 08/05 and 08/06 with
the right slots; the stats added) and the orbs (the two become friends through 19/11 and 19/13; info, an unknown kind
refused, an attack orb offered for a day with its reward waiting, a second refused with 58003, the row in the database, the
list of the friend and not of the one who offers, the orb put on with its value added to the attack, the list empty after,
a second use refused with result 3, no orb from a person who offers none, the reward taken once with item 800000999); after
re-entry the items and the orb are still on the helper; in the maze the list carries them and an item is taken off there.
The old script on the new build: 37 PASS, 3 FAIL, all three intended (a type 2 orb is refused, 27/09 answers result 2
instead of an error, no orb is offered at re-entry).

After steps 5 and 6 (`feature/helpers-summon` rebased on the merge of steps 4 and 7, stand 2,
`e2e_helper.py tester11 password 21311 tester10`): **88 PASS, 0 FAIL** (the script of steps 4 and 7 gave 70; it also checks the gear part as before). The maze part now: two helpers more are given, the person
enters M04 with the automatic call on and after loading gets three helpers in the order of its list (04/15 with the person
as parent, then 27/02 each, the record called), a call at once is refused 58005, after ten seconds a fourth body 58008, one
that is out already and one the person does not have 58007; one is sent away (17/11 for its actor, 27/02 summon 0) and a
second release of it is 58007; with room again the fourth comes; a recall gives one 27/13 per called helper; the person
walks to the puppets and stands, and the helpers walk after it (05/02), use their skills (06/10), hit monsters (06/13 with a
helper as attacker), kill them (17/11) and the person gets the experience (03/37); the helper with the friend's orb is sent
away and the orb comes back (27/11 without it). Reading the maze connection needs a deadline (`deadline_frames`): monsters
and helpers keep it busy.

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
4. **Equipment 27/10** (M–L) — **done** (`feature/helpers-gear`).
   - **Where the items are.** What a helper wears stays an item of the person (`Items`), in the storage the client and the
     reference call the helper storage, **105** (`StorageGroup.HelperEquip`, `ItemPersistent.HelperStorage`), at the place
     `helper number × 3 + slot` (`Inventory.HelperPlace`; Ralfh, number 103, wears his weapon at 309). The inventory keeps
     that storage apart from the storages the client names (`Inventory.WornByHelper`), so the requests that move, sell,
     trade, upgrade or count items by storage and slot never reach a worn item; `Inventory.Items` includes them, so the
     flush, the restore of a failed write and the sockets and broaches at entry see them. They move only through
     `InventoryService.PutOnHelper` (put on, or swap: the worn item goes to the bag slot the new one leaves) and
     `TakeOffHelper` (into the slot the client names when it is a free slot of the item's bag, else the first free one),
     which send the usual "item gone" (08/05) and "item created" (08/06) and mark the inventory dirty.
   - **Schema (coordinator's migration).** One unique index on `Items`: `IX_Items_HelperPlace` on `(PersonId, Slot)`
     filtered by `"Storage" = 105`. The general index `(PersonId, Storage, Slot)` stays non-unique. Since a swap writes two
     items that trade a place, the flush (`ItemStore`) first moves the rows of the items it is about to write that lie in
     storage 105 to a place of their own (`Slot = -1 - Slot`, unique since the old places were), when one of them goes to
     storage 105; the test fails without that step.
   - **Rules** (`HelperGear.Equip`, shared by district and maze through `HelperService`): the three shapes by the serials
     the request names; the helper the person's and in `tb_Helper`; the item in the slot the request names, with that
     serial, in the bag of its kind; slot type 240..242, class limit = the helper's number, level limit ≤ the owner's level;
     a put-on only into an empty slot; a swap only of the worn serial at that slot and an item of the same slot; a take-off
     of the worn serial. Codes: **58004**, and **52010** for a take-off into a full bag. Then 27/10 {0, record}; the record
     (`HelperGear.Record`, also used by 27/01 and 27/03 through `HelperManager`) carries the worn items and the additional
     stats of `HelperStats` (attack to 20 and 21, defense to 24).
   - **Comparison with the original:**

     | Point | Ours | Why |
     |---|---|---|
     | Shapes of the request, order of the checks, codes 58004 and 52010 | same | |
     | Messages: 08/05 for the item leaving the bag, 08/06 for the item coming back, then 27/10 {0, record} | same | |
     | Storage 105 for the worn items, three per helper | same storage; the place encodes the helper | the reference keys the rows by helper in its own procedure; ours keeps the item table as it is |
     | One item per place | a unique index | the reference relies on its procedure; rule 3 |
     | Level limit of the item | checked | the reference does not; the client does, so a sound client never sends it |
     | Helper slot of a swap | bound-checked | the reference indexes its array with the client's value |
     | Serial of a take-off | checked against the worn item | the reference only checks that the slot holds an item |
     | Bag item | must lie in the slot the request names, in the bag of its kind | the reference looks the serial up in any storage |
     | Target of a take-off | the slot the client names when free, else the first free | the reference always takes the first free; the client names a free one |
     | Answer when the database refuses | not possible (the flush writes later) | the reference sends nothing |
     | Stats of a called helper's actor | not recomputed here | no helper is called yet (step 5) |
   - **Tests**: `HelperGearTest.PuttingOnSwappingAndTakingOffKeepOneItemPerSlot` and `WornItemsAndAValidOrbRaiseTheStatsOfTheHelper`
     (client tables), `PersistenceTest.AHelperPlaceHoldsOneItemThroughSwapsFlushesAndANewEntry` (Postgres: put on, swap and
     take off over flushes and a new entry, and the index refusing a second item at a place).
5. **Summon and release in the maze** — **done** (`feature/helpers-summon`).
   - **Rules** (`OpenWorker.Gameplay/Modules/Helpers/HelperRules`): the checks in the reference's order and with its codes
     (down: nothing; 10 s since the last call: 58005; not in a maze: 58006; a maze that refuses helpers (`tb_Maze_Info.RefuseHelpers`,
     column 7, or type 12), a helper not owned or out already: 58007; no room: 58008; the actor not made: 58007). Room: the
     helpers of the whole instance plus the people of the party (one alone) stay under four; the party is read from
     `IPartyRoster` (the players of the maze when Redis cannot answer). Release: down nothing, 58006, 58007.
   - **The place** (`IHelperField`, given by the maze server as `MazeHelperField`; the district has none and answers as
     before): `HelperService` hands summon, release and recall to it. The record of 27/02 is `HelperGear.Record(…, summoned:
     true)` (what the helper wears and a valid orb), and the stats of the actor are `HelperStats.Combat` of the record: origin at
     the owner's level plus what the record adds to the attacks and the defense.
   - **The helper in the maze** (`OpenWorker.Lua/Helpers`: `HelperSquad` per creature manager, `HelperBody`, `SummonedHelper`;
     `LuaMaze.Helpers.cs`): a monster entity of the helper's row outside the creatures of the maze (players do not hit it,
     scripts and sector counts do not see it), its actor from the maze's counter, stats as above, no super armor, never
     holding a sector. 04/15 to everybody in the instance, then its mind starts, then 27/02 to everybody in the instance.
     Release: 17/11 for its actor (no drop, no reward, no kill), 27/02 summon=0, `HelperGear.ReleaseOrb` (27/11). A person
     who falls (`PlayerVitals.HealthChanged` to 0) or leaves (`LuaMaze.Leave`: exit, portal, change of server, disconnect)
     loses its helpers, and its client is sent 27/02 summon=0 for the helpers of the others. A member who comes later sees
     the helpers (04/23) and gets 27/02 for each. Auto summon after 11/22 for a person in no party, in list order while there
     is room, without the delay. `MazeSummonedHelpers` tells the maze clear which helpers are called (their orbs go back).
   - **Comparison with the original**:

     | Piece | Original | Ours | |
     |---|---|---|---|
     | Order of the checks and codes | as above | same | same |
     | Count of the party | the people of the party | the people of the party from Redis, the players of the maze when Redis fails | same |
     | Spawn | 04/15 of one monster to the maze | same | same (the research guessed 04/23) |
     | Place | 100 ahead along the facing, ground within 200, else the owner's place | same | same (the research said behind) |
     | 27/02 at a summon | to the party wherever its members are, else the person | to the players of the instance | kept different: only they can see the actor; a member elsewhere has nothing to name |
     | 27/02 at a release | to the party of the maze, else the person | to the players of the instance | same in effect |
     | Release of the actor | death with a special reason | 17/11 of the actor | same in effect (no drop, no reward) |
     | Orb at a release | released (27/11) | same | same |
     | Release paths | death, exit, maze exit, world enter, district transport | death, every way out of the maze | same |
     | Others' helpers at leaving | 27/02 summon=0 to the leaver | same | same |
     | Joining later | 27/02 after the monster list | 04/23 of the helpers then 27/02 | same |
     | Auto summon | after loading, alone, flag, maze allows, no delay | same, before the script hears the entry | same |
     | Last call time | kept by the person across mazes | kept by the instance per person | kept different: a person leaves a maze to reach another, which takes longer than the delay |
     | Logs of the database (16/2, 16/3) | written | not written | no such log here |
   - **Tests**: `HelperSummonTest`: the checks in their order and the room alone, in a party of two and of four; a called
     helper shown (04/15, then 27/02) to both players of an instance and gone with its owner; a monster killed by a helper
     is the owner's kill; the byte layout of 04/15.
6. **The follower AI and its fighting** — **done** (`feature/helpers-summon`).
   - **The machine** (`OpenWorker.Gameplay/Ai/AiMachine.Follower.cs`, called from a few places of `AiMachine`; `IAiHost.Owner`,
     `HasGround`, `WarpTo`, `Heal`, `FindSkill`, `CountUsers` with defaults for the hosts of monsters): the guard variables
     read the owner (distance, distance less its radius, health in percent; -1, -1, 100 without one); MOVE goes to a place
     around the owner at the state's distances and angles; too far (3500 out of a fight, 9000 at the start of ATTACK; 500
     after ten ways not found) with ground at the owner it is put there (27/13), forgets its target and chooses again; a
     follower never returns home; a skill with `TargetType` 1 is used on the owner; at 0 life it keeps 1 and enters RECOVERY
     (recovery skill played, invincible, a twentieth of its life each second, 17/12, then whole and fighting again); escape
     points from the blows it takes and the escape skill. `SetHelperFarDist`, `SetRecoverySkill`, `SetEscapeInfo` are read
     by `AiScriptLoader`. `ENEMY_COUNT` and `USER_COUNT` now count within the script's distance and are 0 without one, as the
     original counts them (they counted every player of the maze before, which kept the helpers in WAIT for ever).
   - **Sides**: the enemies of a helper are the monsters of a side at war with it (`tb_Faction`, named `LocalFaction`,
     `OtherFaction`, `Relation`; side 10 of the helpers); monsters of a side at war with side 10 may target a helper when it
     hits them and find it by themselves below rank 3; their hits (`HitBy`) go through `DamageCalculator` with the helper's
     stats; a helper getting up takes nothing.
   - **Its blows**: queued while the helpers think and dealt by the maze after them (`LuaMaze.StrikeAsync`): the damage of a
     player's hit (`ApplyDamage` with the helper's stats and the monster's aggro on the helper), 06/13 from the helper, and the
     dead go the way of a player's kill with the owner as killer (`MonsterKilled` → experience by the party rules and drops,
     the reporter for quests and missions, the script's `OnDie`, the sector).
   - **Comparison with the original**:

     | Piece | Original | Ours | |
     |---|---|---|---|
     | Guard variables, MOVE to the owner, far warp, forced 500 | as above | same | same |
     | TRACE_MOVE | far check at its start | not done | the scripts of the helpers do not use it |
     | Recovery | as above, buffs cleared, idle motion at the end | same without the buffs and the idle motion | approximately: no buffs on helpers yet |
     | Escape | for every monster, from every blow | parsed for every script, run for helpers from the blows they take | kept different: 127 monster scripts have it, and their blows come through the shared damage path of the player |
     | `ENEMY_COUNT`, `USER_COUNT` | within the distance, 0 without one | same | fixed (counted every player) |
     | Skill on the owner | a skill of target type 1 → owner for followers, nothing (WAIT) for monsters | owner for followers; a monster still uses it on its target | kept different for monsters: unchanged behaviour |
     | Enemies | the enemy rule of origins, the hit bits of a summoned monster row and factions | factions only | approximately: no summoned monster uses the bits here |
     | Rank | rank ≥ 3 does not search followers | same | same |
     | Kills | owner's: drops, experience, quests, title, killer of the death message | owner's: drops, experience, quests, missions, achievements | same |
     | Buffs of its skills | applied | through the buffs of the maze (`MazeBuffs`): a trigger for its own side gives its buff to the helper, or to the players and helpers its area reaches; a buff-only trigger to the monsters it reaches; a hit's buff to the monsters it hit; the blows it takes carry their buffs to it; a held helper stands still; stats and defence of both sides read with their buffs | same |
   - **Tests**: `HelperAiTest`: follows its owner and is put next to it when too far; beaten down it gets up instead of dying.
   - **Cost on the maze step** (`MazeStepBenchmark`, new parameter `Helpers`: none or three called beside the player;
     BenchmarkDotNet short job): 10 monsters 1.695 → 1.991 µs (24 → 72 B), 10 monsters and 100 timers 8.650 → 8.682 µs,
     100 monsters 13.970 → 14.005 µs, 100 monsters and 100 timers 19.126 → 22.609 µs (the last with an error of ±70 µs of the
     short job). The heaviest case stays near 23 µs a maze, 4.5 ms for 200 mazes, inside the 5 ms of a pass.
   - **Other scripts on the stand** (after the rebase on the merge of the buffs): `e2e_helper.py` 88 PASS, 0 FAIL;
     `e2e_maze.py` 62 PASS, 2 FAIL, the two projectile checks that fail on `dev` too; `e2e_party_maze.py` 24 PASS, 0 FAIL;
     `e2e_prologue_fight.py` 20 PASS, 0 FAIL; `e2e_buffs.py` 12 PASS, 0 FAIL in one run, 11/1 in two: its last check compares
     the mean damage of a few combo hits on two monsters, which hit parts land varies (18 against 38 on one target); on a stand
     of `dev` it passed twice with the same spread. No helper is in the tutorial.
7. **Support** (M–L) — **done** (`feature/helpers-gear`).
   - **Where it is kept: the database.** An orb must be seen by every server and outlast a restart for its day, one orb per
     person and one use per user are limits, and the reward is given once: all of that is what `ARCHITECTURE.md` gives the
     database (rule 3: uniqueness and limits are constraints). Redis would hold the day, but a use and a reward taken would
     then rest on Redis commands, and a restart of Redis would lose a reward still to take. **Schema (coordinator's
     migration):** table `HelperSupports` (key `PersonId` → `Persons` cascade; `SupportType` smallint, `Value` real, `Until`
     timestamptz, `Reward` smallint: 1 to take, 2 taken; `CreatedAt`, `UpdatedAt`), table `HelperSupportUses` (`Id` bigint
     identity; `SupporterId`, `UserId` → `Persons` cascade; `Until` timestamptz, the day of the orb used; `CreatedAt`,
     `UpdatedAt`; unique index `(SupporterId, UserId, Until)`, index `UserId`). The orb a helper wears is kept on its entry
     of the book (`HelperEntry.Orb`, in the `MiscState` document, written by the flush).
   - **Rules** (`OpenWorker.Channel/Community/HelperSupports`, store `HelperSupportStore`, handlers in `HelperService`):
     - 06: type 0 or 1 (another is answered with result 1); value = 20 % of the person's physical attack (max) or defense
       (`PlayerStatsService`); valid 24 h. One statement inserts the row or replaces it only when the old day is over and its
       reward taken; else result **58003** (reward waiting) or 1 (still valid). The uses of the old orb are dropped.
     - 05: registered while valid; an expired orb whose reward waits is still shown (so the window offers the reward); one
       expired with its reward taken is shown as nothing.
     - 07: state 1, room in the bag (**52010**), then a conditional update claims the reward once (**52004** for no reward,
       or one already claimed), then the items of row 1 are given and 27/07 {0}. If the bag filled while the claim was being
       written, the claim is given back and 52010 answered.
     - 08: friends (both kinds `Friend`) with a valid orb not used by the reader; the value scaled by
       `HelperSupports.Scaled` (friend more than 5 levels above: `value / friend level × reader level`), the friend's level
       from its row.
     - 09: the helper the person's (else **58004**); a friend's valid orb not used yet: the use is inserted (the unique
       index decides between two servers), the friend points of row 2 are added to the friend's row for the user in the
       same transaction, the scaled orb goes on the helper and 27/09 {0, helper, record}. Refusals answer 27/09 with result
       1 (expired), 2 (no orb, or not a friend), 3 (used already) and the helper's unchanged record.
     - 27/11: `HelperGear.ReleaseOrb(player, helper)` drops the orb of a helper and sends the record (the client says 21533).
       `HelperGear.OnMazeCleared(player)` releases the orbs of the helpers that are called (`ISummonedHelpers`; the default
       `NoSummonedHelpers` says none is), called by `MazeReporter.MazeClearedAsync`. The release of a called helper (step 5)
       must call `ReleaseOrb`. An orb whose day ended adds nothing and is not shown; it stays on the entry until replaced or
       released.
   - **Comparison with the original:**

     | Point | Ours | Why |
     |---|---|---|
     | Value (20 % of attack max or defense), 24 h, reward items of row 1, friend points of row 2 | same | |
     | Codes 58003, 52010, 52004 and results 1 and 2 of 09 | same | |
     | Where an orb is kept | database rows | the reference kept them in the memory of its relay: a restart lost them |
     | A type other than 0 and 1 | refused | the reference registered an orb worth 0 |
     | 06 when a valid orb exists with its reward taken; 07 without a reward; 09 used already or helper not owned | answered (result 1; error 52004; result 3; error 58004) | the reference is silent, which leaves the client's request guard shut |
     | Reward of an orb whose day ended | kept until taken; blocks a new orb with 58003 | the reference dropped it at the next info, unclaimed; 58003's text and the window's texts speak of taking it after the end |
     | List | valid orbs only | the reference listed expired ones too and refused them on use |
     | Use by a person who is not a friend | refused (result 2) | the reference checked only that the orb exists |
     | Record in a refused 27/09 | the helper as it is | the reference sends an empty record, which the client reads into its own copy |
     | Friend points of a use | written to the friend's row whether online or not | the reference added them only when the friend was online |
     | Total friend points of row 1 | not kept | 0 in both tables, and the wallet has no friend points |
     | Level of the friend for scaling | its row (written by the flush) | the reference used the live level of its friend list |
     | Orbs released at the clear of a maze | called helpers only | same; today none is called |
   - **Tests**: `HelperSupportTest.AnOrbIsListedToFriendsAndUsedOncePerFriendWhileItsDayLasts` and
     `TheRewardIsTakenOnceAndANewOrbWaitsForTheEndOfTheLast` (Postgres), the stats of a valid and an expired orb in
     `HelperGearTest`.

### Open questions (only a run with the real client can answer)

- Whether the client accepts 27/02 before 04/23 for its own helper in practice (both orders are coded; the reference
  sends the spawn first).
- What the party panel shows for a helper: answered from the client (section 3, release): 06/13, 17/12 and 12/12 by actor;
  to see on the real client.
- Whether the client's recall button really hides itself in a party of two or more (its guard reads a count that looks
  like the party size: not sure).
- The reward timing of an orb: answered for step 7 (the client enables the button from the reward state alone; see
  section 5). Whether the window's text changes at the end of the day is the Scaleform part of the window, not read.
- Whether the support registration should survive a restart: it does now (rows of the database).
- Maze type 12 (refused by the reference) does not occur in the client's maze table: ignore unless the table grows.
