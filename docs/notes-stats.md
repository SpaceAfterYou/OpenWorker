# The stats of a player

What a player's stats are made of, when they are worked out again, what the client is told, and how gear wears in a fight.
Branch `feature/equipment-stats` (round 6). The client 1.4.2.3 decides every layout; the original game server decides the
rules. Earlier notes: `docs/notes-stage2.md` (the formulas of the status), `docs/notes-combat.md` (damage, gauges),
`docs/notes-buffs.md` (buffs), `docs/notes-item-setup-handlers.md` (upgrades, sockets, broaches, repairs).

## One function

`CombatTables.GetPlayer(world, player)` is the stats of a player, and the only place they are worked out:

1. `SheetOf` gathers everything the player has into a `StatSheet`: for every stat (the 77 numbers of the stat list) a number
   added and a share in percent. Gear (`GearStats.Collect`), the titles chosen for the abilities, and last the buffs carried
   write into it. A source that names a stat effect (the effect numbers of the item, title and buff tables, 1 to 93) goes
   through the map of effects (`StatEffects.TryMap`): each effect names a stat and whether it adds a number or a share. An
   effect of a current value (life, soul gauge, stamina, soul vapor left) or past 93 (special effects: damage against bosses,
   cooldowns, gold) is not a stat and writes nothing.
2. `CombatStatCalculator.Calculate(status, level, status type, sheet)` turns the status of the hero at its level and the sheet
   into the stats, by the formulas of the game.
3. The soul gauge size comes from the server's own status table, grown by its share and its number.

`PlayerStatsService.Of` and the fight (`LuaMaze`, `PlayerVitals`, `PlayerGauges`, the other person window) all read it, so
what the client is told is what the fight uses.

### The formulas

A main stat (strength, agility, intelligence, balance, vitality, luck) is its value at the level plus its number; a share of a
main stat does nothing. Every other stat is worked out from the main stats with what was added to them:

| Stat | Formula |
|---|---|
| Life | (first + vitality × growth) × (1 + share) + number |
| Physical attack (most) | (strength, or agility for a ranged hero) × 10 × rate × (1 + share) + number; the least is four fifths of it |
| Magical attack (most) | (intelligence × 10 × rate + number) × (1 + share); the least is four fifths |
| Physical defence | agility × rate × (1 + share) + number |
| Magical defence | ((balance × 0.5 + intelligence × 2) × rate + number) × (1 + share) |
| Physical accuracy | (first + intelligence × 0.125 × rate) × 10 + number |
| Magical accuracy | first + intelligence × 0.125 × rate + number |
| Physical evasion | balance × 0.025 × rate × 10 + number |
| Magical evasion | ((balance + intelligence) × 0.025 × rate + number) × (1 + share) |
| Physical critical chance, resistance | the status value + number |
| Magical critical chance | (balance × 0.05 + intelligence × 0.075) × rate + number |
| Magical critical resistance | intelligence × 0.1 × rate + number |
| Physical critical damage | physical attack × rate + strength × 10 × attack rate × rate × share / 100 + number |
| Magical critical damage | magical attack × rate × (share + 0.01): the share is added to a hundredth, as the game has it |
| Damage reductions, graze damage | the status value + number |
| Stamina | first + number; its regeneration × (1 + share) + number |
| Soul gauge | first (server table) × (1 + share) + number; a second adds the rule of the weapon + number + rule × share |

| Move speed, attack speed | 100 times the speed constant of the class (`tb_Status`, 1 for every hero) plus the shares added (effects 31 and 32): 100 is the speed of the table |
| Resistance to a state (burn, poison, shock, bleed, stun, paralysis, sleep, freeze, charm, confusion, silence, weakness) | the numbers added (effects 57 to 59 and 67 to 75) |
| Attack and resistance of an attribute (light, darkness, cool, abhor, heal, pain) | the numbers added (effects 80 to 91); held for the damage of attributes, which is not built; the client does not take stats past 60 |

The stats the calculator holds are the ones the client's stat window and the fight use (the old 30, the two speeds and the twelve
resistances). PvP and the rest are collected in the sheet but not held: nothing reads them yet. The move speed of the stats is what
the pace check of the server reads (`AbilityComponent.Speed`, kept in step by the stats service), so a buff of speed lets the player
run that much faster without being refused, and a debuff of speed slows the limit down.

