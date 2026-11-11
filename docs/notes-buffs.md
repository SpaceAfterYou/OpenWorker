# Buffs and debuffs

What a buff is, where it comes from, what the client is told and what the server does with it, as the maze server keeps
buffs now. The client 1.4.2.3 decides every layout; the original game server decides the rules. Earlier notes:
`docs/notes-combat.md` (the checks of a skill, the gauges, the damage), `docs/notes-ai.md` (monsters),
`docs/notes-party-maze.md` (the shared instance).

## The buff table

`tb_Buff` of the client, 1184 rows. Its columns are named in `Structures.json` by matching the client's columns with the
structure of the original over the 764 rows both have (`BuffRow`):

| Column | Name | What it is |
|---|---|---|
| 0 | `Id` | unsigned 16-bit (rows above 32767 exist; the action files name them as `int`) |
| 1 | `AllowedPlaces` | bits: 1 anywhere, 2 a maze, 4 a district, 8 a battle zone (10 for almost every row) |
| 2 | `Kind` | 0 buff, 1 debuff, 2 combined |
| 3 | `Effect` | what it does, see below |
| 4 | `Group` | buffs of a group replace or stack on each other |
| 5 | `SuccessRate` | out of 10000 |
| 6 | `Grade` | a buff of the group replaces another only from the same grade up |
| 7 | `DurationMilliseconds` | 0: lasts until something takes it away |
| 8 | `StandByMilliseconds` | how long a hit waits before the buff begins |
| 9 | `TickMilliseconds` | how often it acts while it lasts |
| 10, 11 | `OverlapType`, `OverlapCount` | how a second one of the group counts, how many may stack |
| 12 | `DeleteType` | what does not take it away: 1 death, 2 leaving the game |
| 13 | `ActionName` | the animation the carrier plays (`B_Stun`, ...) |
| 14..17 | `CallOutBuff`, `CallOutSkill`, `HitCallBuff`, `ChangeBuff` | what follows the end, the top of a stack |
| 18..23 | `Stat1..3`, `Value1..3` | up to three stats, by the numbers of the stat effects of items and titles, and their values |
| 24..34 | (unnamed) | the client's own: icons, effect files, strings |

The client has no column for the original's "system type" (0 ordinary, 1 not given to a boss, 2 and 3 not held back by resistances
and not rolled, 3 also gets through invincibility). The server's own `tb_Buff` has it; `BuffSystemRow` (optional, read from
`res-server/` only) gives it to the buffs of the client's table that the two tables agree on (same kind, effect and group: 893 of the
1184, 22 of them with a type of 2 or 3); the others are ordinary buffs.

The tutorial's buff 10000 is in this table (the earlier note that it was missing was wrong): it lasts, ticks every half
second and gives back 5 % of the life, all of the soul gauge and 3 stamina at each tick.

`tb_Deck_Bonus` columns named too: `Phase`, `BonusType` (0 damage, 1 cooldown, 2 soul gauge cost), `RequiredLevel`,
`BonusValue`.

## Effects

| Effect | What the server does |
|---|---|
| 0 stats | each stat that is not a current value goes into the stats of the carrier (through the same stat effects as titles and gear); a current value (13/14 life, 17/18 soul gauge, 21/22 stamina, 27/28 soul vapor; even numbers in percent of the greatest) is given back when the buff begins and again at every tick, and never taken back |
| 4, 5 | super armor, invincible: the defense of the carrier in a hit |
| 7 with stat 66 | the carrier's skills cost no soul gauge (it must still have it) and cool down shorter by the value, in percent |
| 11 | takes away the carrier's buffs of the effect its first value names |
| 15 | (counted, not used yet) the soul gauge a hit gives is doubled |
| 17 | cooldowns shorter by the value in hundredths |
| 18 | stamina costs lower by the value in percent |
| 24 | a bundle: gives the three buffs its values name instead |
| 26 | takes the value in percent of the greatest life once, when it begins |
| 30 | soul gauge costs lower by the value in percent |
| 111..114, 133 | burn, poison, shock, bleed, corrosion: hurt at every tick, by stat 13 (a number), 14 (percent of the current life) or 16 (percent of the greatest) |
| 121..124 | stun, paralysis, sleep, freeze: the carrier neither moves nor attacks |
| 125, 126, 131, 132 | charm, confusion, silence, weakness: kept as states (weakness also changes stats) |

