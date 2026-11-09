# Combat and skills

What a new character must do and survive to play through the prologue, how the server decides it, and how that compares with
the original game server. The client 1.4.2.3 decides every wire layout; the original server decides the rules. Earlier
notes: `docs/notes-stage2.md` (health, damage, sectors), `docs/notes-ai.md` (monsters), `docs/notes-prologue.md` (the
flow of the prologue).

Buffs and debuffs: `docs/notes-buffs.md` (feature/skill-buffs). Closed from the list below: the buffs of skills (the reaction buff is read and given), the tutorial's buff 10000 (it is in the client's table and is given), the skill state of a player held by a buff (56009), cooldown and cost reductions of buffs and the deck bonus, the three position messages; continuous costs need nothing (no skill of the client's table has one above zero).

## What the tutorial asks of the player

| Mechanic | Where in the prologue | The server now |
|---|---|---|
| The skills, points and deck of the character reach the client | the character goes from the gate straight into `T01_TUTORIAL`, without a district | the maze loads the skill book at entry and sends 06/70, as the district does |
| Plain attacks and their combo (`B_N_Attack_01..05`) | first fight of T01, every fight after | allowed without learning or deck (skills of type 1 or 2 with a passive type, as the original); hits from the action file, damage by the formula below |
| The first active skills (Haru: First Blade, Pierce Step, Spin Cutter) | the fights; T02 asks for Spin Cutter (`OnActionSkill` → condition 1000132) | must be learned and in the deck; a new character has its default skills learned and its active ones in the first positions of its deck |
| The soul gauge: full at the start, filled by hits, spent by skills | the tutorial movie of the soul gauge, every skill | the size from the server's status table, filled a second at a time by the rule of the hero's weapon, filled by every hit of a skill that absorbs, spent by a skill whose cost is the soul gauge |
| Evade and dash, paid with stamina | the tutorial movie of evade | allowed for every hero, the stamina cost taken; stamina fills a second at a time |
| The special attack (`B_S_Attack_N_01`, passive type 2) | the tutorial movie | allowed without a deck, its 10 s cooldown counted |
| Cooldowns | every skill | per cooldown group and the global cooldown of the skill table |
| Monsters attack back, the player's health, death and revival | both fights | as `docs/notes-ai.md`; a revival fills the gauges too |
| Waves and sector clears by kills | T01: 4 monsters, then 8 more when 4 are dead; T02: what its talk spawns | as `docs/notes-stage2.md` |
| Experience and level, skill points of a level | every kill | experience as before; a level reached gives the skill points of the server's table |

## The checks of a skill (06/08), in order

1. The place of the request: taken when it is a place and the player could have run there since the last move
   (`MovementPace`, as for a move); otherwise the skill starts where the server has the player, and a refused place counts
   against the error budget once a second. An angle that is not one is replaced by the facing of the player.
2. The skill is in the table, owned and usable without a deck or in the deck: the plain attacks and the common skill, skills
   of type 1 or 2 with a passive type (attacks, special attack, evade, dash, revival), skill types 4, 5, 6, 7 and 9 and skills
   usable in any state are owned and need no deck; any other skill must be learned (itself, or a level of its group with the
   same divergence group) and stand in a position of the deck (or its swap skill). Otherwise 56001.
3. The level of the skill (56003), the hero of the skill (56002), the player alive (56009).
4. The cost, by the cost attribute of the skill: health (the cost must stay below it, 56004), soul gauge (56005), stamina
   (56006), soul vapor (56007). A cost type 2 is a percentage of the current value; a cost under one point is one point.
5. The cooldown of the group and the global cooldown (56010).

A refusal is the answer 06/09 with the code (the client shows it and cancels the skill), and nothing starts: no cost, no
cooldown, no hit. An allowed skill takes its cost (03/34 tells the player), starts its cooldowns, answers 06/09 with 0 and is
played. A hero whose soul gauge rests after a skill (Jin, rule 5 of the server's soul gauge table) does not fill it for the
delay of the table after a skill that is not a plain move.

Projectiles (06/31) are fired only by the skill the player is playing: a skill that was refused fires nothing.

## Damage of a hit

As in `docs/notes-stage2.md`, with one term more: a monster whose super armor is broken (it has a gauge and it is empty) takes
half as much again, after the defence and before the rate of the attack and the skill. The physical critical chance of the
monster table is no longer added to a monster: the original adds the magical one and not the physical one.

## Gauges

- Soul gauge size: `SG_First_Value` of the server's own `tb_Status` (the client's table lacks the column): 200 for most
  heroes, 10 for Erwin, 100 for Stella. Without the server table the gauge is not counted and costs are not checked.
- Soul gauge per second, with the soul gauge regeneration of the status as the constant: the sword (and the heroes after the
  fifth) `(max + 100) / 200 × c`; the gun `max / 400 × c`; Lily `c`; Jin `c × max × 0.05 + 10 + level × 0.2`; Stella
  `(max / 500 + (max − current) / 150) × c`.
- A hit gives the soul gauge of the attacker `absorption of the skill × the soul gauge absorption rate of the status of the
  target`, while the gauge is not full; the hits of one attack are told once.
- Stamina: size `ST_First` of the status (100), `ST_Reg` per second (5); a use of stamina starts its second again.
- The gauges are full when a player enters a maze and when it revives; the sizes and regenerations go with the stat list.

## Skill points and learning

- The points a level gives come from the server's own `tb_Levelup_Point` (the client's has none): nothing for levels 1 to 3,
  then 1, 3, 1, 1, 1, 1, 5 at level 10 and so on (57 by level 25). Every level reached gives its points (06/73); a person
  loaded with fewer points than its level has earned gets the difference once, so a level reached on a server that did not
  hold the skills is not lost. A new character starts with none, and with the default skills of its hero. Without the server
  table a new character starts with 32 points, 16 free, as before, and levels give none.
