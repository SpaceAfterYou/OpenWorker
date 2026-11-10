# The actor number of a player, the move between servers, and the skills of a player seen by the others

Round 8, branch `feature/actor-ids`. The task was a new actor number for a person on every server (so that the client builds
the model of its player again when it comes from a district to a maze) and the skills of a player shown to the others (06/10).
What the original server does decided both.

## The actor number: one per person, on every server, as the original does

The original server gives a player one actor number, and it is the character id: the character is loaded with the number the
client sends when it enters (the number the previous server put in its redirect), and that number is the actor of the player on
every server. The same value is the actor in the world packets (movement, skills, hits, chat), the member in the party, the
friend, the member of a league and the key of everything the game sends to the database. Monsters, NPCs and the other actors
carry their kind in the three high bits, so no number of a player is ever theirs.

So nothing is split here: `ActorComponent.Identifier` of a player stays its person id on the gate, in a district and in a maze, and
the community code keeps using it as the person id. A different number per server would be a different protocol from the original's:
the client has one id space and compares the id of a packet with the id of its own player (party invite, join and leave, master,
trade, private shop, friend request, the party manager), and the packets of the community are encoded once by the server that
sends them and cannot carry a number per receiver.

## Why the maze kept the animations of the city, and the fix

The client chooses the animation set (district or battle) when the play scene starts, from the kind of the map it is on. It does
that for itself in the usual move, and it is given the kind by the record of the character (03/32) only in the login. The original
server answers every entry with `ENTER_GAMESERVER_RES` (03/22) first. The client then (a) at the character selection asks for the
record (03/31) and gets 03/32, (b) in the game, when it moves to another server (a portal, a maze, the way back, another channel),
puts the player at the place of the answer, loads the map if it is another one, and asks for the objects around it.

Our servers never sent 03/22. They answered every entry with the record, also a move in the game. The record sets the kind of the
map before the play scene starts, so the scene finds the set already chosen and does not switch it, and the model of the player is not
built again because the actor number is the same: the player kept the animations of the district in the maze and never played an
attack. (The client patch of 2026-11-10 removes that assignment.)

Now `PersonEnterGameServerRequest.FirstConnect` decides, as the client sets it from the reason of the connection (1 for the
character selection and the prologue, 0 for a move in the game):

| Client | District and maze answer |
|---|---|
| first connect | the record 03/32, as before |
| move in the game | 03/22: `i32 error` (0), `u8` (0), the place (laid out as in the record), `u8` echelon level, `i32` echelon experience |
| 03/31 (asks for the record) | the record 03/32, as before, whatever came first |

`PersonSnapshotMapper.CreateEntryResponse` makes both. The rest of the entry (skills, stats, options, the community) is unchanged.

## The skills of a player, to the others

After the answer 06/09 the player gets, everybody who sees the player (the channel around it in a district, the party in a maze)
but the player gets 06/10 (`SkillActiveBroadcastResponse`): the actor of the player, the skill, the place the server took for the skill,
the divergence the server has for the player (the number in the request is not trusted), the parent skill, the slot of the deck and
the random key of the request. The player gets 06/09 only.

* In a maze the skill must pass the checks that already decide the answer (owned, in the deck, level, hero, cost, cooldown). A refused skill
  is answered with its code and played by nobody.
* In a district nothing is taken for a skill and none is refused, as before; the others play it when it is one the player could start
  (owned, in the deck, level, hero, alive). The answer 06/09 now carries the divergence, as it does in a maze.

What the original broadcasts of a skill, and what ours does:

| Message | Original | Ours |
|---|---|---|
| 06/10 the start of a skill | everybody but the player; the actor in it is the one the client names | same (added); fixed: the actor is the player's own, not the one the request names |
| 06/13 the hits | everybody but the player (the client reports them) | kept different: the server judges the hits and sends them to everybody in the maze, the player too, because the client plays its own hits from them |
| 06/34 trap position, 06/22 akashic record | everybody but the player | the trap goes to everybody in the maze (kept); the akashic record is not changed |
| 06/42 key press, 06/44 target change, 06/36 pre-target list, 06/4B sub input, 06/4D start and 06/50 end of charging | everybody but the player | not sent to the others yet (open) |
| 06/14..06/17 buffs | everybody, the player too | belongs to the buffs branch |

## Open and for a session with the real client

* The move from a district to a maze, and back, without the client patch: the animations of the player in both, attack and skill in the maze.
* The same district on another channel: no loading screen, the objects shown again.
* Two players in a maze: the second sees the skills of the first.
* The character selection and the prologue still land in the right place (the record path is unchanged).
* If 03/22 is not enough, the fallback is a different actor number in the maze (the person id with a mark in a high bit of the number, the
  person id recovered by clearing it) and, because the client compares ids with its own player, the player's own id in the packets of the
  party built for it; that needs every packet of the community checked against the client, which is why it was not done first.
* The client patch (`action-set-switch.patch`) can go once the list above is checked with the real client; nothing in the client folder
  was removed.
