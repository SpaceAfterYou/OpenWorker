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

1. The place of the request: taken when it is a place and within the reach of one move (`MovementPace`, as for a move:
   `docs/notes-fight-movement.md`); otherwise the skill starts where the server has the player, and a refused place counts
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
| Place of the skill | replaced by the area's when invalid | replaced by the server's when invalid or out of the reach of a move | stricter: a place out of reach is not taken |
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
- ~~The positions of 06/45, 06/47 and 06/05 (skills that move the player) are taken as sent.~~ Done as the original does it: `docs/notes-fight-movement.md`.
- ~~Endurance is not worn.~~ Done: gear wears on blows given, blows taken and falls (`docs/notes-stats.md`, "Endurance").

## What a blow does to the one it hits

The server works out the push, the throw and the time a monster is lost to its reaction, and sends the result in the hit
(06/13); no client reports anything about a monster, and the debug trace of the original (05/0B) is never sent. Every client
plays the reaction from the same blow with the same rules, so the server keeps the place of the monster by the same rules too:
the next blow and the next step of the mind judge the monster where the clients show it.

**The numbers** come from the attack event of the attacker's action file (`AttackReaction`): the distance, the height (and the
height in the air), the speed, the way (`Arrow`), the turn, the grade, the weight flag and the hardness of the blow (0 weak, 1
plain, 3 and 4 strong), and from three columns of the monster table: the weight rank, what the monster cannot be thrown by
and whether it has a motion for the break of its armor. `ExtraMove.Of` is the one rule for a blow of a player on a monster and
of a monster on a player:

- nothing moves a target that cannot be thrown, that the attacker holds, by an event that deals no damage, or by an event that
  has no hardness, distance or height;
- the push is `-(distance + roll)` forward (`roll` between -20 and 20, none for the straight way) and the height, or the height
  meant for the air when the target is in it; a heavy attacker throws a light target 35 % farther (rank gap 2 or more), a light
  one 35 % less far (gap -2 or -3) or not at all (gap -4 or less), only for the throws that lift, and only when the event or the
  stand type of the target asks for weight; a guard halves it and a charge adds;
- a throw that lifts (types 2, 3, 4) is told in the frame of the attacker, which every client turns by the yaw of the blow;
  any other reaction is turned here by the yaw of the attacker (with a random turn of 7.5 degrees at most for the usual way,
  away from the burst for the second way) and told as the place the monster will be at, a radius before a wall.

**What the monster does** (`MonsterReaction`, one per monster body):

| Reaction | The monster is lost to it for |
|---|---|
| Flinch (type 1) | the animation of the flinch, and a slide of the push in a tenth of a second (done in 0.3 s) |
| Knock back (type 5) | the two animations of the push, and the slide |
| Stun (type 6) | the animation of the stun (a stun buff holds the mind as before) |
| Launch and knock down (types 2, 3, 4) | the air (the height and the gravity say how long, then a hang of 0.3 s; 0.2 s of delay first for type 3), the landing animation, lying for 1.0 to 3.0 s, the getting up |
| Armor break | the animations of the break and the time the group gives the armor (3 s without one) |

Flights go along the yaw of the blow with a speed that falls as the distance is covered, one step of the navigation mesh at a time;
a wall stops them a radius before it. A monster whose file has no lying or getting up animation does not lie. A weak blow
(hardness 0) shows no animation and costs no time; a blow on a monster whose armor holds, or that nothing can hurt, shows
one only if it is hard enough. A monster fixed in its place (stand type 2 and 3) flinches without sliding and keeps attacking
through a flinch. A reaction that shows ends the attack the monster was making (its later blows and shots never come) and its
walk, and the mind of the monster does not start an attack or a walk until the reaction is over; the walk, stop and idle
messages (05/02, 05/04, 05/0C) of a monster in a reaction are not sent.

**What the clients are told**, in this order for a blow: the hit slow of a weak blow that rolled (05/12), then the hit with the
throw, the fly speed, the defense type and the gauge of every target (06/13), then the buffs. Later: where a monster
landed (06/49), where a wall cut its flight short (06/48), where a player in the way stopped a slide (05/13), and that it
begins to get up (17/33). The gauge of a super armor (17/53) goes out when the armor is whole again, not with every hit.

**Super armor.** The gauge is the share of the life of the monster that the group table gives (35 % for most rows; the share
of the count of players of the maze, `PartyRevision`). A hit takes its damage by the share of the skill (`SuperArmorRules.Damage`);
the armor breaks when nothing is left or at once for a hard blow, the monster takes half as much again and reacts like any other
(or stands helpless through the break animations, if its table says it has them), and the armor is whole again after the
animations of the break and the recovery time of the group. The mind is told when it is back
(`MonsterBody.OnSuperArmorRestored`, empty for now: the skill a script keeps for this moment is cast from there).

**Comparison with the original**