## Gear

`GearStats` adds, for each piece of equipment worn (the ability equipment, an item of use type 2):

- its physical and magical attack and defence from the item table;
- its five stat options from the item table up to the first empty one, those of class 0 (the others work on a skill or a
  function, not on a stat: the column was read as a hero restriction before, which was wrong);
- the five options rolled for it when it was made;
- its upgrade: the share of `tb_Reinforce` (the last ten columns, `StatRate01..10`, in percent) for its level, of its physical
  attack and its physical defence;
- the gems in its open sockets: a gem of sub category 1 gives all its options; another gives a weapon only its resistances to
  attributes (effects 86 to 91) and other gear only its attacks of attributes (80 to 85); then the options of the socket;
- all of this times the share its endurance leaves: the penalty of `tb_Item_Repair` at the endurance it has fallen to (none
  from 80, 5 % from 65, 10 % from 50, 25 % from 35, 50 % from 5, all of it at 0), while it is below its most.

**Sets** (`tb_Item_SetItem`, the set column of the item table, now named `SetItem`): when two or more different pieces of a set
are worn, the set gives the bonus of every count from two up to the count worn (a set of four worn whole gives the bonuses of
two, three and four pieces); each bonus has four effects, those of class 0 are stats. Below two pieces nothing.

**Clothing** (the shape equipment): every broach set into it gives its stat options; a group of three places whose three
broaches are all broaches of a set (effect type 6) of one theme (the effect value) gives the buff of `Tb_Broach_Set` at the
code `10000 × theme + 1000 × (group + 1) + the three ranks as digits`: the stats of that buff (its three stats) are added for
as long as the clothing is worn.

Costume sets (`tb_Item_CostumeSet`) give gestures, not stats.

## Titles, cards, the deck, passives, the league, buffs

- The two titles chosen for the abilities add their effects of group 0; the titles owned and those shown add nothing.
- The cards of the akashic record book add nothing to the stats; the passive cards of the quick slots give buffs by chance
  (`docs/notes-buffs.md`).
- The deck bonus is a bonus of a skill (damage, cooldown, cost), not a stat (`docs/notes-buffs.md`).
- Passive skills give buffs (`docs/notes-buffs.md`); boosters of the status kind add stats through the sheet while the person has them.
- The league gives nothing to the stats in this client (its skills work on experience, prices and credit).
- Buffs are added last, through the same map; a buff of a current value is not a stat (`docs/notes-buffs.md`).

## What the client is told

| When | Messages |
|---|---|
| Entry into a district or a maze | 03/51 (origin stats: the hero at its level with nothing added, every stat the calculator holds) and 03/34 type 1 (every stat with everything on it). The greatest life is set from them. |
| Anything that changes them | 03/34 type 0 with the stats that changed; 03/51 again when the level changed the origin stats; with them the life left (stat 1) when the greatest life took it down, and the soul gauge and stamina left when their sizes did. Nothing is sent when nothing changed. |

`PlayerStatsService` keeps what the player was last told (`ShownStatsComponent`) and compares. It is asked after: putting gear
on and taking it off (`InventoryService.Move`), a level (`ExperienceService`), every answer of the work on items (an upgrade,
a gem, opening a socket, a broach, restoring: `ItemSetupAnswers`), a repair (`ItemRepairService`), a change of the titles of the
abilities (`TitleBook.AbilitiesChanged`), a change of buffs (`MazeBuffs`), and wear. The client keeps 61 stats and drops the
others with a log line, so only numbers below 61 are sent.

The greatest life follows the stats: in a maze through the vitals of the maze (`MazeHealthResize`), which tell the party; in a
city the player's life is kept full. The gauges keep what they have up to their new sizes (`PlayerGauges.Resize`).

## Endurance

The server's own `tb_Item_Endurance` (`ItemEnduranceRow`, optional: without it nothing wears) has a row per hero. Every row of
the reference: the weapon loses 1 by a chance of 25 in 10 000 each time its blow lands, and 1 by 500 in 10 000 when its carrier
falls; each piece of armour (head, shoulders, body, legs) loses 1 by 250 in 10 000 each time a blow lands on its carrier, and
5 every time its carrier falls.