- Learning (06/71), every refusal the error answer 56012 and nothing changed: the next level of a skill one after the other, a
  new skill from its first level; the required level; the hero; the free points (column 8 of the skill table, which was read
  from the maximum level before); the points spent in all; the required skill. The new level replaces the old one in the book
  and in the deck.
- Divergence (06/77): one of the divergence groups of the skill, its level and points, and the next level of the one chosen.
- Deck (06/75): positions 0 to 5, no more skills in a position than the deck has slots, every skill learned; otherwise nothing
  changes and the answer is false. A new character has three slots (the original's start); one more (06/76) up to four,
  56013 past that; the count the client sends is not taken.
- Reset (06/72): the default skills stay, every point comes back, the decks empty.

## The tables

`tb_Skill` columns named by matching the client's columns with the original's structure over the rows both have: the group,
levels, use position and hero, required level, points and spent points, maximum level, required skill, next level, divergence
groups, skip priority, skill type, passive value, cost attribute, cost and cost type, direction, use and normal state. Two
names were wrong: column 9 is the maximum level, not the points of a skill; column 54 is whether the cooldown is saved, and the
bullet sync type is column 59 (188 skills were taken as flown by the client alone and not announced with 06/32; 5 are).

Server tables read from `res-server/` (optional; a missing one turns its feature off): `tb_Levelup_Point` (points per level),
`tb_Status` (soul gauge size), `tb_Soul_Guage` (when the soul gauge fills).

## Comparison with the original

| Piece | Original | Ours | |
|---|---|---|---|
| Skill owned, in the deck | as above | same | same |
| Order of the checks | owned, deck, level, hero, awakening, can attack, cost, cooldown | owned, deck, level, hero, alive, cost, cooldown | same, without awakening (not kept) |
| Skill state (motion, down, counter hit) | 56009 / 56016 by use and normal state | not checked | kept different: the server has no motion state of the player |
| Place of the skill | replaced by the area's when invalid | replaced by the server's when invalid or out of pace | stricter: a place out of reach is not taken |
| Cooldown reduction | rates of items, options, deck bonus | none | not done: no source of those rates yet |
| Cost reduction | deck bonus, items, soul cost down | none | not done: as above |
| Continuous costs (cost type 1) | drained each second during the skill | taken once | kept different: no skill of the tutorial has one |
| Soul gauge | as above, buff conditions of the soul gauge table | as above, without the buff of Lily's gauge | approximately |
| Stamina regeneration | each second, off during a continuous stamina cost | each second | same |
| Gauges at entry | full on the first entry and in a district, kept from the character otherwise | full at every maze entry | kept different: the gauges are not saved |
| Damage | `docs/notes-stage2.md` and the broken super armor | same; item special effects, flying and down targets, attributes, PvP not done | approximately |
| Monster stats | adds the magical critical chance of the table, not the physical one | same now | fixed |
| Learning | as above, a new group at any level | as above, a new group from its first level | stricter: the client learns one level after the other |
| Level-up points | `Give_Skill_Point` at each level | same, plus the catch-up at load | same |
| New character | deck from the database procedure (not visible) | active default skills in the first positions | assumption |
| Deck page | active page of five | positions of the first page | the client 1.4.2.3 sends positions 0 to 5 only |
| Buffs of skills | the buff of the reaction of an attack, success rate, standby, ticks, end | not done | the action file's reaction buff is not read yet |

## Not done

- Buffs and debuffs of skills (`tb_Buff`): the buff named by the reaction of an attack trigger is not parsed from the action
  files, so no skill applies one; the tutorial's buff 10000 is not in the client's table.
- Charging, key press, sub input and target change are answered as before (no checks of their own); continuous skill costs.
- ~~The stats of an item do not change with its upgrade or gems; stamina and soul gauge of gear, titles and akashic cards are
  not added.~~ Done: `docs/notes-stats.md` (feature/equipment-stats). Akashic cards give no stats in the original either.
- The positions of 06/45, 06/47 and 06/05 (skills that move the player) are taken as sent, as before.
- ~~Endurance is not worn.~~ Done: gear wears on blows given, blows taken and falls (`docs/notes-stats.md`, "Endurance").

## Tests

`SkillRulesTest`: a skill not learned or not in the deck is refused and takes nothing; a cooldown refuses until it is over;
a cost that cannot be paid is refused and takes nothing; the soul gauge fills by hits and is spent by its skill; learning goes
one level after the other and costs its points; a level gives its skill points (and a catch-up gives them once).
`DamageCalculatorTest` has one worked example per term, the broken super armor among them. `ServerTablesTest` reads the three
server tables to their trailer. `AttackAreaTest` holds the areas of the hit events.

`docs/tools/e2e_prologue_fight.py <e2e_client.py> <login> <password>` (stand with `OW_PROLOGUE=true`) fights through both
tutorial mazes as a client does: a new Haru gets its skills (06/70) and its gauges (03/34) at the entry of T01; a skill it has
not learned is refused (56001) and a skill used again before its cooldown is over is refused (56010) and costs nothing; it
walks to the monsters at 450 units a second and fights the two waves of T01 (12 monsters) with its combo and its three skills
as their soul gauge and cooldowns allow, with no refusal and no move refused by the speed check; the monsters hit back; the
soul gauge falls by the cost of each skill and fills by time and hits; the kills give experience; then T02 spawns its fight
on the talk 910601, it is fought with the skill the tutorial asks for, and the way out leads to the district. 20 checks.

`docs/tools/e2e_maze.py` walks at a legal pace too (it used to jump to its targets, which the speed check refuses): it rises
to level 51 with `/exp` and learns the flying skill it fires in the district, starts every projectile skill before it fires
its shots, walks into the quest box before it reports it, and a skill sent from a place the player cannot have reached is
checked to start where the player is. Two checks of the projectiles (the first shot and the thrown weapon) still fail, as
they failed before this work: the target monster walks while the shot is aimed at the place it was last reported.

## To check with the real client

- That a new character in `T01_TUTORIAL` sees its skills in the deck and can use Spin Cutter in `T02_TUTORIAL`.
- That the soul gauge bar follows 03/34 and empties by the cost of a skill.
- That a refused skill (06/09 with a code) shows its message and the character stays usable.
- That the evade spends stamina as the bar shows.

## Cost on the maze step

`MazeStepBenchmark` now fills and steps the gauges of its player as the server does (BenchmarkDotNet short job, Ryzen 9
5950X): 10 monsters 1.635 µs (24 B), 10 monsters and 100 timers 6.069 µs, 100 monsters 13.571 µs (166 B), 100 monsters and
100 timers 18.154 µs; the phase 5 figures were 1.660, 5.477, 14.801 and 17.653 µs. The gauges cost a read and a write of a
component per member and step, and the stats of the player once a second.
