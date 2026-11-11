# Monster AI: how a monster thinks, what it tells the clients, what it does when it is hurt

Code: `OpenWorker.Gameplay/Ai` (the machine and its profile), `OpenWorker.Lua/AiScriptLoader.cs` (reads the scripts), `OpenWorker.Lua/Ai`
(the body of a monster: it walks, turns, attacks and tells the clients), `OpenWorker.Lua/Managers/LuaCreatureManager.cs` (the monsters of
a maze, hits on them, the players they fight). Tests: `AiMachineTest` (and `AiMachineTest.Damage`), `AiScriptLoaderTest`, `SkillAggroTest`,
`ActionFileTest`, `ProjectileFlightTest`, `MonsterWireFormatTest`, `HelperAiTest`. The helpers of the players run on the same machine
(`notes-helpers.md`). How a hit moves and staggers a monster is `notes-combat.md`.

## 1. Where the behaviour of a monster comes from

**The scripts are in the client.** `data13.v` holds `Scripts/AI`, about 1167 Lua files; the stand extracts them to `client_scripts/AI`
(`stand_prepare.py`). A script is a state machine written as calls on `ai:` (the comments are in CP949, the files start with a BOM).
Without the folder the monsters have no AI and stand still (the log says "No script of the AI named ...").

A monster row (`tb_Monster`, client) gives its action file, the speeds (walk, run, turn, gaze), the script name, the stand type (2 and 3:
never walks, 1: walks only in a fight) and ten skill slots. The skills (`tb_Skill`) give range, angle, cooldown group, cooldown, global
cooldown and the animations; the monster's action file gives the attacks and when they land.

**Tables only the server has** (`res-server`, optional): the hate of a skill (kind and value, `SkillAggroRow`, the server's `tb_Skill`).
Neither the client's skill table nor its monster table has the columns of the hate, of the skill inclination of a monster, or of the bits
that say whom a summoned monster fights. Without the server table the hate counts the damage alone.

## 2. The machine

A monster thinks every `AiCheckTime` (0.1 s in most scripts). It looks at the transitions of its state in order; a transition is the AND of
its conditions and the first that holds is taken; if none holds the state does its own update. A state with a time ends when the time is
over and picks the next state by the weights of the script. The conditions compare a variable (target distance and direction, health,
timers, cooldowns, counters, `Random`, counts of enemies and users within a distance and so on) with a number or a range. Fuzzy logic
(`RegisterFuzzyConditions`, 24 scripts) is not computed: the weights are the script's.

States: wait, patrol, select action, battle wait, battle move, gaze, move, attack, ten attack-skill states, ten common-action states,
return, suicide (it asks the maze to remove the monster; a script of the maze decides), recovery, trace move, request help, runaway and
the protection states. A state a script did not register cannot be entered. **Trace move** follows the owner of a helper; a monster
without an owner goes back to waiting (no script of this client uses the state).

**Skills.** A group names up to three slots; the order is by position, random or round. A skill is skipped while its cooldown group
runs or while its own conditions fail; when none fits, the first slot is used. A skill starts when the target is in `[RangeMin - 30,
RangeMax + target radius]`; farther away the monster runs to the target and finds its path again every 0.3 s, and gives up after 30 s.

**Sight and hate.** A monster that is "first attacker" looks for the nearest enemy within its sight every period. An enemy it sees gets
a hate of 1; every blow adds hate (section 4). The monster looks at the one it hates most.

## 3. What a monster tells the clients

All of these are checked against the receivers of the client (`MonsterWireFormatTest`).

| Message | When we send it |
|---|---|
| 05/02 move | the monster sets off, then every 0.6 s while it walks |
| 05/04 stop | it arrives or is stopped |
| 05/08 battle pose | the fight pose is switched on or off |
| 05/0A gaze | it starts or stops circling its target |
| 05/14 direction | it turns where it stands (towards its target) |
| 05/0C animation | a common action of the script, with the number of the animation in the action file; an idle animation of a monster out of battle; the pause of 0.3 s before a walk (below) |
| 05/0F animations | a spawn box names an animation for its monsters (`m_ChangeSpawnAction`, set on most boxes): sent with the monsters, one entry each, delay 0; a player who comes in later is sent the animations the monsters are still in the middle of |
| 06/10, 06/13 | a skill starts, a blow of it lands |
| 05/20, 05/15 | a skill that grabs has taken a player and says where it will be put down; the player is put down there, or freed where it stands (grabs, below) |
| 17/22 target | the monster takes another enemy (-1: none) |
| 17/32 invisible | an event of a skill animation hides the monster (flags, kind, value), and again, with nothing, when it shows itself |
| 17/34 escape | the monster has escaped: its defense type now and for how long (zero for a skill) |
| 17/12 stats | a helper or returning monster heals |
| 17/53 super armor | knockback's (`notes-combat.md`) |

Not sent: 05/0B (a debug dump of the AI that the original sends to a game master's client only), 05/0D (a few small switches of a mover
that only game master commands and cutscenes use), 05/21 (the relay of a player's own turn), 17/33 (the monster gets up from the ground:
it goes with the reaction, `notes-combat.md`). 05/34 and 05/35 are not the monster's: a skill of a player sends them (`notes-combat.md`,
"Carrying").