`EnduranceWear` (Gameplay, items) does it: `Attacked` for every hit of a player that does damage (`LuaMaze`), `Hit` for every
monster's hit that hurts a player and leaves it standing, `Died` when a player falls to a monster or to a buff
(`LuaCreatureManager`, `MazeBuffs`). A piece wears only while it has endurance, when its row has endurance at all, and when it
does not carry option 118 (fixed endurance), in the item table or rolled. What wore is told in 18/12 (`u8 n, n × (i64 serial,
u8 endurance)`, the client sets the endurance of the item and warns at 50 and below), written by the flush, and its stats fall
as the repair table says. A repair gives the endurance back and the stats with it.

## The attack of an item, and the options of an upgrade

The client's item table has one number for the attack and one for the defence; the server's own `tb_Item` has two. For 917 of the
5544 items the two tables share, the client's number is the **least** (900 equal it, 442 equal the most, and in 17 neither), and the
most is the server's. The original rolls the attack and the defence of an item between the two when the item is made, and keeps the roll
with the item; the client 1.4.2.3 never receives it. Here (`ItemRollRow`, an optional server table, and `GearStats.Rolled`) the roll is
picked by the serial of the item, so it needs no column and an item rolls the same every time; the range is the original's, the
distribution is even. Without the server's table the least is used, as before.

The server's `tb_Item` also names, for each item, a row of `tb_Reinforce_Option`: up to five options, each from the level of upgrade it
begins at, with a class (1 a stat effect, 2 a skill option, which is not built), an effect and a value. The stat options count once the
item has reached their level (`ReinforceOptionRow`).

## Item creation

`InventoryService.Create` now gives a new item (`ItemOptionRoller`):

- its options: the row of the server's own `tb_Random_Option` (`ItemOptionRollRow`, read from the server folder only; the
  client's table of the same name has the count only) named by the option column of the item: a count between the least and
  the most of the row, then that many different options picked at random from the first listed ones, each with a value between
  its bounds. When the count is more than the options listed to pick from, the item gets none, as in the original;
- for equipment, its open sockets: those of the server's own `tb_Socket` (`SocketChanceRow`) open from the start, and up to
  three more each by its chance; without the server's table, those the client's table opens (`FixedSockets`).

## Comparison with the original

| Piece | Original | Ours | |
|---|---|---|---|
| Effects onto stats | a fixed map, number or share | the same map | same |
| Order of number and share | per stat: most grow by the share then take the number; magical attack, magical defence and magical evasion take the number first | same | same |
| Recalculation | only the stats that changed and those listed under them; the physical defence is not listed under agility | everything at every change | fixed: an agility change alone did not move the physical defence |
| Attack and defence of an item | rolled per item between a least and a most, kept with the item | rolled between the client's least and the server's most, picked by the serial | kept different: not stored; the client 1.4.2.3 does not receive the roll |
| Stat options of an item | class 0 a stat, others a function option | same | fixed (read as a hero restriction before) |
| Rolled options | at creation from the server's option table | same | fixed (none before) |
| Upgrade | share of the upgrade table × (attack and defence + title value) | share × attack and defence | same: no item titles in this client |
| Options of the upgrade level | a table of options by level | the stat options, from the server's two tables | same for the stat options; skill options not built |
| Item titles | rolled at creation, add attack and defence | not done | the client 1.4.2.3 has none |
| Gems | as above | same | same |
| Endurance on stats | penalty of the repair table | same | same |
| Sets | levels 2 to the count worn | same | same |
| Broaches and broach sets | options; the set gives a buff, shown when it is the chosen effect | options; the stats of the set's buff, and the buff shown | kept different: no choice of the shown effect |
| Titles | the ability pair | same | same |
| Akashic book, deck bonus, league | no stats | no stats | same |
| Passive skills, passive cards, boosters | buffs; a booster's status effects are stats | buffs (`docs/notes-buffs.md`); boosters through the sheet | same |
| Move and attack speed, resistances | stats of the character | stats of the character, in 03/34 | same |
| Buff of a broach set | a buff on the wearer, in a city too | a buff on the wearer shown only; the stats counted by the gear | kept different: no chosen effect |
| Messages at entry | origin list and whole list | same, stats below 61 | same for the client |
| Messages on a change | changed stats, life and gauges broadcast to the others | changed stats to the player; the party hears life through its own messages | kept different: the client's own stats are told |
| Endurance table | per hero | same | same |
| Wear triggers | a hit with a reaction, a landed blow, a death; one maze kind excluded | a hit that does damage, a death, in any maze | approximately: the excluded maze kind is not known in this client |
| Fixed endurance | option 118, and function options that stop wear | option 118 | approximately |

