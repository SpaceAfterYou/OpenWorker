# The prologue and the tutorial mazes

A new character starts the game the way the client 1.4.2.3 expects: the character selection sends it to the first
tutorial maze instead of its district, the client plays the prologue there and in the second tutorial maze, and the
way out of the second one takes it to the first district. It happens once; a character that has played it, and every
character made before there was a prologue, goes to its district.

## The flow

| Step | Client | Server |
|---|---|---|
| Selection | `03/13` (the flag after the account is always 0 in this client) | the gate answers `03/15` instead of `03/14` when the character has the prologue ahead, with the maze server and the start box of `T01_TUTORIAL` (maze 21111) |
| Entry | connects to the address of `03/15` exactly as for `03/14` and sends `03/21` | the maze takes the reservation the gate wrote, like any maze entry |
| First tutorial | loads the map, `11/22`; plays its cutscenes (`03/19` when one starts and ends); walks into the boxes of the script (`11/7E`), finishes talks (`11/62`) | the script of `T01_TUTORIAL.lua` runs: quests reset, a buff, the fight after the cutscene, the first quest |
| On to the second | at the end of its last cutscene the client sends `04/01` for maze 21112 itself | the ordinary entry into a maze from a maze (`04/02`, a new connection) |
| Second tutorial | the guide walks ahead of the player, the player takes the tutorial quests, uses skills, clicks the switch, fights | the script of `T02_TUTORIAL.lua` runs; the maze tells it about the quests and the skills of the player and when the guide reaches each point |
| The end | at the end of the last cutscene, or when the player skips a cutscene with Esc in either maze, the client sends the way out of the maze (`11/23`) | the prologue ends (below), and the way out of the maze leads to district 10003, start box 1000301, as its escape box says |

The client handles `03/15` and `03/14` the same way: both connect to the given address and send `03/21` with the flag 1. It
keeps no other value of the answer than the map, the address and the port. The type of change in `03/15` is 0 (entering a
map), and the type of the map is a maze.

`03/16` (into a battle zone) is read the same way too; the client keeps the map, the address, the port and the result,
which is the int before the list (`PersonEnterBattleZoneResponse.Result`). The other values between the zone and the result
are not read, so they keep their placeholder names. The server never sends it.

`03/20` is the answer to `03/19`: a byte, not zero while a cutscene plays (the client shows that someone watches one) and
zero when it is over. It is unchanged.

## The state

`Persons.ProloguePending` (bool, default false): true while the character has the prologue ahead.

- A column rather than a document: the gate needs one bit of the row at selection and nothing else, the maze reads it with
  the row it loads at entry, and it changes once in the life of a character. A document would cost a second query at each
  entry for one bit.
- The default false makes every existing character count as having played the prologue. The gate sets it to the value of
  the setting when it creates a character.
- It is cleared by `IPrologueStore.FinishAsync`, an update with the condition `ProloguePending`, so two servers that end
  the prologue at once write it once; the call returns whether it changed the row.
- The gate reads it at selection (`IPrologueStore.IsPendingAsync`), fresh from the database.

## The end of the prologue

`PrologueService` of the maze server. At entry it marks the player (`PrologueComponent`) when the row says the prologue is
ahead. When such a player takes the way out of a tutorial maze (type 1 of `tb_Maze_Info`: 21111 and 21112):

1. the mark goes first, so a second request to leave does nothing;
2. the quests of the tutorial are reset (`QuestManager.RemoveAllActiveEpisodes`), and the quest that stands for the whole
   prologue is taken and finished by force (`AcceptEpisode`, `CompleteEpisode`), so the story goes on from it: episode
   100017, whose finish opens 100020, the first quest of the town;
3. the player is flushed, so its quests are written before the flag;
4. the flag is cleared, once.

A character that has played it and comes back into a tutorial maze through a portal leaves it like any maze. A character
that leaves the tutorial another way (disconnects, goes back to the gate) keeps the flag and starts the prologue again at
the next selection.

## What the tutorial gives

- The quest of the whole prologue, 100017, finished, with what it gives: in the client tables nothing (no experience, gold
  or items); its finish is what lets the player take 100020.
- On the way, what the tutorial quests give through the quest module, when the player finishes them: 100015 gives the
  items 640101001 ×3 and 630101001 ×5.
- The buff 10000 that both scripts apply at entry (it is not in the buff table of the client, so nothing is applied).
- No experience or skills of its own.

The quest that stands for the prologue comes from the server's own `tb_Character_Info` (`CharacterStartRow`, read from
`res-server/` only): the tutorial maze and its start box, the first quest and that quest, per hero. Without the table the
prologue starts in maze 21111 at its default start box (2111101) and ends without a quest. The table names the start box
2110001, which the client's map of `T01_TUTORIAL` does not have, so the default box is used.

## The maze API the tutorial scripts use

| Call or event | What it does now |
|---|---|
| `MoveNpcToWayPoint(npc, point)` | the townsman walks straight to the point of the map at 300 units a second (the client sees `05/02`, then `05/04` when it arrives), and the script hears `OnNpcWayPoint` when it is there; a later walk of the same townsman cancels the earlier arrival |
| `SetNpcRotation(npc, yaw, key)` | the townsman turns (`05/14`), and the script hears `OnNpcRotation` a second later, as before |
| `OnActionSkill(skill)` | the script hears every skill the player starts |
| `OnUpdateQuest(user, type, id)` | the script hears the episodes taken (0), finished (1) and the conditions done (2), from any part of the game: `QuestWatch` compares the quests of the player with what it saw at the previous step of the clock; only a script that has the function is watched |
| `ChangeNpcAnimation`, `SetMonsterAllowPassiveType` | still not done (logged once); the first only changes what the client shows, the second only how the guide's monsters take passive skills |

Everything else the two scripts call was there already.

## The setting

`Game:Prologue` (bool, default true, in `GameOptions`; the gate reads it). Off, the gate sends every character to its
district, and characters it creates have no prologue ahead, so they never see it even when the setting is turned on later.
The maze needs no setting: it ends the prologue only of a character that has it ahead.

The test stand starts the gate with it off (`OW_PROLOGUE` of `docs/tools/stand_up.sh`, false by default), so the e2e
scripts that select a new character land in the district as before; `OW_PROLOGUE=true` turns it on for
`docs/tools/e2e_prologue.py`.

## Assumptions

- The walking speed of townsfolk (300) is the walking speed of the heroes; the tables give townsfolk no speed.
- The reset of the quests at the end clears the finished episodes too (that is what `RemoveAllActiveEpisodes` does); for
  a new character they are the tutorial quests only.
- The quest of the whole prologue is not given to characters that had played it before this change, or that finished
  the prologue some other way; a character whose flag is cleared always has it.
- The prologue ends at the way out of either tutorial maze, also when the player skips the cutscenes of the first one.

## To check with the real client

- That `03/15` with change type 0 and map type maze loads `T01_TUTORIAL` and plays the first cutscene group.
- That the guide of `T02_TUTORIAL` walks visibly from point to point with the move messages of a monster, and turns.
- That the tutorial end button and Esc on a cutscene both send `11/23` and the client lands in the district.
- The flag after the account in `03/13` (always 0 here).