**Turning before a walk.** A monster that stands and sets off more than 45 degrees away from the way it looks does not set off at once. In
battle, with the animations of a turn (`B_Turn_L`, `B_Turn_R`, and `B_Big_Turn_*` beyond a quarter of a turn), it turns on the spot: 05/0C
with the animation, no walk until it is over, then 05/02. Without them, or out of battle, it turns at once and tells 05/0C with a delay of
0.3 s, then 05/02 straight away; the server keeps it still for that time. A monster on its way that finds its path again is not stopped by it.

**Idle animations.** A monster out of battle that has stood or done nothing else for 5 s plays its idle animation (`N_Idle_01`) with a chance
of one in five at each look, and tells 05/0C with no delay. The original gives every monster these two numbers; a script may change them and
none of this client does.

**Grabs.** A skill of the kind the table calls type 8 plays its start, then the animation of holding if the start took a player, or the one of
the miss if not. Its start has an attack event of the grabbing kind: it hurts like any blow, except that a player nothing can hurt (an
invincible one) is not hit at all, and the nearest of the others that no other monster holds is taken, one at a time: 05/20 tells where it
will be put down (the drop place of the event, from the monster, turned the way it looks, no farther than the ground goes), then the hit
(06/13). The monster's other blows do not land while it holds. The animation of holding names the moment it lets go and for how long the
player flies; at that time the blow named by the event lands on the player wherever the area is, and 05/15 puts it down at the place with
the flag that makes the client move it; if the skill ends, or the monster is hurt or dies, with the player still held, 05/15 frees it where it
stands. What the grabbed player may do is not checked by the server (as the original). The AI uses the skill only if its script has a group
that names its slot.

**Spawn animation.** A monster with a spawn animation that does not repeat does nothing until the animation is over. Animations that
repeat (standing) are only shown.

**Hiding.** An animation event can hide a monster for a time, until its life is down to a share, or until the monsters of some tables are dead
(looked at once a second). Hidden, it cannot be hit or hit by a projectile, and thinks only if the event keeps its mind on. The kind that
waits for the monsters it summoned is not followed: monsters do not summon yet.

## 4. When a monster is hurt

A blow that lands goes through the machine in this order: it adds to the points of the escape; a monster with no enemy takes the one who
hurt it and enters select-action; the hate of the attacker grows; then, for a blow with damage, the counters (`PreSkillDamageCount`,
`HitCount`) grow, the group is called if the monster has an enemy, and the last-damage time becomes zero.