| 8 | reflection: the carrier loses the value in percent of the damage it takes once more (never to nothing), as the original has it |
| 9 | blood: the carrier takes back the value in percent of the damage it deals as life, for as many targets of one hit as its first stat says (0: all) |
| 12 | a blood mark: whoever gave it, and its friends, take back that share of the damage they deal to the carrier |
| 13, 14, 16, 19 | kept (the greatest damage roll, a once-only multiple of a summon's damage, more super armor taken, more gold) and counted in `BuffBook.Rates`; nothing reads them yet |
| 21 | taunt: the monster that carries it is turned on whoever gave it (its care for the giver grows by 5 and its target is the giver) |
| 25 | soul absorption: a hit gives the attacker's soul gauge the value in ten-thousandths of the gauge, for the first targets the first stat names |
| 28, 29, 31 | more damage to bosses (rank 3 to 5), less from bosses, more of all damage; added up over the buffs of a carrier |
| 2, 20, 23, 27, 32 and above | kept and shown; no row of the client's table needs them (20, a buff of the district that gives fatigue back, and 2 are in it) |

Anything else is kept on the carrier and shown, and does nothing more. A buff is held back by the place: a row allows a maze (bit 2),
a district (bit 4), a battle zone (bit 8) or anywhere (bit 1); `BuffRules` refuses a buff where its row does not allow it and takes the
buffs a carrier may not have away when it comes to a place (`ClearForPlace`). 1136 rows of the client's table are for a maze and a battle
zone (10), 41 are for anywhere (the broach sets and a few more), 6 are for a battle zone only and 1 is for a district.

## Where buffs come from

- **The reaction of an attack.** Every attack trigger of an action file names a target type and a buff (`AttackTrigger.Buff`,
  `TargetType`, read between the damage rate and the reaction type). The 46 hero files have 344 triggers with a buff, the
  1018 monster files 1658. An attack for the enemies gives its buff to every target it reached, after the damage, by the chance
  of the buff. An attack for the attacker's side (target types 1 self, 2 self and friends, 6 friends, 7 self and party, 8 party)
  hurts nobody: type 1 gives the buff to the attacker wherever it stands, the others to the players (or monsters, for a
  monster) its area reaches, the attacker among them for 2 and 7. An attack whose only effect is a buff (a damage rate of
  zero) hurts nobody either (before, such a trigger with a reaction dealt its whole damage).
- **A maze script**: `ApplyBuff(actor, buff)` gives it for sure (no chance, no stand-by), with the actor as its owner, to a
  player or a monster of the maze. The tutorials call it with 10000 at entry.
- **Anything else in the maze** (a helper, a chat command): `MazeBuffs.Give(target, buff, source)`, reached through
  `LuaMaze.Buffs`, gives it as a hit does; `Give(target, buff, owner, roll: false)` as a script does.
- `/buff-give <buff> [monster actor]` gives one for testing.

- **Passive skills** (`PassiveBuffs`, `BuffSources`): a skill of type 1 whose passive type is 13 gives the buff its passive value
  names for as long as the player has it, and so does the divergence chosen for it when that is of the passive kind (the client's table
  has none). 32 skills of the client are such (three or five levels each, buffs 5001 to 5503). They are matched against the book once a
  second and at once when the learned list is replaced, so a skill learned or reset in the maze shows within a moment, and a death
  that took the buff is made good. They work in a maze only (their rows are for a maze).
- **Passive cards**: a card in the quick slots of the book whose row names an invoke condition (`InvokeCondition`, its chance
  `InvokeRate`, the buff `Buff`, the `Cooldown` of its group) gives its buff when the condition happens and the chance and the cooldown
  allow, and the player is told with 06/21. The conditions the maze reports: a hit (1), a critical hit (6), a missed hit (11), being
  hit (16), a critical hit taken (21), a skill used (26), being equipped (46: every second while the card is in the slot), the life
  under a fifth (50) and under half (54), a combo of a hundred (48) and of twenty (49). The cards of the client's table with other
  conditions (jump, dash, evade, items picked up) are not fired: the maze has no such events. 223 of its 298 cards name a condition.
- **Combos** (`tb_Combo_Buff`): the hits of one player, none more than three seconds after the one before, count; when they reach a
  row's number the row gives its fixed buff and a few random ones picked one after another by their weights. Past the last row the
  last row repeats every hundred hits. The soul vapor a row gives is not given: the server has no soul vapor of a player.
- **Auras** (`tb_Aura`): `SetAura` puts an aura on a person or a monster: its buff goes to the ones it is for (the carrier alone, or
  the carrier and its friends, its friends, its enemies, everyone else, by the target type) within its distance, now and at every
  check, until the aura runs out or is replaced; 06/18 tells the maze. An aura comes from an item (`EFFECT` items of effect type 2:
  two in the client's table), from the chat command `/aura-give`, or from the reaction of an attack whose action file names one. The
  action files of the client are of versions up to 237 and the aura sits in versions from 242, so no attack of the client names one;
  the reader reads it for the files of newer versions (a test writes one).
- **Boosters**: a booster's status effect (effect 9) names a stat effect and a value; the stats go into the stat sheet as long as the
  person has the booster, and the stats service is told at every change of the boosters.
- **Broach sets**: the clothing gives the buff of a set (27001 to 27045) to its wearer, in a district too, and shows it. Its stats are
  counted by the gear as before, so the buff is marked as shown only (`StatsCounted = false`); no choice of the one that is shown.
- **Resistances**: the chance of a buff is the table's less what the carrier resists of its state (the stats 49 to 60, which gear,
  titles and buffs raise); a buff of a system type of 2 or 3 is not rolled at all.
- **A monster that gets up** (the recovery state of the AI) loses every buff at its start, at every step and at its end.

Not given: the conditional buffs of a hit that the client's tables name for items and titles (their skill options), the buffs of an
interaction object (an aura link), and the cards that wait for jumps, dashes and evades.

## Carried from one server to the next

A person writes its buffs down when it leaves a server (person state kind 14, `BuffMemory`) with the time each has left, and gets
them back when it comes to the next one (the maze or the district), in a relog too. The time away does not count. A buff the place
does not allow is not put back, as the original takes such buffs away on entering an area. The buffs that the person's passive skills
and clothing give are not written down: they are given again on entering. What a buff gave when it began (life back) is not given
again when it is put back.

## A district

`DistrictBuffs` keeps a book for each person of a district, ticks it on a simulation clock of the district, tells the person and
those who see it (06/14 to 06/16), runs the sources (the clothing's broach set) once a second, restores what the person carried in
and writes it down when the person leaves (a close handler before the one that writes the person). Only buffs whose rows allow a
district or anywhere can be there.

## The rules

Kept per carrier in a `BuffBook` (`OpenWorker.Gameplay/Buffs`); a player has one from the moment it is in a maze until it
leaves (a component the stats read), a monster from the first buff it gets. `BuffRules` changes a book and writes what
changed; `MazeBuffs` (`OpenWorker.Lua/Buffs`, one per instance) tells the clients and changes life and gauges.

1. **Giving** (a hit or a source): a bundle gives its parts; the chance of the buff is rolled; a buff with a stand-by time
   waits for it (and is dropped at once when its group could not take it). The dead take nothing.
2. **Applying**: at most 50 buffs on a carrier; a debuff does not stick to what is invincible; a frozen carrier takes no
   other state and nothing that hurts. A buff with no owner is the carrier's own.
3. **A group already there**: the new grade must be at least the old one (above it for a taunt). A group whose stack count is 0
   is never renewed; 1 is renewed (time and count back to one); more stack up to the count (at most 10): overlap type 1
   starts the time again, types 0 and 2 add a part of the time (half, a quarter, an eighth ... of it), types 1 and 2 also add
   the values of the table to the values of the buff. When the stack reaches the top and the buff has a change buff, the group
   turns into it. Another buff of the group of a high enough grade replaces the one there (06/16).
4. **Sleep and freeze** put out burns, poisons, shocks and bleeds.
5. **Time**: buffs count down on the maze clock (no timers); a buff that lasts never runs out. A tick happens when more than
   the tick time has gathered. A buff that hurts does nothing while the carrier is invincible. A buff that runs out calls its
   call-out buff.
6. **Death** takes every buff not kept on death (`DeleteType` bit 1); a player who leaves the maze leaves its buffs there
   (the next server keeps none).
7. **Stats**: a player's stats (`CombatTables.GetPlayer`) and a monster's in a hit carry the stat effects of their buffs, so
   attack, defense and critical in the damage formula are the buffed ones; the defense of a carrier is the stronger of its own
   and its buff's.
8. **Held monsters**: a stunned, paralysed, sleeping or frozen monster drops what it was doing and its mind does not run
   until the state is over.
9. **Skills**: a held player is refused every skill with 56009. The soul gauge cost is lower by the soul cost buffs and the
   soul cost bonus of the deck page; the stamina cost by the stamina buff; effect 7 skips paying the soul gauge. The cooldown
   is shorter by the cooldown buffs and the cooldown bonus of the deck (clamped to 0..100 %, none for a passive skill). The
   deck bonus is the one the player chose for the first page (the only one the client 1.4.2.3 uses), for a skill that stands in
   the deck, from its required level.

## Messages

All of them go to every member of the instance; a member who joins later gets 06/14 for every buff carried at that moment.

| Message | Layout (client reader) | When |
|---|---|---|
| 06/14 `SkillUpdateBuffResponse` | `u32 actor, u16 buff, f32 seconds left (0 for a lasting buff), u8 stacks, u32 owner` | a buff begins, is renewed or stacks |
| 06/15 `SkillBuffDeleteBroadcastResponse` | `u32 actor, u16 buff, u8 ran out` | a buff ends or is taken |
| 06/16 `SkillBuffChangeBroadcastResponse` | `u32 actor, u16 old, u16 new, f32 seconds, u8 stacks, u32 owner` | a buff of a group replaces another |
| 06/17 `SkillBuffDamageBroadcastResponse` | `u32 actor, u16 buff, i32 damage, i32 life left, u32 owner` | a buff hurts |
| 03/34 | `u8 0, u32 actor, [f32, u16]` | to a player: the stats its buffs changed; to everyone: the life a buff gave back |
| 17/12 | `u32 actor, u8 count, [f32, u16]` | a monster's stats its buffs changed, its life a buff gave back |

The original sends one more byte at the end of 06/14 (whether it is shown) that the client 1.4.2.3 does not read; it is
left out. 03/41 is sent when a buff fells a player; a monster a buff kills dies as one a hit kills (17/11, the kill counts
for the player who gave the buff, or the first player).

## Comparison with the original

| Piece | Original | Ours | |
|---|---|---|---|
| Buff of an attack's reaction | to every target reached, after the damage | same | same |
| Attacks for the attacker's side | targets by type: self, friends, party | same, in the maze | same |
| Attacks with no damage rate | no damage, buff only | same | fixed (they dealt damage) |
| Chance of a buff | success rate less the resistance stat of the effect | same | fixed (the stats exist now) |
| System type of a buff | exempts from resistance, invincibility, boss ranks | from the server's table where it agrees with the client's | approximately: 893 of 1184 rows |
| Stand-by time | waits, checks the group | same | same |
| Bundle buffs | gives the three it names | same | same |
| Capacity | 50 | 50 | same |
| Group, grade, overlap, change buff | as in the rules above | same | same |
| A change of buff in a group | abilities of the new one set only for some effects | set for every effect | fixed: a control debuff replaced by a stronger one kept holding |
| Updates of a stack | 06/14 sent twice for a renewed buff | once | kept different: one is enough for the client |
| Ticks | on the update of the object | on the maze clock (50 ms) | same, on the grid of the ticks |
| A ticking buff of non-current stats | adds the stat again at each tick | adds it once | kept different: no such row in the client's table was found to matter |
| Death | clears all but those kept on death | same, for players | same |
| Leaving a maze | clears the debuffs, keeps the rest | writes the buffs down for the next server; the next place takes what it does not allow | approximately: the debuffs stay too, and the places decide |
| Monster going home (recovery) | clears every buff, at the start, every second and at the end | same | same |
| Buffs through a relog | every buff is written with its time and put back | same, without the time away and without what gave them at the beginning | same |
| Held states | the mover cannot move or attack | the monster's mind does not run; the player's skills are refused | same in effect |
| Resistances | from stats | same | same |
| Taunt, reflection, blood, absorption, boss rates, attack rates | as their effects | same (several of one kind: the strongest, or the sum for the rates) | approximately: the original keeps the last one set |
| Skill unlock, jump counts, the percent of a stat | as their effects | kept, shown, no effect | not done: no row of the client's table needs them |
| Buffs of passive skills | applied when the skill is learned, reset or loaded, and on a condition | applied and kept matched with the skills | same |
| Buffs of cards | by condition, chance and cooldown, and the player is told | same, for the conditions the maze has | approximately |
| Buffs of combos | the row passed by the combo count | same | same, without the soul vapor |
| Auras | applied to those around at every check | same | same |
| Buffs of boosters | the stat effects of the booster | same | same |
| Buffs of the broach set of the clothing | given on wearing, in a district too, shown if it is the chosen effect | given and shown, the stats counted by the gear | kept different: no chosen effect |
| Buffs of a district | the same book | `DistrictBuffs` | same |
| Cooldown reduction | buff rate + item effect + skill option + deck bonus × 100, clamped, none for passive | buff rate + deck bonus × 100, clamped, none for passive | approximately: no item effects or skill options yet |
| Soul gauge cost | − deck bonus − item effect − soul cost buff | − deck bonus − soul cost buff | approximately |
| Stamina cost | − stamina buff | same | same |
| Free soul gauge (effect 7) | not paid, still checked | same | same |
| Deck bonus | of the deck page the skill is used from | of the first page, for a skill in the deck | the client 1.4.2.3 uses one page |

## The rest of the combat audit

- **The states that refuse a skill**: 56009 now also for a player held by a buff. The use and normal states of a skill
  (56009 / 56016 for a skill that needs the player down, countering or under a skill-unlock buff) are still not checked: the
  server has no motion state of the player.
- **Cooldown and cost reductions**: buffs and the deck bonus done; item special effects and skill options still not.
- **Continuous costs** (cost type 1): no skill of the client's table has one above zero (10 rows of type 1, all cost 0), so
  there is nothing to drain; not built.
- **The three position messages** (06/45 warp, 06/47 sync, 06/05 end of a passive move): the place is taken only when it is
  within the reach of one move (`docs/notes-fight-movement.md`); a refused place counts against the error budget once a
  second and the player stays. The warp is shown to everyone in the maze (06/46), with the place the server keeps.

## Tests

`BuffSourcesTest` (the second round of this note): a resistance holds back the chance of its state, and a system type that
resistances do not hold back sticks all the same; a buff stays only where its row allows; a passive skill gives its buff and the buff
goes when the skill does, and a death does not keep it; a passive card gives its buff by its condition and cools down; an aura gives
its buff to the friends within its reach, now and at the next check, and runs out; a combo gives its buff when the hits reach its
number; blood, reflection and a buff against bosses change what a hit does; a buff goes with the player to the next server with the
time it had left. `HelperAiTest`: a monster that gets up has its buffs cleared. `ActionFileTest`: the aura at the end of an attack
of a newer file is read. `ServerTablesTest` reads the server's buff table to its end.

`docs/agents/feature-buffs-stats/e2e_buffs_stats.py` (stand with `OW_PROLOGUE=true`, a new character in the first tutorial maze):
the speeds and resistances in the whole list at entry; a passive skill learned in the maze gives its buff (06/14) and raises the
attack the player is told; a buff of speed changes the speed (03/34); an aura is shown (06/18) and gives its buff; after a relog
through the gate the buff of the skill, the buff for anywhere, the lasting buff of the aura and the buff of speed with the time it
had left are on the player again. `e2e_district_buffs.py` (a stand without the prologue): a buff for anywhere written by the last
server is on the person at the district (06/14), one for the maze only is not, and the person leaves with the first in its document.

The earlier tests, kept:

`BuffTest`: a hit whose reaction names a buff gives it to the target once and everyone sees it; a buff changes the stat it
names and the change is undone when it ends; a second buff of a group follows its stacking rule (renew, refuse a lower
grade, turn into the change buff at the top); a burn ticks on the clock and stops at its end, and at the death of a player;
a stunned monster with a real mind does nothing until the stun ends (needs the client tables, AI scripts and action files);
the tutorial's buff given by its script reaches the player and gives life at its ticks. `SkillRulesTest`: a held player
cannot start a skill; the deck bonus and buffs shorten cooldowns and lower costs. `ActionFileTest` reads the buff and the
target type of an attack.

`docs/tools/e2e_buffs.py` (stand with `OW_PROLOGUE=true`): the tutorial's buff at entry, the rise attack's buff on the
player, a buff of stats (03/34 up, then down with 06/15 after its 10 s), the special attack's mark on the monster, a debuff of
the defences shown on the monster with its defence (17/12) and more damage taken.

Stand 5 (2026-11-09, after the rebase on `dev`): `e2e_buffs.py` 13 PASS 0 FAIL; `e2e_prologue_fight.py` 20 PASS 0 FAIL (as
before); `e2e_ai.py` (M08 21331, 40 s) 11 PASS 0 FAIL (as before); `e2e_party_maze.py` 24 PASS 0 FAIL (as before);
`e2e_maze.py` (M04 21311) 62 PASS 2 FAIL, as before (62/2), with one change to the script: its checks of the synced and the
warped place sent the player to a monster 5000 units away in one message when the first one had died, which the server now
refuses as it refuses such a move (and every later move of the script then failed), so the script walks to the monster first.
The two failures are of a monster that walks while it is aimed at: "camera decides where a thrown weapon goes" (as before) and,
in this run, "each attack comes at its own moment" (the first of the two hits missed the walking monster; it passed in the run
before the change of the script).

The damage of the debuffed monster: without `[29, 35, 18, 18, 30, 31]`, with `[31, 38, 36, 40]` (hits that are not critical
and not missed, in the order of the combo); the defence of a monster of level 1 is small next to 50 × its level, so the
difference is small.

## To check with the real client

- The icons and the times of the new buffs: a passive skill's buff, a card's buff with its message (06/21), an aura's buff, the broach
  set's buff in a district, the buffs after a relog and after a change of server (the time they have left).
- That the aura effect (06/18) shows on the carrier, and goes with the end of the aura.
- That the speed felt is the speed the stats say: a buff of speed (the stats 31 and 32) and the pace check of the server.

- That 06/14 shows the icon and the time of a buff on the player and on a monster, and 06/15 takes it away.
- That a stunned monster plays its stun animation (the client plays the animation of the buff by itself).
- That 06/17 shows the damage of a burn.
- That the tutorial's buff keeps the life and gauges full as the tutorial expects.

## Cost on the maze step

`MazeStepBenchmark` has a parameter that puts a ticking buff (the tutorial's) on every monster and the player. BenchmarkDotNet
short job: 10 monsters 1.695 µs without, 1.950 µs with; 10 monsters and 100 timers 6.088 / 6.222 µs; 100 monsters 13.895 /
16.440 µs; 100 monsters and 100 timers 18.192 / 20.802 µs. Without buffs the numbers are those of before (1.635, 6.069,
13.571, 18.154 µs in `docs/notes-combat.md`).