## Assumptions

- Every new item is made with its options, as the original makes items given by drops, shops and rewards; splitting a stack
  (which the original makes without options) does not go through creation for equipment.
- The stats of the broach set's buff last while the clothing is worn, which is what a buff that lasts does.
- The client shows the origin stats next to the final ones (its stat window reads both arrays).

## To check with the real client

- That the stat window shows the speeds and the resistances the client keeps (stats 18, 19 and 49 to 60 are below the 61 it keeps)
  and the line it prints for a rising speed; that the speed felt is the speed told (a buff of speed in a maze).
- That the icon of a broach set's buff shows in a city when the three broaches of a set are in the clothing worn.

- That the stat window shows the hero's own stats and the bonus of gear after entry (03/51 and 03/34 type 1).
- That putting a weapon on raises the attack in the stat window, and the chat line the client prints for a rising stat.
- That 18/12 lowers the endurance shown on the item and warns at 50.
- That a set's bonus and the buff of a broach set show in the stat window.

## Tests

`PlayerSpeedsTest`: the speeds are 100 plus the shares of the buffs, the resistances the numbers added, the player is told both and the
speed the pace allows follows; a booster of the status kind raises the stats while the person has it. `GearRollTest`: the attack of an
item is rolled between its least and its most, the same every time, spread over the range; the options of the upgrade count from
their level. `ServerTablesTest` reads the server's item table and the table of the upgrade options to their end.

`PlayerStatsTest` (tables made by the test): gear put on adds its stats and the player is told, taken off removes them,
nothing is sent when nothing changed; the stats sent are those the fight uses; an upgrade, a gem in an open socket and three
broaches of one theme each add what they name; a set gives its bonus at two pieces and loses it below; a hit wears the armour,
at zero its stats are gone and the player is told, a repair gives them back; a new item gets the options and the open sockets of
its rows. `TitleTest` covers the titles, `BuffTest` the buffs, `ServerTablesTest` reads the three server tables to their end.

`docs/tools/e2e_stats.py <login> <password> [maze location]` (a stand without the prologue; M08 by default, whose monsters come
for the player): a scripted client is told its origin stats and its whole list at entry, puts a weapon on and reads the attack
it adds (and four fifths of it as the least), takes it off and reads it go back, upgrades it once in the bag (no stat changes)
at an NPC that upgrades, and reads the share of the upgrade when it is worn again, puts on two pieces of armour and reads their defence, enters the maze
with the same stats, and waits among the monsters bare-handed until it falls: the armour wears on the hits and by five at the
fall (18/12).

Stand 2 (2026-11-09), before (dev) and after this work, PASS/FAIL: `e2e_prologue_fight.py` 20/0 and 20/0, `e2e_buffs.py` 12/0
and 12/0 (stand with the prologue); `e2e_maze.py` (M04 21311) 63/1 and 63/1, the one failure the known one of the thrown
weapon aimed at a walking monster; `e2e_helper.py` 88/0 and 88/0 (stand without the prologue: with it both scripts stop at the
selection of a new character, which goes to the prologue). `e2e_stats.py` 7/9 on dev (no whole list at entry, no change on
equip, no wear) and 16/0 after: the weapon adds its 1000 to the attack and its upgrade 10 more, two pieces of armour add 37
defence, in M08 the player took 53 hits and fell, the body armour wore by the hits (99, 98, 97, 92 with the fall) and both pieces
lost five at the fall. The stand logs show no error of the servers. After the rebase on the NPC services (the upgrade is served
only by an NPC that offers it, near the player) the script walks to an upgrading NPC of the city first: 17/0.