**Hate of a blow.** `(damage + base) * 0.0002`, where `base` is `damage + value` for a skill of kind 0 and `damage * (1 + value / 100)`
for another kind (the server's table; a skill made to draw hate has kind 2 and value 1000: twelve times the damage in all), multiplied by
`1 + 1.5 / 1.4 / 1.3 / 1.2` for the player whose level is the lowest of the group / the next / the third / the fourth (a player past
the fourth earns none). Poison and burns add `damage * 0.0001`. Kept different from the original: the monster's own inclination for the
kind of skill (a tenth of the hate for two thirds of the monsters) is not used, the client has no column for it.

**The look.** When the hate has changed and `AiCheckTime` of `SetDmgAggressive` (1 s) has passed, and after a sighting, the monster
takes the player with the most hate as its enemy. At every look the hate of everyone decays with time, is halved, and is lost when it is
no more than the reset time of `SetDmgAggroReset` (1 to 20 s, always the case in practice): the monster goes for whoever did most since the last
look. The percent of `SetDmgAggressive` is not read. When the enemy has given no hate for the reset time, or is farther than the reset
distance, its hate is forgotten and the monster looks again; with `SetEnableClearTarget` it may be left without an enemy.

**Taunt.** The buff that turns a monster on its giver holds the monster on that player for as long as the buff lasts and the player is
there; when the buff ends hate decides again.

**Group.** A spawn box with an aggro group gives each of its monsters a group, a distance and a count. When a monster of a group has an
enemy and is hurt, or has just seen one, the monsters of the group within the distance that have no enemy take the same enemy (hate 1,
select-action), at most "count" of them, the caller included. A monster calls its group once.

**Escape.** `SetEscapeInfo(points, chance, ..., reset time, kind, value)`: a skill adds a point, a projectile half, a trap a fifth; from
the first time two blows land within half a second of each other, every blow adds a fifth (the original never resets this). At the
points, the next start is a random half and the monster escapes with `chance` in ten thousand: kind 0 gives it the super armor defense
for `value` ms (unless its armor is broken: then the armor mends in its own time) and ends its flinch, kind 1 invincibility for `value` ms, kind 2 plays the skill `value` (not while it lies on the
ground or flies). The clients are told with 17/34. Points start again after the reset time without a blow. 104 of the 108 scripts
that have an escape use kind 2. A helper that is getting up does not escape.

**Super armor skill.** `SetSuperArmorSkill` names a skill the monster plays each time its super armor is whole again; no script of this
client has it. The maze calls `MonsterBody.OnSuperArmorRestored` when an armor that broke is whole again, and that calls
`AiMachine.OnSuperArmorRestored`.

**The reaction gate.** A monster that flinches, is thrown, lies or gets up neither walks nor starts a skill (`MonsterReaction`,
`notes-combat.md`); the machine reads that through the host's `CanMove` and `CanAttack`. An escape that gives a defense also ends a
flinch at once.

## 5. The shot of a monster

A projectile of a monster never goes by a camera: it flies the way the monster faces, turned and lowered by the angles of the event, and
starts at the monster's body, or in front of and beside it for a thrown attack (`ProjectileFlight.LaunchByFacing`). The random spread
some events carry is not applied.

## 6. Comparison with the original

| Piece | Original | Ours | |
|---|---|---|---|
| Order of the steps of a blow, the hate formula, the look, the taunt | as above | same | same |
| Inclination of the monster in the hate | used | not used | kept different: not in the client's table |
| Escape: points, latch, chance, three kinds, 17/34 | as above | same | same |
| Escape kind 0 | super armor for the time unless the armor is broken, and the flinch ends | same | same (it was given to every monster before) |
| Group call | caller and called monsters | same | same |
| Spawn animation (05/0F) | one packet per monster | one packet per spawn | same in effect |
| The animations of all monsters to a player who comes in | the current animation of every monster | those in the middle of a spawn or common action | kept different: the stand animation is shown by the monster's own messages |
| Turn before walking | a turn animation in battle, else 05/0C with a 0.3 s delay before the move, when the turn is over 45 degrees | same (battle turns only with the animations of the fight) | same |
| Idle motions | every monster, 5 s and one in five (a script may change it) | same | same: the first reading, that only a script asks for them, was wrong |
| Grab (05/20, 05/15) | the first free player of the area, the hold, the let-go and the blow that follows, 05/15 | same, as above | same; the player is placed at the drop place when the time is up, not moved along the way; a hold broken by the monster's death is freed with 05/15 |
| Hiding (17/32) | four kinds of end | three kinds | the kind that waits for summoned monsters needs monsters that summon |
| Summoned monsters and who they fight | bits of the monster row | not used | only the bosses of the Akashic mode and field objects of a skill have them |
| Projectile direction | as above | same | fixed |

## 7. Not done, and what needs the real client

- Grabs of the bosses and the carry of a skill (`notes-combat.md`): to see with the real client.
- The number of the animation in 05/0C and 05/0F is the place of the animation in the action file of the monster (an assumption that
  the client reads it that way).
- Play session: a spawn animation (a monster rising from the ground), a monster that escapes with its skill, a monster turning on the
  one who hurt it most, a hidden monster, a projectile of a monster leaving at its side, a boss that grabs (the player is held at the bone,
  flies to the place, takes the blow and is put down; the grabbed one is not let to move), a monster that stands out of battle and plays its
  idle animation, a monster that turns before it walks.