| Piece | Original | Ours | |
|---|---|---|---|
| Who simulates the push of a monster | the server, every client replays it | same | same |
| The rule for the vector, weights, guard, charge | as above | as above | same |
| A throw with no height | plays the animation of the throw, does not fly, and pushes with the vector as it is | the same, with the vector turned by the yaw first | fixed |
| Place told for a push that keeps the monster on its feet | the destination, or its own place if nothing moves it | same | same |
| Fly speed in the hit | the speed the monster flies at, also when it does not fly | same | same |
| Armor gauge and defense type in the hit | the values after the hit | same | same |
| The gauge told with every hit (17/53) | only when the armor is whole again | same | fixed (we sent it with every hit) |
| Armor break time | animations and recovery time | same | same |
| A hit on a monster that lies or hangs in the air | animations for the hit (air hit, down hit) | the timeline is not changed by a flinch; a launch starts the throw again | simplified |
| A slide stopped by a player | 05/13 on every step | the slide stops and 05/13 goes once | kept different |
| A flight over a pit | allowed | stops at the edge | kept different |
| Slow of a hit on the speed of a flight | scales it | not applied (05/12 is sent) | kept different |
| A killed monster | reacts as it dies | removed at once; the hit still carries the throw | kept different |
| The push of a player | the server moves the player and the report of the client overwrites it | only told; the report of the client is taken as before | kept different |
| Armor frames of a skill (a defense type given by an animation or a stretch of one) | read from the action file | read, see "Armor of the moment" | same |
| Parts of a monster (17/54) | hit part from the bones | not done | not done: see "What is not done and why" |
| Weight rank of a player | 5, the default of every body | 5 | fixed (it was 0) |
| A helper hit by a monster | thrown like any monster | thrown, the throw told in the hit | fixed |

Not done: the reaction rate of the hit slow, the break of the armor of a player.

## Armor of the moment

The defense type a body takes a hit with is not only what its table says. Every animation of an action file names one (the fifth value
of its header): 4 says nothing and the defense stays the table's (and the buffs'); 0 makes it plain, taking even a super armor off; 2 makes
it armored, 3 invincible, 5 hurt but never moved (a new defense type, `NoReaction`, that shows no reaction to any blow, hard or not). A
`DefenseTypeTrigger` event of the animation (a stretch in seconds) says one for its time, unless the super armor of the monster is broken.
Invincible beats armored beats plain beats nothing (`DefenseFrames`).

- **A player** takes it from the animation of the skill it plays (`SkillPlayback.Playing`): every skill of the heroes armors it, an
  evade or a roll makes it invincible for their length (a blow in them deals nothing and moves nothing), walking and standing say
  nothing. The monster's blow tells the client the defense type in 06/13, and a player armored or invincible is thrown only by a blow hard
  enough (`PlayerThrow`).
- **A monster** takes it from the animations of the skill it is playing (`MonsterBody.FrameDefense`); outside a skill its defense is its
  table's, as before. The reactions to hits (flinch, fall, get up) keep their own timeline in `MonsterReaction`.

## Carrying (05/34, 05/35)

Not the grab of a boss: three skills of Haru (First Blade, Pierce Step, Spin Cutter) and one or two of each other hero carry away what
stands in front of them for a short time. The event names the time (0.2 to 0.3 s), the distance (50 to 150), the angle (100 to 150 degrees)
and a weight rank. From the moment the event comes, at every step of the maze, the monsters in that area before the player that live,
do not lie, and are not carried yet are looked at: one of a weight rank at or above the limit, or a fixed one (stand type 2 or 3), is not
carried, and the player stops against it (05/13 with the place the server has); any other is told to the clients with 05/34 {carrier,
carried, direction, distance} (the direction is from the player to the monster, the distance their distance now), stops, and from then on
stands before the player the way it did, a body of each and the distance added, until the time is over or the ground stops it. It is let
go with 05/04 and 05/35. While carried a monster neither thinks nor fights. The client's own report of where the carried monster ended
(05/36) is not used; the server follows the player.

## Special effects of gear

The tables name effects from 100 on that are not stats: a share of damage to a kind of monster, of cooldown, of cost. Gear, titles and
buffs write them into the stat sheet (`StatSheet.Apply`), the stats carry them (`CombatStats.Special`), and these count:

| Effect | What it does |
|---|---|
| 100 | the cooldown of every skill but a passive one is shorter by this share (added to the buffs' and the deck's, at most 100 %) |
| 103 to 108 | the damage to a plain monster / a named one, a boss or a raid boss (rank 3 to 5) / a monster that fights close by / one that shoots (none of this client does) / one in the air / one lying down |
| 126 to 133, 151, 153 | the damage to the monsters of a faction (faction minus 6 is 0, 1, 2, 3, 10, 11, 12, 13, 22, 23) |
| 109 to 112, 134 to 141, 152, 154 | the same taken away from the damage a monster of that kind does to the player |
| 113 | the damage to the super armor of a monster is greater by this share |
| 114 | the soul gauge cost of a skill is lower by this share |
| 121, 122 | life and soul gauge given back for each monster killed |
| 142 | the damage of a blow that only grazed is lower by this share |

