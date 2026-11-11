# The movement of a player in a fight

Round 9, branch `feature/fight-movement`. What the server does with the places a client reports, the moves that skills make,
and what the others see of the input of a player's skill. The client 1.4.2.3 decides every wire layout; the original server
decides the rules. Earlier notes: `docs/notes-combat.md` (the checks of a skill), `docs/notes-actor-ids.md` (the answer to a
move between servers, 06/10).

## The first move in a map, and the moves of the map the client left

The first move after the entry to a maze was refused as about 68 000 units too far. That distance is the one between the
start of a district and the start of the biggest maze (District 6), so the place that was refused was the one the client
had left. On the stand the server keeps exactly the place it tells the client (the reservation, the answer of the district
04/02 and the answer 03/22 of the maze all name the same place), so the fault is on the other side: the client goes on
reporting its object's place (a timer sends the place, and the walk and stop messages come from the same object) until the
loading scene has taken the place of 03/22. Those messages reach the new server first.

Two rules of the original server and one of ours deal with them:

- A move, a stop, a jump and a position report name the map the client is in. A request whose map is not the map of the
  player is dropped without a word: no answer, no warp, no fault. (The original does the same before it looks at the place.)
- When the server tells a client a place (the entry to a district or a maze, a warp inside a maze, the escape of a stuck
  player), the moves that come while the client has not taken it are dropped without a fault if they are too far from the
  place the server keeps. The grace is `World:PlaceGrace` (5 s); the first move from near the told place ends it. This is
  not in the original; it covers the moves that carry the new map and the old place.

## The rule of a move

As in the original server (and the opposite of what this server did):

- A move is refused only when it takes the player further than `World:MaxMoveStep` (3000 units, the two axes added) from the
  place the server keeps, or when the place is not a number. The place stays, nobody else is told, the player is warped back
  to the place the server keeps (04/08), and the first refusal in a second counts against the error budget of the session.
- A move that covers more ground than the hero could have run in the time it had (the run speed of the hero, the move speed
  of the abilities, `World:SpeedTolerance`, a burst of `World:MoveBurst` saved up) is let through and counted
  (`movement.too_fast`). A dash, an evade, a jump or the move of a skill is far below the limit of a message and goes through.
  Refusals are counted too (`movement.refused`, tag reason `far` or `stale`).
- The same rule holds in a district and in a maze, and for the places of the skill requests that carry one.

Not done: the original also refuses, in a district only, a move that rises more than 500 units with no floor under the new
place. Our districts have no navigation mesh.

## The ground state (05/40)

The client says whether the player stands on the ground and how high the ground is. The server keeps what it last said
(`GroundStateComponent`) for the player alone and tells nobody. Nothing reads it yet: the original keeps the same two
numbers and no reader of them was found by name (a fall's damage and the skills that need the ground would be the readers).

## Skills that move the player

All of these run in a maze only, for a player who is on its feet:

- 06/45 (warp): the place is taken when it is within the reach of a move, and everybody in the maze, the player too, is told
  where the player is (06/46). A place out of reach is not taken; the player alone is told the place the server keeps.
  The original also clamps the place to the walkable ground (no query for that is exposed here) and does not look at the
  skill that sent it: the action files of this server do not keep the movement of an animation, so no distance of the
  skill is checked. It is the same limit of a message as for a move.
- 06/47 (sync) and 06/05 (end of a dash or an evade): the place is taken when it is within the reach of a move; nothing is
  sent to the others. 06/48 and 06/49 are what the original sends for what the server itself moves (a monster that lands,
  an animation that moves a monster); it never sends them for a player, so neither do we.

## What the others see of the input of a skill

In a maze, to everybody who shares the maze with the player but the player, with the actor of the player first (the actor in
the request is never trusted):

| Request | Sent to the others |
|---|---|
| 06/41 key of a skill held or let go | 06/42 |
| 06/43 target picked | 06/44 |
| 06/4A sub animation asked for | 06/4B (the client reads the index only) |
| 06/4C without a body: charging starts | 06/4D |
| 06/4C with four bytes: charging ends after that time | 06/50 |
| 06/36 targets of a skill | 06/36 with the actor of the player first |
| 06/33 place of a trap dragged with the mouse | 06/34 |

The client 1.4.2.3 sends the start and the end of charging under one opcode (06/4C), which the original splits into two; the
request reads the body to tell them apart. Numbers out of bounds are not sent on: a sub animation outside 0 to 255, a press
time that is not a number or is past a minute, more than 32 targets, a trap place that is not a place. A district sends
nothing (the original does not either). `SkillInputRelay` does the sending.

## Comparison with the original

| Piece | Original | Ours | |
|---|---|---|---|
| Request of another map than the player's | dropped | dropped | same |
| Move over 3000 units added over both axes | refused, player warped back with 06/46 | refused, warped back with 04/08 | same rule; the client takes either message |
| Rise of more than 500 without a floor | refused in a district | not done | kept different: no navigation in a district |
| Moves too fast | counted and logged, not in a district | counted by time credit, everywhere | approximately |
| Moves from the place the client left, after a told place | none known | dropped for 5 s | added |
| 05/40 | kept | kept, nothing reads it | same |
| 06/45 | maze, alive, place clamped to the ground, told to everybody | maze, alive, limit of a message, told to everybody; refused to the player alone | kept different: no clamp |
| 06/47 | maze, alive, place replaced by the server's when too far, camera direction | same without the camera direction | same |
| 06/48 and 06/49 of a player | never sent | not sent | same (the plan asked for them: not done, see above) |
| 06/42, 06/44, 06/4B, 06/4D, 06/50, 06/36, 06/34 | maze, to the others | maze, to the others | fixed (06/34 was sent to nobody, the others were not sent) |
| 06/4A with a yaw | the original's request has one | the client sends the index only | the client decides |
| 06/4E and 06/51 (end of charging, both clicks) | requests of the original | no sender in this client | not done |

## Tests

`MovementRuleTest`: a move too fast but not too far is taken and counted; a move of another map is dropped without a fault;
the moves of the place the client left are dropped until it takes the told place and refused after the grace; the ground
state is kept for its own player. `DistrictMovementTest` keeps the refusal of a move over the limit. `SkillInputSeenTest`: the
input goes to the others and not to the player, and numbers out of bounds are not sent on. `SkillWireFormatTest`: both shapes
of 06/4C and the layout of the list of targets.

## To check with the real client

- Enter District 6 from Rocco Town and any maze by a portal and by `/warp`: the maze log has no "Refused a move" line, and
  `movement.refused{reason=stale}` counts the moves that were dropped.
- The dash, the evade and a jump near a wall are felt as before; nobody is pulled back.
- A warp skill is seen by a second player of the party, and so are the held key of a skill, a charging skill and a trap.
