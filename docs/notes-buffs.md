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

The client has no column for the original's "system type" (which exempts some buffs from resistances and from the
invincibility check); every buff is taken as the ordinary kind.

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

Anything else is kept on the carrier and shown, and does nothing more.

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

Not given yet: passive skills, items that give a buff when used, boosters (their stat effects), the combo buffs, auras,
the conditional buffs of a hit, buffs of a district.

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
| Chance of a buff | success rate less the resistance stat of the effect | success rate, no resistance | kept different: no resistance stats yet |
| System type of a buff | exempts from resistance, invincibility, boss ranks | not in the client table | kept different: every buff is the ordinary kind |
| Stand-by time | waits, checks the group | same | same |
| Bundle buffs | gives the three it names | same | same |
| Capacity | 50 | 50 | same |
| Group, grade, overlap, change buff | as in the rules above | same | same |
| A change of buff in a group | abilities of the new one set only for some effects | set for every effect | fixed: a control debuff replaced by a stronger one kept holding |
| Updates of a stack | 06/14 sent twice for a renewed buff | once | kept different: one is enough for the client |
| Ticks | on the update of the object | on the maze clock (50 ms) | same, on the grid of the ticks |
| A ticking buff of non-current stats | adds the stat again at each tick | adds it once | kept different: no such row in the client's table was found to matter |
| Death | clears all but those kept on death | same, for players | same |
| Leaving a maze | clears the debuffs, keeps the rest | clears everything | kept different: buffs do not follow the player to another server |
| Monster going home (recovery) | clears every buff | not done | not done: the AI's recovery is another area |
| Held states | the mover cannot move or attack | the monster's mind does not run; the player's skills are refused | same in effect |
| Resistances, immunity flags | from stats | not done | not done |
| Taunt, reflection, blood, absorption, boss rates, attack rates, skill unlock, jump counts | as their effects | kept, shown, no effect | not done |
| Buffs of passive skills, items, boosters, combos, auras | applied | not done | not done |
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
- **The three position messages** (06/45 warp, 06/47 sync, 06/05 end of a passive move): the place is taken only when the
  player could have run there since the last place, as for a move; a refused place counts against the error budget once a
  second and the player stays. The warp is shown to everyone in the maze (06/46), with the place the server keeps.

## Tests

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

- That 06/14 shows the icon and the time of a buff on the player and on a monster, and 06/15 takes it away.
- That a stunned monster plays its stun animation (the client plays the animation of the buff by itself).
- That 06/17 shows the damage of a burn.
- That the tutorial's buff keeps the life and gauges full as the tutorial expects.

## Cost on the maze step

`MazeStepBenchmark` has a parameter that puts a ticking buff (the tutorial's) on every monster and the player. BenchmarkDotNet
short job: 10 monsters 1.695 µs without, 1.950 µs with; 10 monsters and 100 timers 6.088 / 6.222 µs; 100 monsters 13.895 /
16.440 µs; 100 monsters and 100 timers 18.192 / 20.802 µs. Without buffs the numbers are those of before (1.635, 6.069,
13.571, 18.154 µs in `docs/notes-combat.md`).