Each share takes its part of what the one before it left, after the defense of the target and before the damage rate of the event, the
skill and the rest, as in the original. Not counted: 101 and 102 (battle between players), 115, 116 and 117 (more experience, gold and soul
vapor from a kill, which the reward code of the maze pays), 118 to 120, 123 to 125 (a buff to give, a reflection), 143 and 144 (less damage
while in the air or lying: the server does not follow a player on the ground), 145 to 148, 149 and 150 (speed in a district or a maze: the
pace check only counts a move that is too fast), and the rates of bosses and of every blow that a buff gives. No client message carries
the special effects.

## Skills and the state of a player

A player thrown into the air by a blow that shows its reaction cannot start a skill whose use state is the plain one (most skills, and the
evade), until the flight is over, which is counted from the height of the throw (up and down again at least, so that a skill of a player
who has already landed is never refused): the answer is 56016, or 56009 for a skill that asks for a motion, a counter hit or a buff.
Skills for any state (use state 1) and the ones whose normal state allows a hit go through. The server does not follow the player once it
is on the ground, and a skill for the ground only (the roll, the rise attack) or for a counter hit is not checked.

## What is not done and why

- **Parts of a monster (17/54).** Only six monsters of this client are of the kind that has parts (the Deus Ex Machina and its two halves,
  normal and hard). A part has its own life, a share of the monster's, and only a blow that touches it hurts it; the monster's own life is
  then not touched, and when parts are broken the monster is changed into another one or spawns one. Which part a blow touched comes from the
  bones of the model at the moment of the blow, which needs the skeleton of the animation; the server has none, so no hit has a part.
- **The charge of a skill.** The event that makes a skill grow with the key held is in no action file of this client.
- **Continuous skill costs.** No skill of this client's table has a cost of type 1 above zero.
- **Skill options on cooldown and cost.** An item option that names a skill (class 2 and up in the original) has no source in this
  client's item table, which has classes 0 and 1 only.
- **Motion states of a player on the ground**, see "Skills and the state of a player".

## Tests

`SkillRulesTest`: a skill not learned or not in the deck is refused and takes nothing; a cooldown refuses until it is over;
a cost that cannot be paid is refused and takes nothing; the soul gauge fills by hits and is spent by its skill; learning goes
one level after the other and costs its points; a level gives its skill points (and a catch-up gives them once).
`ExtraMoveTest`, `MonsterReactionTest` and `SuperArmorRulesTest` hold the rules of a throw, of the time a monster is lost to a reaction
and of the armor, and `ActionFileTest` the reaction block of an event.
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

## To check with the real client (grabs, armor, carrying)

- A boss that grabs (the Asphalt Golem in the fourth episode of the Concrete Jungle, level 17): the player is held, flies to the place,
  takes the blow and is put down there; nothing else about it moves wrongly.
- Pierce Step on a monster in front: the monster is carried by the player for a quarter of a second and let go; on a heavy one the player
  stops.
- A hero in the middle of a skill is not thrown by a plain blow and an evade is not hurt at all.
- A monster whose attack animation says it is armored does not flinch through the attack.

## To check with the real client (blows)

- A plain hit slides a monster 100 to 140 units away in a fraction of a second, and the next hit lands where the monster is.
- A knock down (the fifth plain attack): the monster flies about 430 units and 49 up, hangs, lands where the server says
  (06/49 puts it there), lies 1 to 3 s and gets up; it does not walk or attack before.
- Power Slash on a small monster kills it; on a heavy one (a BlankPuppet) the blow is a vector of zero and, by the rules of the
  original, the monster plays the animations of a knock down without flying: does it lie down, or only flinch?
- A launch with no height (an air attack of height 0): what the client plays when the hit says type 2 and the vector is zero.
- The Discus (super armor): the gauge falls with each hit and it does not flinch until the armor breaks; the break plays its
  animations (about 4.5 s), then the armor is whole again.
- A monster without lying and getting up animations (Bloodyball, Glouton) knocked down.
- A heavy monster's blow lifts a player 35 % farther; the push of a player is smooth and the server does not pull the player back.

## Cost on the maze step

`MazeStepBenchmark` now fills and steps the gauges of its player as the server does (BenchmarkDotNet short job, Ryzen 9
5950X): 10 monsters 1.635 µs (24 B), 10 monsters and 100 timers 6.069 µs, 100 monsters 13.571 µs (166 B), 100 monsters and
100 timers 18.154 µs; the phase 5 figures were 1.660, 5.477, 14.801 and 17.653 µs. The gauges cost a read and a write of a
component per member and step, and the stats of the player once a second.
